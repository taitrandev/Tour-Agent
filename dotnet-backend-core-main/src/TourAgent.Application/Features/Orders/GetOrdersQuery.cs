namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Truy vấn danh sách đơn hàng tour kèm bộ lọc theo pipeline status, từ khóa tìm kiếm và phân trang.
/// </summary>
/// <param name="Status">Trạng thái cần lọc (lead_consulting, deposit_pending, visa_processing, final_payment_pending, t3_departure, ready_to_depart).</param>
/// <param name="Search">Từ khóa tìm kiếm (Mã đơn, Tên khách, SĐT, Tên tour).</param>
/// <param name="Page">Số trang (bắt đầu từ 1).</param>
/// <param name="PerPage">Số lượng bản ghi mỗi trang (mặc định 50).</param>
public sealed record GetOrdersQuery(
    string? Status = null,
    string? Search = null,
    int Page = 1,
    int PerPage = 50) : IQuery<Result<OrdersListResponseDto>>;

/// <summary>
/// Xử lý truy vấn danh sách đơn hàng tour, tính toán metrics realtime và trả về kết quả chuẩn.
/// </summary>
public sealed class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, Result<OrdersListResponseDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public GetOrdersQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực thi truy vấn danh sách đơn hàng theo điều kiện lọc và phân trang.
    /// </summary>
    /// <param name="query">Tham số truy vấn.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<OrdersListResponseDto>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var perPage = query.PerPage <= 0 ? 50 : query.PerPage;

        var ordersQuery = _dbContext.TourOrders
            .AsNoTracking()
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .Include(o => o.Departure)
            .AsQueryable();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var in3Days = today.AddDays(3);

        // 1. Lọc theo trạng thái pipeline
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusFilter = query.Status.Trim().ToLowerInvariant();
            if (statusFilter == "t3_departure")
            {
                ordersQuery = ordersQuery.Where(o => o.DepartureDate != null && o.DepartureDate >= today && o.DepartureDate <= in3Days);
            }
            else
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }
        }

        // 2. Lọc theo từ khóa tìm kiếm (Mã đơn, Tên khách, SĐT, Username, Tên tour)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLowerInvariant();
            ordersQuery = ordersQuery.Where(o =>
                o.OrderCode.ToLower().Contains(keyword) ||
                (o.Contact != null && (
                    (o.Contact.Name != null && o.Contact.Name.ToLower().Contains(keyword)) ||
                    (o.Contact.Phone != null && o.Contact.Phone.ToLower().Contains(keyword)) ||
                    (o.Contact.TelegramUsername != null && o.Contact.TelegramUsername.ToLower().Contains(keyword))
                )) ||
                (o.Travel != null && (
                    o.Travel.Name.ToLower().Contains(keyword) ||
                    (o.Travel.Code != null && o.Travel.Code.ToLower().Contains(keyword))
                ))
            );
        }

        // 3. Đếm tổng số bản ghi thỏa điều kiện lọc
        var total = await ordersQuery.CountAsync(cancellationToken);

        // 4. Lấy dữ liệu phân trang
        var orders = await ordersQuery
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync(cancellationToken);

        // 5. Tính toán các chỉ số thống kê Realtime toàn hệ thống
        var allOrders = _dbContext.TourOrders.AsNoTracking();
        var statTotal = await allOrders.CountAsync(cancellationToken);
        var statConsulting = await allOrders.Where(o => o.Status == "lead_consulting").CountAsync(cancellationToken);
        var statDepositPending = await allOrders.Where(o => o.Status == "deposit_pending").CountAsync(cancellationToken);
        var statVisaProcessing = await allOrders.Where(o => o.Status == "visa_processing").CountAsync(cancellationToken);
        var statFinalPayment = await allOrders.Where(o => o.Status == "final_payment_pending").CountAsync(cancellationToken);
        var statT3Count = await allOrders.Where(o => o.DepartureDate != null && o.DepartureDate >= today && o.DepartureDate <= in3Days).CountAsync(cancellationToken);

        var metrics = new OrderMetricsDto(
            Total: statTotal,
            LeadConsulting: statConsulting,
            DepositPending: statDepositPending,
            VisaProcessing: statVisaProcessing,
            FinalPaymentPending: statFinalPayment,
            T3Departure: statT3Count);

        // 6. Chuyển đổi sang danh sách DTO
        var items = orders.Select(o => new TourOrderDto(
            Id: o.Id,
            OrderCode: o.OrderCode,
            ContactId: o.ContactId,
            Contact: o.Contact == null ? null : new OrderContactDto(
                Id: o.Contact.Id,
                Name: o.Contact.Name,
                Phone: o.Contact.Phone,
                Email: o.Contact.Email,
                TelegramUsername: o.Contact.TelegramUsername,
                Channel: o.Contact.Channel),
            TravelId: o.TravelId,
            Travel: o.Travel == null ? null : new OrderTravelDto(
                Id: o.Travel.Id,
                Title: o.Travel.Name,
                Code: o.Travel.Code,
                PriceFrom: o.Travel.PriceFrom),
            TravelDepartureId: o.TravelDepartureId,
            ConversationId: o.ConversationId,
            Channel: o.Channel,
            PaxAdults: o.PaxAdults,
            PaxChildren: o.PaxChildren,
            TotalPrice: o.TotalPrice,
            DepositPercent: o.DepositPercent,
            DepositAmount: o.DepositAmount,
            DepositStatus: o.DepositStatus,
            DepositPaidAt: o.DepositPaidAt,
            RemainingAmount: o.RemainingAmount,
            RemainingStatus: o.RemainingStatus,
            RemainingPaidAt: o.RemainingPaidAt,
            PaymentAccountNote: o.PaymentAccountNote,
            VisaStatus: o.VisaStatus,
            VisaNotes: o.VisaNotes,
            OperatorName: o.OperatorName,
            OperatorBookingRef: o.OperatorBookingRef,
            DepartureDate: o.DepartureDate?.ToString("yyyy-MM-dd"),
            TourGuideName: o.TourGuideName,
            TourGuidePhone: o.TourGuidePhone,
            TourGuideNotes: o.TourGuideNotes,
            PreDepartureNotifiedAt: o.PreDepartureNotifiedAt,
            Status: o.Status,
            AiSummary: o.AiSummary,
            Notes: o.Notes,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt)).ToList();

        var lastPage = (int)Math.Ceiling((double)total / perPage);
        var pagination = new Contacts.PaginationMetaDto(
            CurrentPage: page,
            LastPage: Math.Max(1, lastPage),
            PerPage: perPage,
            Total: total);

        var response = new OrdersListResponseDto(
            Success: true,
            Data: items,
            Pagination: pagination,
            Metrics: metrics);

        return Result.Success(response);
    }
}

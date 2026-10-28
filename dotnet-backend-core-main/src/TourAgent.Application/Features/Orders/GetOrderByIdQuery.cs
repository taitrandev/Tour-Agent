namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Truy vấn thông tin chi tiết một đơn hàng tour theo mã định danh ID.
/// </summary>
/// <param name="Id">ID đơn hàng cần xem.</param>
public sealed record GetOrderByIdQuery(long Id) : IQuery<Result<TourOrderDto>>;

/// <summary>
/// Xử lý truy vấn thông tin chi tiết đơn hàng tour kèm các quan hệ liên kết.
/// </summary>
public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<TourOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public GetOrderByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Nạp chi tiết đơn hàng và ánh xạ sang TourOrderDto.
    /// </summary>
    /// <param name="query">Đối tượng chứa ID đơn hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<TourOrderDto>> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .AsNoTracking()
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .Include(o => o.Departure)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure<TourOrderDto>(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng với mã ID {query.Id}."));
        }

        var dto = new TourOrderDto(
            Id: order.Id,
            OrderCode: order.OrderCode,
            ContactId: order.ContactId,
            Contact: order.Contact == null ? null : new OrderContactDto(
                Id: order.Contact.Id,
                Name: order.Contact.Name,
                Phone: order.Contact.Phone,
                Email: order.Contact.Email,
                TelegramUsername: order.Contact.TelegramUsername,
                Channel: order.Contact.Channel),
            TravelId: order.TravelId,
            Travel: order.Travel == null ? null : new OrderTravelDto(
                Id: order.Travel.Id,
                Title: order.Travel.Name,
                Code: order.Travel.Code,
                PriceFrom: order.Travel.PriceFrom),
            TravelDepartureId: order.TravelDepartureId,
            ConversationId: order.ConversationId,
            Channel: order.Channel,
            PaxAdults: order.PaxAdults,
            PaxChildren: order.PaxChildren,
            TotalPrice: order.TotalPrice,
            DepositPercent: order.DepositPercent,
            DepositAmount: order.DepositAmount,
            DepositStatus: order.DepositStatus,
            DepositPaidAt: order.DepositPaidAt,
            RemainingAmount: order.RemainingAmount,
            RemainingStatus: order.RemainingStatus,
            RemainingPaidAt: order.RemainingPaidAt,
            PaymentAccountNote: order.PaymentAccountNote,
            VisaStatus: order.VisaStatus,
            VisaNotes: order.VisaNotes,
            OperatorName: order.OperatorName,
            OperatorBookingRef: order.OperatorBookingRef,
            DepartureDate: order.DepartureDate?.ToString("yyyy-MM-dd"),
            TourGuideName: order.TourGuideName,
            TourGuidePhone: order.TourGuidePhone,
            TourGuideNotes: order.TourGuideNotes,
            PreDepartureNotifiedAt: order.PreDepartureNotifiedAt,
            Status: order.Status,
            AiSummary: order.AiSummary,
            Notes: order.Notes,
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt);

        return Result.Success(dto);
    }
}

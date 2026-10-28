namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Contacts;

/// <summary>
/// Lệnh xác nhận khách hàng đã hoàn tất chuyển khoản đặt cọc 50%.
/// </summary>
/// <param name="Id">ID đơn hàng cần xác nhận cọc.</param>
/// <param name="PaymentAccountNote">Ghi chú đối soát STK hoặc mã giao dịch ngân hàng.</param>
public sealed record MarkDepositCommand(
    long Id,
    string? PaymentAccountNote = null) : ICommand<Result<TourOrderDto>>;

/// <summary>
/// Xử lý xác nhận cọc, cập nhật trạng thái đơn hàng sang visa_processing hoặc final_payment_pending,
/// đồng thời nâng stage của CRM Contact lên 'hot'.
/// </summary>
public sealed class MarkDepositCommandHandler : ICommandHandler<MarkDepositCommand, Result<TourOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public MarkDepositCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực thi đánh dấu đã cọc và nâng cấp trạng thái Lead trong CRM.
    /// </summary>
    /// <param name="command">Thông tin lệnh.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<TourOrderDto>> HandleAsync(
        MarkDepositCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure<TourOrderDto>(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng #{command.Id}."));
        }

        order.MarkDepositPaid(command.PaymentAccountNote);

        // Nâng stage của Contact lên Hot nếu đang ở Prospect hoặc Warm
        if (order.Contact != null && order.Contact.Stage != CrmContact.StageCustomer)
        {
            order.Contact.UpdateStage(CrmContact.StageHot);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

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

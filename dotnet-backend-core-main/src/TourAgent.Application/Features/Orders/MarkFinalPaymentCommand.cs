namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Contacts;

/// <summary>
/// Lệnh xác nhận khách hàng đã hoàn tất thanh toán 50% còn lại (hoàn tất 100% tiền tour).
/// </summary>
/// <param name="Id">ID đơn hàng cần xác nhận thanh toán đủ.</param>
public sealed record MarkFinalPaymentCommand(long Id) : ICommand<Result<TourOrderDto>>;

/// <summary>
/// Xử lý xác nhận thanh toán đủ 100%, nâng trạng thái đơn sang 'ready_to_depart' và nâng stage Contact lên 'customer'.
/// </summary>
public sealed class MarkFinalPaymentCommandHandler : ICommandHandler<MarkFinalPaymentCommand, Result<TourOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public MarkFinalPaymentCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực thi ghi nhận thanh toán hoàn tất cho đơn hàng.
    /// </summary>
    /// <param name="command">Thông tin lệnh.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<TourOrderDto>> HandleAsync(
        MarkFinalPaymentCommand command,
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

        order.MarkRemainingPaid();

        // Nâng stage của Contact lên Customer (đã chốt sale thành công)
        if (order.Contact != null)
        {
            order.Contact.UpdateStage(CrmContact.StageCustomer);
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

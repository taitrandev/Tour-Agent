namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Kết quả phản hồi sau khi cập nhật tiến trình Visa kèm mẫu tin nhắn chúc mừng (nếu đậu visa).
/// </summary>
public sealed record UpdateVisaResult(
    TourOrderDto Order,
    string? ZaloMessage);

/// <summary>
/// Lệnh cập nhật trạng thái làm visa cho đơn hàng.
/// </summary>
public sealed record UpdateVisaStatusCommand(
    long Id,
    string VisaStatus,
    string? VisaNotes = null,
    string? OperatorName = null,
    string? OperatorBookingRef = null) : ICommand<Result<UpdateVisaResult>>;

/// <summary>
/// Xử lý cập nhật tiến trình visa và soạn sẵn tin nhắn Zalo gửi khách hàng.
/// </summary>
public sealed class UpdateVisaStatusCommandHandler : ICommandHandler<UpdateVisaStatusCommand, Result<UpdateVisaResult>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public UpdateVisaStatusCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện cập nhật trạng thái visa, tự động chuyển đơn hàng sang final_payment_pending nếu visa đậu.
    /// </summary>
    /// <param name="command">Tham số cập nhật visa.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<UpdateVisaResult>> HandleAsync(
        UpdateVisaStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure<UpdateVisaResult>(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng #{command.Id}."));
        }

        order.UpdateVisaStatus(
            status: command.VisaStatus,
            notes: command.VisaNotes,
            operatorName: command.OperatorName,
            operatorBookingRef: command.OperatorBookingRef);

        await _dbContext.SaveChangesAsync(cancellationToken);

        string? zaloMessage = null;
        if (command.VisaStatus == "approved")
        {
            var customerName = order.Contact?.Name ?? "Quý khách";
            var tourTitle = order.Travel?.Name ?? "chương trình du lịch";
            zaloMessage = order.GetZaloVisaSuccessMessage(customerName, tourTitle);
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

        return Result.Success(new UpdateVisaResult(dto, zaloMessage));
    }
}

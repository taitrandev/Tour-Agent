namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Kết quả phản hồi sau khi lưu thông tin HDV và dặn dò T-3 kèm tin nhắn Zalo soạn sẵn.
/// </summary>
public sealed record MarkT3NotifiedResult(
    TourOrderDto Order,
    string ZaloMessage);

/// <summary>
/// Lệnh cập nhật thông tin Hướng Dẫn Viên và đánh dấu đã dặn dò khách hàng trước ngày khởi hành 3 ngày (T-3).
/// </summary>
public sealed record MarkT3NotifiedCommand(
    long Id,
    string? TourGuideName = null,
    string? TourGuidePhone = null,
    string? TourGuideNotes = null) : ICommand<Result<MarkT3NotifiedResult>>;

/// <summary>
/// Xử lý cập nhật thông tin HDV và chuẩn bị mẫu tin nhắn dặn dò khởi hành T-3.
/// </summary>
public sealed class MarkT3NotifiedCommandHandler : ICommandHandler<MarkT3NotifiedCommand, Result<MarkT3NotifiedResult>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public MarkT3NotifiedCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện ghi nhận thông tin dặn dò và HDV.
    /// </summary>
    /// <param name="command">Thông tin lệnh dặn dò.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<MarkT3NotifiedResult>> HandleAsync(
        MarkT3NotifiedCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure<MarkT3NotifiedResult>(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng #{command.Id}."));
        }

        order.MarkT3Notified(
            guideName: command.TourGuideName,
            guidePhone: command.TourGuidePhone,
            guideNotes: command.TourGuideNotes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var customerName = order.Contact?.Name ?? "Quý khách";
        var tourTitle = order.Travel?.Name ?? "chương trình du lịch";
        var zaloMessage = order.GetZaloT3DepartureMessage(customerName, tourTitle);

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

        return Result.Success(new MarkT3NotifiedResult(dto, zaloMessage));
    }
}

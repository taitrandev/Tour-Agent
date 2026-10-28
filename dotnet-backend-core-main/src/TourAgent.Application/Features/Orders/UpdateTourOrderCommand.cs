namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Lệnh cập nhật thông tin đơn hàng tour.
/// </summary>
public sealed record UpdateTourOrderCommand(
    long Id,
    int? PaxAdults = null,
    int? PaxChildren = null,
    decimal? TotalPrice = null,
    string? DepartureDate = null,
    string? OperatorName = null,
    string? OperatorBookingRef = null,
    string? PaymentAccountNote = null,
    string? Status = null,
    string? Notes = null) : ICommand<Result<TourOrderDto>>;

/// <summary>
/// Xử lý cập nhật thông tin đơn hàng tour.
/// </summary>
public sealed class UpdateTourOrderCommandHandler : ICommandHandler<UpdateTourOrderCommand, Result<TourOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public UpdateTourOrderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện cập nhật các trường thông tin đơn hàng.
    /// </summary>
    /// <param name="command">Dữ liệu cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<TourOrderDto>> HandleAsync(
        UpdateTourOrderCommand command,
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

        DateOnly? departureDate = null;
        if (!string.IsNullOrWhiteSpace(command.DepartureDate) && DateOnly.TryParse(command.DepartureDate, out var dt))
        {
            departureDate = dt;
        }

        order.UpdateDetails(
            paxAdults: command.PaxAdults ?? order.PaxAdults,
            paxChildren: command.PaxChildren ?? order.PaxChildren,
            totalPrice: command.TotalPrice ?? order.TotalPrice,
            departureDate: departureDate ?? order.DepartureDate,
            operatorName: command.OperatorName ?? order.OperatorName,
            operatorBookingRef: command.OperatorBookingRef ?? order.OperatorBookingRef,
            paymentAccountNote: command.PaymentAccountNote ?? order.PaymentAccountNote,
            status: command.Status ?? order.Status,
            notes: command.Notes ?? order.Notes);

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

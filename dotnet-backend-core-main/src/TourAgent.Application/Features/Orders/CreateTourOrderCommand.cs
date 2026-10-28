namespace TourAgent.Application.Features.Orders;

using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Orders;

/// <summary>
/// Lệnh tạo mới một đơn hàng tour thủ công (Manual Booking / Sales tạo đơn).
/// </summary>
public sealed record CreateTourOrderCommand(
    long? ContactId = null,
    long? TravelId = null,
    long? TravelDepartureId = null,
    long? ConversationId = null,
    string? Channel = null,
    int PaxAdults = 1,
    int PaxChildren = 0,
    decimal TotalPrice = 0,
    int DepositPercent = 50,
    string? DepartureDate = null,
    string? PaymentAccountNote = null,
    string? AiSummary = null,
    string? Notes = null) : ICommand<Result<TourOrderDto>>;

/// <summary>
/// Xử lý tạo mới đơn hàng tour, kiểm tra hợp lệ và lưu vào cơ sở dữ liệu.
/// </summary>
public sealed class CreateTourOrderCommandHandler : ICommandHandler<CreateTourOrderCommand, Result<TourOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public CreateTourOrderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện tạo đơn hàng mới và trả về DTO của đơn hàng vừa tạo.
    /// </summary>
    /// <param name="command">Dữ liệu đầu vào của đơn hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<TourOrderDto>> HandleAsync(
        CreateTourOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        DateOnly? parsedDepartureDate = null;
        if (!string.IsNullOrWhiteSpace(command.DepartureDate) && DateOnly.TryParse(command.DepartureDate, out var dt))
        {
            parsedDepartureDate = dt;
        }

        var orderResult = TourOrder.Create(
            contactId: command.ContactId,
            travelId: command.TravelId,
            travelDepartureId: command.TravelDepartureId,
            conversationId: command.ConversationId,
            channel: command.Channel ?? "manual",
            paxAdults: command.PaxAdults,
            paxChildren: command.PaxChildren,
            totalPrice: command.TotalPrice,
            depositPercent: command.DepositPercent,
            departureDate: parsedDepartureDate,
            paymentAccountNote: command.PaymentAccountNote,
            aiSummary: command.AiSummary,
            notes: command.Notes);

        if (!orderResult.IsSuccess)
        {
            return Result.Failure<TourOrderDto>(orderResult.Error);
        }

        var order = orderResult.Value;
        _dbContext.TourOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new TourOrderDto(
            Id: order.Id,
            OrderCode: order.OrderCode,
            ContactId: order.ContactId,
            Contact: null,
            TravelId: order.TravelId,
            Travel: null,
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

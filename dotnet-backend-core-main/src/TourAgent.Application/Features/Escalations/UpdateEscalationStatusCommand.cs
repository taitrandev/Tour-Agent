namespace TourAgent.Application.Features.Escalations;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Lệnh cập nhật trạng thái phiếu chuyển giao chuyên viên (Escalation Desk Action).
/// </summary>
/// <param name="Id">Mã định danh phiếu yêu cầu.</param>
/// <param name="NewStatus">Trạng thái mới (contacting, approved, rejected, closed).</param>
/// <param name="ResolvedBy">Tên chuyên viên hoặc người duyệt thao tác.</param>
/// <param name="Notes">Ghi chú bổ sung khi xử lý.</param>
public sealed record UpdateEscalationStatusCommand(
    long Id,
    string NewStatus,
    string? ResolvedBy = "Admin",
    string? Notes = null
) : ICommand<Result<EscalationTicketDto>>;

/// <summary>
/// Handler xử lý cập nhật trạng thái Escalation trong cơ sở dữ liệu.
/// </summary>
public sealed class UpdateEscalationStatusCommandHandler : ICommandHandler<UpdateEscalationStatusCommand, Result<EscalationTicketDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu.</param>
    public UpdateEscalationStatusCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<EscalationTicketDto>> HandleAsync(
        UpdateEscalationStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Escalations
            .Include(e => e.Conversation)
            .Include(e => e.Travel)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<EscalationTicketDto>(new Error("Escalation.NotFound", $"Không tìm thấy phiếu yêu cầu với ID #{command.Id}."));
        }

        var normalizedStatus = command.NewStatus.Trim().ToLowerInvariant();
        var operatorName = !string.IsNullOrWhiteSpace(command.ResolvedBy) ? command.ResolvedBy : "Admin";

        switch (normalizedStatus)
        {
            case "contacting":
                ticket.MarkAsContacting(operatorName);
                break;

            case "approved":
                ticket.Approve(operatorName, notes: command.Notes);
                break;

            case "rejected":
            case "closed":
                ticket.Reject(operatorName, command.Notes);
                break;

            default:
                return Result.Failure<EscalationTicketDto>(new Error("Escalation.InvalidStatus", $"Trạng thái '{command.NewStatus}' không hợp lệ."));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var channel = ticket.Conversation?.Channel ?? (ticket.ChatId != null ? "telegram" : "web_widget");
        var tourTitle = ticket.Travel?.Name ?? (ticket.TravelId.HasValue ? $"Tour #{ticket.TravelId}" : "Chưa chọn tour cụ thể");

        var dto = new EscalationTicketDto(
            Id: ticket.Id,
            Code: $"#ESC-{ticket.Id}",
            CustomerName: !string.IsNullOrWhiteSpace(ticket.CustomerName) ? ticket.CustomerName : "Khách quan tâm tour",
            CustomerPhone: ticket.CustomerPhone ?? "Chưa có SĐT",
            Channel: channel.Contains("telegram", StringComparison.OrdinalIgnoreCase) ? "telegram" : "web_widget",
            TourTitle: tourTitle,
            DiscountPercent: ticket.DiscountPercent ?? 0,
            GroupSize: ticket.GroupSize ?? 1,
            Notes: ticket.Notes ?? string.Empty,
            Status: ticket.Status,
            TimeAgo: "Vừa xong",
            CreatedAt: ticket.CreatedAtUtc,
            ConversationId: ticket.ConversationId,
            TravelId: ticket.TravelId
        );

        return Result.Success(dto);
    }
}

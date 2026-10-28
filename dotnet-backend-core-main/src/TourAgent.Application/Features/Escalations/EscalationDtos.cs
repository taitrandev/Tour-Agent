namespace TourAgent.Application.Features.Escalations;

/// <summary>
/// DTO biểu diễn dữ liệu của một phiếu chuyển tiếp tư vấn (Ticket Bàn Quyết Định).
/// Phục vụ hiển thị trên trang Admin Escalations Desk.
/// </summary>
public sealed record EscalationTicketDto(
    long Id,
    string Code,
    string CustomerName,
    string CustomerPhone,
    string Channel,
    string TourTitle,
    int DiscountPercent,
    int GroupSize,
    string Notes,
    string Status,
    string TimeAgo,
    DateTime? CreatedAt,
    long? ConversationId,
    long? TravelId
);

/// <summary>
/// DTO chứa danh sách tickets kèm số lượng thống kê theo từng trạng thái.
/// </summary>
public sealed record EscalationsListResponseDto(
    IReadOnlyList<EscalationTicketDto> Items,
    int TotalCount,
    int PendingCount,
    int ContactingCount,
    int ApprovedCount
);

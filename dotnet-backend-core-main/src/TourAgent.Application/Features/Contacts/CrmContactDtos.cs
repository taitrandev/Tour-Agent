namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// DTO đại diện cho thông tin một khách hàng CRM hiển thị trên danh sách bảng và báo cáo.
/// </summary>
public sealed record CrmContactDto(
    long Id,
    string? Name,
    string? Phone,
    string? Email,
    string? TelegramChatId,
    string? TelegramUsername,
    string? ZaloId,
    string Channel,
    string Stage,
    IReadOnlyList<string>? Tags,
    string? Notes,
    DateTime? LastActiveAt,
    int ConversationsCount,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// DTO tóm tắt một phiên hội thoại thuộc về khách hàng trong popup chi tiết 360°.
/// </summary>
public sealed record ContactConversationDto(
    long Id,
    string Channel,
    string ChannelUserId,
    string Status,
    DateTime? LastMessageAt,
    int MessagesCount);

/// <summary>
/// DTO chi tiết toàn diện của một khách hàng (CRM 360°) kèm lịch sử tất cả phiên chat.
/// </summary>
public sealed record CrmContactDetailDto(
    long Id,
    string? Name,
    string? Phone,
    string? Email,
    string? TelegramChatId,
    string? TelegramUsername,
    string? ZaloId,
    string Channel,
    string Stage,
    IReadOnlyList<string>? Tags,
    string? Notes,
    DateTime? LastActiveAt,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ContactConversationDto> Conversations);

/// <summary>
/// DTO thông tin phân trang chuẩn theo định dạng camelCase.
/// </summary>
public sealed record PaginationMetaDto(
    int CurrentPage,
    int LastPage,
    int PerPage,
    int Total);

/// <summary>
/// DTO phản hồi danh sách khách hàng kèm phân trang.
/// </summary>
public sealed record ContactListResponse(
    bool Success,
    IReadOnlyList<CrmContactDto> Data,
    PaginationMetaDto Pagination);

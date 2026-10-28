using System.Text.Json.Serialization;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// DTO đại diện cho thông tin một khách hàng CRM hiển thị trên danh sách bảng và báo cáo.
/// </summary>
public sealed record CrmContactDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("telegram_chat_id")] string? TelegramChatId,
    [property: JsonPropertyName("telegram_username")] string? TelegramUsername,
    [property: JsonPropertyName("zalo_id")] string? ZaloId,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("last_active_at")] DateTime? LastActiveAt,
    [property: JsonPropertyName("conversations_count")] int ConversationsCount,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt);

/// <summary>
/// DTO tóm tắt một phiên hội thoại thuộc về khách hàng trong popup chi tiết 360°.
/// </summary>
public sealed record ContactConversationDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("channel_user_id")] string ChannelUserId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("last_message_at")] DateTime? LastMessageAt,
    [property: JsonPropertyName("messages_count")] int MessagesCount);

/// <summary>
/// DTO chi tiết toàn diện của một khách hàng (CRM 360°) kèm lịch sử tất cả phiên chat.
/// </summary>
public sealed record CrmContactDetailDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("telegram_chat_id")] string? TelegramChatId,
    [property: JsonPropertyName("telegram_username")] string? TelegramUsername,
    [property: JsonPropertyName("zalo_id")] string? ZaloId,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("last_active_at")] DateTime? LastActiveAt,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("conversations")] IReadOnlyList<ContactConversationDto> Conversations);

/// <summary>
/// DTO thông tin phân trang chuẩn tương thích API PHP Laravel.
/// </summary>
public sealed record PaginationMetaDto(
    [property: JsonPropertyName("current_page")] int CurrentPage,
    [property: JsonPropertyName("last_page")] int LastPage,
    [property: JsonPropertyName("per_page")] int PerPage,
    [property: JsonPropertyName("total")] int Total);

/// <summary>
/// DTO phản hồi danh sách khách hàng kèm phân trang.
/// </summary>
public sealed record ContactListResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] IReadOnlyList<CrmContactDto> Data,
    [property: JsonPropertyName("pagination")] PaginationMetaDto Pagination);

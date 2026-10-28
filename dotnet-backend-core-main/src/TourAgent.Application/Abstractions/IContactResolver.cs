using TourAgent.Domain.Contacts;
using TourAgent.Domain.Conversations;

namespace TourAgent.Application.Abstractions;

/// <summary>
/// Hợp đồng nhận diện, bóc tách và đồng bộ hồ sơ khách hàng CRM từ các kênh tin nhắn (Telegram, Web, Zalo).
/// Tự động quét số điện thoại Việt Nam trong tin nhắn để thăng hạng Hot Lead tức thì.
/// </summary>
public interface IContactResolver
{
    /// <summary>
    /// Nhận diện hoặc tạo mới CrmContact, bóc tách SĐT và gắn liên kết với Conversation.
    /// </summary>
    /// <param name="channel">Kênh giao tiếp (telegram, web_widget, zalo).</param>
    /// <param name="channelUserId">Mã định danh người dùng trên kênh.</param>
    /// <param name="text">Nội dung tin nhắn khách hàng gửi.</param>
    /// <param name="senderName">Tên hiển thị của khách hàng.</param>
    /// <param name="username">Username tài khoản (ví dụ trên Telegram không kèm @).</param>
    /// <param name="conversation">Phiên hội thoại cần liên kết.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Hồ sơ CrmContact đã được đồng bộ.</returns>
    Task<CrmContact> ResolveAndSyncContactAsync(
        string channel,
        string channelUserId,
        string? text,
        string? senderName,
        string? username,
        Conversation conversation,
        CancellationToken cancellationToken = default);
}

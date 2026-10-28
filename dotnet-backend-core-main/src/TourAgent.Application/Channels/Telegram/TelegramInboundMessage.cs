namespace TourAgent.Application.Channels.Telegram;

/// <summary>
/// Dữ liệu tin nhắn gửi từ Telegram được đẩy vào hàng đợi bất đồng bộ In-Process Channel.
/// </summary>
/// <param name="ChatId">Mã phòng chat Telegram của khách hàng.</param>
/// <param name="Text">Nội dung câu hỏi hoặc yêu cầu của khách hàng.</param>
/// <param name="ChannelMessageId">Mã định danh tin nhắn ngoại bộ phục vụ kiểm tra chống trùng lặp (Idempotency).</param>
/// <param name="SenderName">Tên hiển thị của người gửi tin nhắn.</param>
/// <param name="Username">Tên tài khoản Telegram (nếu có).</param>
public sealed record TelegramInboundMessage(
    string ChatId,
    string Text,
    string ChannelMessageId,
    string? SenderName = null,
    string? Username = null);

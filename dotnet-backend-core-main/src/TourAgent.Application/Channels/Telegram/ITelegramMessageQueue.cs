namespace TourAgent.Application.Channels.Telegram;

/// <summary>
/// Hàng đợi tin nhắn In-Memory bất đồng bộ sử dụng System.Threading.Channels
/// để xử lý tin nhắn Telegram ngầm mà không làm chậm thời gian phản hồi Webhook HTTP (Zero-latency ack).
/// </summary>
public interface ITelegramMessageQueue
{
    /// <summary>
    /// Đẩy tin nhắn mới nhận từ Webhook vào hàng đợi xử lý ngầm.
    /// </summary>
    /// <param name="message">Dữ liệu tin nhắn Telegram cần xử lý.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>ValueTask hoàn thành khi tin nhắn được đưa vào hàng đợi.</returns>
    ValueTask EnqueueAsync(
        TelegramInboundMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy luồng đọc tin nhắn liên tục cho Background Worker.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Luồng đọc bất đồng bộ IAsyncEnumerable các tin nhắn trong hàng đợi.</returns>
    IAsyncEnumerable<TelegramInboundMessage> ReadAllAsync(
        CancellationToken cancellationToken = default);
}

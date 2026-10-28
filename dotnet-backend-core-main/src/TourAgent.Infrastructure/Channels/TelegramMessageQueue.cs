using System.Threading.Channels;
using TourAgent.Application.Channels.Telegram;

namespace TourAgent.Infrastructure.Channels;

/// <summary>
/// Thực thi hàng đợi tin nhắn Telegram bất đồng bộ dựa trên System.Threading.Channels (Thread-safe, Lock-free).
/// </summary>
public sealed class TelegramMessageQueue : ITelegramMessageQueue
{
    private readonly Channel<TelegramInboundMessage> _channel;

    /// <summary>
    /// Khởi tạo hàng đợi không giới hạn (Unbounded Channel) với SingleReader tối ưu.
    /// </summary>
    public TelegramMessageQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateUnbounded<TelegramInboundMessage>(options);
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(
        TelegramInboundMessage message,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TelegramInboundMessage> ReadAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

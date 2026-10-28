using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Channels.Telegram;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller tiếp nhận Webhook từ Telegram Bot API và đẩy vào hàng đợi bất đồng bộ (Zero-latency Ack).
/// Đảm bảo phản hồi HTTP 200 tức thì cho máy chủ Telegram để tránh tình trạng retry hoặc timeout.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/telegram")]
[Route("api/v1/agent/webhook/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramMessageQueue _queue;
    private readonly ILogger<TelegramWebhookController> _logger;

    /// <summary>
    /// Khởi tạo Controller với hàng đợi tin nhắn Telegram Channel.
    /// </summary>
    /// <param name="queue">Hàng đợi In-Process Channel.</param>
    /// <param name="logger">Logger ghi nhận sự kiện Webhook.</param>
    public TelegramWebhookController(
        ITelegramMessageQueue queue,
        ILogger<TelegramWebhookController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Tiếp nhận sự kiện cập nhật (Update) từ Telegram Webhook và đẩy vào hàng đợi xử lý ngầm.
    /// </summary>
    /// <param name="payload">Nội dung JSON của Telegram Update.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>HTTP 200 OK với {"ok": true}.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleUpdateAsync(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        // Kiểm tra xem payload có chứa tin nhắn văn bản không
        if (payload.TryGetProperty("message", out var msgElement))
        {
            if (msgElement.TryGetProperty("text", out var textProp) &&
                textProp.ValueKind == JsonValueKind.String &&
                msgElement.TryGetProperty("chat", out var chatProp) &&
                chatProp.TryGetProperty("id", out var chatIdProp))
            {
                var text = textProp.GetString() ?? string.Empty;
                var chatId = chatIdProp.ToString();

                string rawMessageId = Guid.NewGuid().ToString("N");
                if (msgElement.TryGetProperty("message_id", out var msgIdProp))
                {
                    rawMessageId = msgIdProp.ToString();
                }

                string? senderName = null;
                string? username = null;
                if (msgElement.TryGetProperty("from", out var fromProp))
                {
                    if (fromProp.TryGetProperty("username", out var unProp))
                    {
                        username = unProp.GetString();
                    }

                    if (fromProp.TryGetProperty("first_name", out var fnProp))
                    {
                        senderName = fnProp.GetString();
                        if (fromProp.TryGetProperty("last_name", out var lnProp))
                        {
                            var ln = lnProp.GetString();
                            if (!string.IsNullOrWhiteSpace(ln))
                            {
                                senderName = $"{senderName} {ln}";
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(username))
                    {
                        senderName = username;
                    }
                }

                var channelMessageId = $"tg_{chatId}_{rawMessageId}";

                var inbound = new TelegramInboundMessage(
                    ChatId: chatId,
                    Text: text,
                    ChannelMessageId: channelMessageId,
                    SenderName: senderName,
                    Username: username);

                _logger.LogInformation("Đã tiếp nhận Webhook Telegram từ ChatId {ChatId}: {Text}", chatId, text);

                // Đẩy vào hàng đợi ngầm mà không block response
                await _queue.EnqueueAsync(inbound, cancellationToken);
            }
        }

        // Luôn trả về 200 OK ngay lập tức cho máy chủ Telegram
        return Ok(new { ok = true });
    }

    /// <summary>
    /// Kiểm tra trạng thái hoạt động của Telegram Webhook Endpoint.
    /// </summary>
    /// <returns>HTTP 200 OK kèm thông tin trạng thái.</returns>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "online",
            channel = "telegram",
            timestamp = DateTime.UtcNow
        });
    }
}

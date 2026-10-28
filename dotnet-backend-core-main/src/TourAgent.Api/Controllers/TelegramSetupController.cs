using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Abstractions;

namespace TourAgent.Api.Controllers;

/// <summary>
/// DTO yêu cầu lưu Bot Token mới.
/// </summary>
/// <param name="BotToken">Chuỗi token do @BotFather cấp.</param>
public sealed record SaveTokenRequest(string BotToken);

/// <summary>
/// DTO yêu cầu đăng ký Webhook URL.
/// </summary>
/// <param name="WebhookUrl">Đường dẫn URL HTTPS công khai.</param>
public sealed record SetWebhookRequest(string WebhookUrl);

/// <summary>
/// DTO yêu cầu lưu Admin Chat ID nhận duyệt deal.
/// </summary>
/// <param name="AdminChatId">Mã Chat ID hoặc Group ID Telegram.</param>
public sealed record SaveAdminChatIdRequest(string AdminChatId);

/// <summary>
/// DTO yêu cầu gửi tin nhắn text thử nghiệm.
/// </summary>
/// <param name="ChatId">Mã Chat ID người nhận.</param>
/// <param name="Text">Nội dung tin nhắn cần gửi.</param>
public sealed record TestMessageRequest(string ChatId, string? Text = null);

/// <summary>
/// DTO yêu cầu gửi tin nhắn duyệt deal mẫu kèm Inline Keyboard.
/// </summary>
/// <param name="AdminChatId">Mã Chat ID chỉ định (hoặc lấy từ cấu hình mặc định).</param>
public sealed record TestEscalationRequest(string? AdminChatId = null);

/// <summary>
/// Controller quản lý cấu hình kết nối, thiết lập Webhook và kiểm tra tương tác Telegram Bot (tương thích 100% logic PHP).
/// </summary>
[ApiController]
[Route("api/v1/agent/telegram")]
public sealed class TelegramSetupController : ControllerBase
{
    private readonly ITelegramAdapter _telegramAdapter;
    private readonly ITelegramSettingsStore _settingsStore;
    private readonly ILogger<TelegramSetupController> _logger;

    /// <summary>
    /// Khởi tạo TelegramSetupController với các dịch vụ hạ tầng cần thiết.
    /// </summary>
    /// <param name="telegramAdapter">Cổng giao tiếp Telegram Bot API.</param>
    /// <param name="settingsStore">Cửa hàng lưu trữ cấu hình Telegram.</param>
    /// <param name="logger">Logger ghi nhận sự kiện.</param>
    public TelegramSetupController(
        ITelegramAdapter telegramAdapter,
        ITelegramSettingsStore settingsStore,
        ILogger<TelegramSetupController> logger)
    {
        _telegramAdapter = telegramAdapter;
        _settingsStore = settingsStore;
        _logger = logger;
    }

    /// <summary>
    /// Lấy trạng thái cấu hình Telegram Bot, thông tin tài khoản Bot và dữ liệu Webhook hiện tại.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Trạng thái cấu hình Telegram và thông số Webhook.</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var settings = _settingsStore.GetSettings();
        var hasToken = !string.IsNullOrWhiteSpace(settings.BotToken);

        object? botInfo = null;
        object? webhookInfo = null;
        var maskedToken = hasToken ? MaskToken(settings.BotToken) : string.Empty;

        if (hasToken)
        {
            var meResult = await _telegramAdapter.GetMeAsync(cancellationToken: cancellationToken);
            botInfo = new
            {
                ok = meResult.Ok,
                description = meResult.Description,
                result = meResult.Result
            };

            var whResult = await _telegramAdapter.GetWebhookInfoAsync(cancellationToken: cancellationToken);
            webhookInfo = new
            {
                ok = whResult.Ok,
                description = whResult.Description,
                result = whResult.Result
            };
        }

        var request = HttpContext.Request;
        var suggestedWebhookUrl = $"{request.Scheme}://{request.Host}/api/v1/agent/webhook/telegram";

        return Ok(new
        {
            configured = hasToken,
            bot_token_masked = maskedToken,
            bot_info = botInfo,
            webhook_info = webhookInfo,
            suggested_webhook_url = !string.IsNullOrWhiteSpace(settings.WebhookUrl) ? settings.WebhookUrl : suggestedWebhookUrl,
            admin_chat_id = settings.AdminChatId
        });
    }

    /// <summary>
    /// Lưu Telegram Bot Token mới và kiểm tra tính hợp lệ qua getMe với Telegram API.
    /// </summary>
    /// <param name="request">Chứa chuỗi bot_token.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả xác thực và lưu trữ.</returns>
    [HttpPost("save-token")]
    public async Task<IActionResult> SaveTokenAsync(
        [FromBody] SaveTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BotToken) || request.BotToken.Trim().Length < 15)
        {
            return BadRequest(new { success = false, message = "Telegram Bot Token không hợp lệ (tối thiểu 15 ký tự)." });
        }

        var trimmedToken = request.BotToken.Trim();

        // 1. Kiểm tra tính hợp lệ của token với Telegram API
        var testResult = await _telegramAdapter.GetMeAsync(trimmedToken, cancellationToken);
        if (!testResult.Ok)
        {
            var errorDesc = testResult.Description ?? "Token không hợp lệ hoặc Telegram không phản hồi.";
            return UnprocessableEntity(new
            {
                success = false,
                message = $"Không thể kết nối với Telegram Bot: {errorDesc}"
            });
        }

        // 2. Lưu token vào kho cấu hình bền vững
        await _settingsStore.UpdateBotTokenAsync(trimmedToken, cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Lưu Telegram Bot Token thành công!",
            bot_info = new
            {
                ok = true,
                result = testResult.Result
            }
        });
    }

    /// <summary>
    /// Đăng ký Webhook URL công khai cho Telegram Bot.
    /// </summary>
    /// <param name="request">Chứa đường dẫn webhook_url.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả đăng ký webhook.</returns>
    [HttpPost("set-webhook")]
    public async Task<IActionResult> SetWebhookAsync(
        [FromBody] SetWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WebhookUrl) || !Uri.TryCreate(request.WebhookUrl, UriKind.Absolute, out _))
        {
            return BadRequest(new { success = false, message = "Đường dẫn Webhook URL không đúng định dạng hợp lệ." });
        }

        var webhookUrl = request.WebhookUrl.Trim();
        var result = await _telegramAdapter.SetWebhookAsync(webhookUrl, cancellationToken);

        if (!result.Ok)
        {
            var errorDesc = result.Description ?? "Lỗi khi cài đặt Webhook.";
            return UnprocessableEntity(new
            {
                success = false,
                message = $"Telegram báo lỗi: {errorDesc}"
            });
        }

        await _settingsStore.UpdateWebhookUrlAsync(webhookUrl, cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Kích hoạt Webhook Telegram thành công!",
            result = result.Description
        });
    }

    /// <summary>
    /// Hủy bỏ Webhook Telegram và xóa các cập nhật đang chờ xử lý.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả gỡ bỏ Webhook.</returns>
    [HttpPost("delete-webhook")]
    public async Task<IActionResult> DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        var result = await _telegramAdapter.DeleteWebhookAsync(dropPendingUpdates: true, cancellationToken);

        if (!result.Ok)
        {
            var errorDesc = result.Description ?? "Lỗi khi hủy Webhook.";
            return UnprocessableEntity(new
            {
                success = false,
                message = $"Telegram báo lỗi: {errorDesc}"
            });
        }

        await _settingsStore.UpdateWebhookUrlAsync(string.Empty, cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Đã hủy Webhook Telegram thành công!",
            result = result.Description
        });
    }

    /// <summary>
    /// Lưu Admin Chat ID của Quản lý nhận thông báo duyệt deal qua Telegram.
    /// </summary>
    /// <param name="request">Chứa admin_chat_id.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả lưu Chat ID.</returns>
    [HttpPost("save-admin-chat-id")]
    public async Task<IActionResult> SaveAdminChatIdAsync(
        [FromBody] SaveAdminChatIdRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdminChatId))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập Admin Chat ID." });
        }

        var chatId = request.AdminChatId.Trim();
        await _settingsStore.UpdateAdminChatIdAsync(chatId, cancellationToken);

        return Ok(new
        {
            success = true,
            message = $"Lưu Admin Chat ID [{chatId}] thành công!",
            admin_chat_id = chatId
        });
    }

    /// <summary>
    /// Gửi một tin nhắn văn bản thử nghiệm tới Chat ID chỉ định.
    /// </summary>
    /// <param name="request">Chứa chat_id và nội dung text tùy chọn.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả gửi tin nhắn.</returns>
    [HttpPost("test-message")]
    public async Task<IActionResult> TestMessageAsync(
        [FromBody] TestMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChatId))
        {
            return BadRequest(new { success = false, message = "Vui lòng cung cấp Chat ID." });
        }

        var chatId = request.ChatId.Trim();
        var text = !string.IsNullOrWhiteSpace(request.Text)
            ? request.Text
            : "👋 Chào bạn! Đây là tin nhắn thử nghiệm từ hệ thống AI Agent Travel.\n🤖 Bot tư vấn tour 24/7 đã kết nối thành công!";

        var success = await _telegramAdapter.SendTextMessageAsync(chatId, text, cancellationToken);

        if (!success)
        {
            return UnprocessableEntity(new
            {
                success = false,
                message = "Lỗi khi gửi tin nhắn Telegram. Vui lòng kiểm tra lại Chat ID hoặc Bot Token."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Đã gửi tin nhắn thử nghiệm thành công! Hãy kiểm tra Telegram của bạn."
        });
    }

    /// <summary>
    /// Gửi thử nghiệm tin nhắn duyệt deal mẫu kèm 2 nút bấm Inline Keyboard [✅ Duyệt] và [❌ Từ chối].
    /// </summary>
    /// <param name="request">Chứa admin_chat_id tùy chọn.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả gửi tin nhắn duyệt deal mẫu.</returns>
    [HttpPost("test-escalation")]
    public async Task<IActionResult> TestEscalationAsync(
        [FromBody] TestEscalationRequest request,
        CancellationToken cancellationToken)
    {
        var chatId = !string.IsNullOrWhiteSpace(request.AdminChatId)
            ? request.AdminChatId.Trim()
            : _settingsStore.GetSettings().AdminChatId.Trim();

        if (string.IsNullOrWhiteSpace(chatId))
        {
            return UnprocessableEntity(new
            {
                success = false,
                message = "Vui lòng nhập hoặc lưu Admin Chat ID trước khi thử nghiệm."
            });
        }

        var sampleText = "🛎️ *[THỬ NGHIỆM] YÊU CẦU DUYỆT DEAL ĐOÀN MẪU*\n\n"
            + "👥 *Quy mô đoàn:* 20 khách (Đại gia đình)\n"
            + "🏷️ *Đề xuất giảm:* 10%\n"
            + "✈️ *Tour:* Cung Đường Vàng Nhật Bản 6N5Đ\n"
            + "📝 *Ghi chú:* Khách hỏi ưu đãi đoàn đi tháng 10 ngắm lá đỏ.\n\n"
            + "_Đây là tin nhắn kiểm tra tính năng Human-in-the-Loop. Vui lòng bấm thử nút bên dưới:_";

        var buttons = new[]
        {
            new[]
            {
                new { text = "✅ Duyệt giảm 10% (Test)", callback_data = "deal:test_approve" },
                new { text = "❌ Giữ nguyên giá (Test)", callback_data = "deal:test_reject" }
            }
        };

        var result = await _telegramAdapter.SendMessageWithInlineKeyboardAsync(chatId, sampleText, buttons, cancellationToken);

        if (!result.Ok)
        {
            return UnprocessableEntity(new
            {
                success = false,
                message = $"Lỗi gửi tin nhắn duyệt mẫu: {result.Description ?? "Không gửi được tin nhắn."}"
            });
        }

        return Ok(new
        {
            success = true,
            message = $"Đã gửi tin nhắn duyệt deal mẫu thành công tới Chat ID {chatId}! Hãy kiểm tra Telegram."
        });
    }

    /// <summary>
    /// Che bớt các ký tự của token Telegram để bảo vệ tính bảo mật khi hiển thị trên giao diện.
    /// </summary>
    /// <param name="token">Chuỗi token đầy đủ.</param>
    /// <returns>Chuỗi token đã che mờ (ví dụ: 8195***:AAEN***ekAM).</returns>
    private static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 15)
        {
            return "********";
        }

        var parts = token.Split(':', 2);
        if (parts.Length == 2)
        {
            var botId = parts[0].Length > 4 ? parts[0][..4] + "***" : parts[0] + "***";
            var secret = parts[1].Length > 8
                ? parts[1][..4] + "***" + parts[1][^4..]
                : parts[1][..2] + "***";

            return $"{botId}:{secret}";
        }

        return token[..6] + "***" + token[^4..];
    }
}

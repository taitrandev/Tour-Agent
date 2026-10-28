using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Telegram;

/// <summary>
/// Adapter tích hợp trực tiếp với Telegram Bot API chính thức qua HTTP Client.
/// Hỗ trợ kiểm tra danh tính bot (getMe), cấu hình webhook, gửi tin nhắn văn bản, ảnh và nút bấm Inline Keyboard.
/// </summary>
public sealed class TelegramAdapter : ITelegramAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ITelegramSettingsStore _settingsStore;
    private readonly ILogger<TelegramAdapter> _logger;

    /// <summary>
    /// Khởi tạo TelegramAdapter với HttpClient, ITelegramSettingsStore và ILogger.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient để gửi yêu cầu mạng.</param>
    /// <param name="settingsStore">Cửa hàng lưu trữ và quản lý cấu hình Telegram.</param>
    /// <param name="logger">Logger ghi nhận thông tin hoạt động.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public TelegramAdapter(
        HttpClient httpClient,
        ITelegramSettingsStore settingsStore,
        ILogger<TelegramAdapter> logger)
    {
        _httpClient = httpClient;
        _settingsStore = settingsStore;
        _logger = logger;
    }

    /// <summary>
    /// Constructor hỗ trợ tương thích ngược với IConfiguration cho Unit Test.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient.</param>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <param name="logger">Logger ghi log.</param>
    public TelegramAdapter(
        HttpClient httpClient,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<TelegramAdapter> logger)
        : this(httpClient, new TelegramSettingsStore(configuration, Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramSettingsStore>.Instance), logger)
    {
    }

    /// <summary>
    /// Tạo URL cơ sở tới Telegram Bot API dựa trên token đang kích hoạt hoặc token chỉ định.
    /// </summary>
    /// <param name="overrideToken">Token ghi đè tạm thời nếu có.</param>
    /// <returns>Chuỗi Base URL Telegram API.</returns>
    private string GetBaseUrl(string? overrideToken = null)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken)
            ? overrideToken.Trim()
            : _settingsStore.GetSettings().BotToken.Trim();

        return string.IsNullOrWhiteSpace(token)
            ? string.Empty
            : $"https://api.telegram.org/bot{token}";
    }

    /// <inheritdoc />
    public async Task<bool> SendTextMessageAsync(
        string chatId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogInformation("[Mô phỏng Telegram] Gửi tin nhắn tới ChatId {ChatId}: {Text}", chatId, text);
            return true;
        }

        var endpoint = $"{baseUrl}/sendMessage";

        var payloadWithMarkdown = new
        {
            chat_id = chatId,
            text,
            parse_mode = "Markdown"
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payloadWithMarkdown),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Telegram gửi Markdown thất bại ({StatusCode}). Đang gửi lại dạng plain-text...", response.StatusCode);

            var plainPayload = new
            {
                chat_id = chatId,
                text
            };

            var plainJson = new StringContent(
                JsonSerializer.Serialize(plainPayload),
                Encoding.UTF8,
                "application/json");

            var retryResponse = await _httpClient.PostAsync(endpoint, plainJson, cancellationToken);
            return retryResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ khi gọi Telegram Bot API gửi tin nhắn tới {ChatId}", chatId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendPhotoAsync(
        string chatId,
        string photoUrl,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogInformation("[Mô phỏng Telegram] Gửi ảnh tới ChatId {ChatId}: Url={PhotoUrl}, Caption={Caption}", chatId, photoUrl, caption);
            return true;
        }

        var endpoint = $"{baseUrl}/sendPhoto";
        var payload = new
        {
            chat_id = chatId,
            photo = photoUrl,
            caption
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ khi gọi Telegram Bot API gửi ảnh tới {ChatId}", chatId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<TelegramOperationResult> GetMeAsync(
        string? overrideToken = null,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl(overrideToken);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new TelegramOperationResult(false, "Chưa cấu hình Telegram Bot Token.");
        }

        var endpoint = $"{baseUrl}/getMe";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var isOk = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var desc = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            if (isOk && doc.RootElement.TryGetProperty("result", out var resultProp))
            {
                return new TelegramOperationResult(true, null, resultProp.Clone());
            }

            return new TelegramOperationResult(false, desc ?? "Không thể lấy thông tin Bot từ Telegram.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi getMe từ Telegram API");
            return new TelegramOperationResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<TelegramOperationResult> GetWebhookInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new TelegramOperationResult(false, "Chưa cấu hình Telegram Bot Token.");
        }

        var endpoint = $"{baseUrl}/getWebhookInfo";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var isOk = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var desc = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            if (isOk && doc.RootElement.TryGetProperty("result", out var resultProp))
            {
                return new TelegramOperationResult(true, null, resultProp.Clone());
            }

            return new TelegramOperationResult(false, desc ?? "Không thể lấy thông tin Webhook.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi getWebhookInfo từ Telegram API");
            return new TelegramOperationResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<TelegramOperationResult> SetWebhookAsync(
        string webhookUrl,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new TelegramOperationResult(false, "Chưa cấu hình Telegram Bot Token.");
        }

        var endpoint = $"{baseUrl}/setWebhook";
        var payload = new { url = webhookUrl };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var isOk = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var desc = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            if (isOk)
            {
                return new TelegramOperationResult(true, desc ?? "Kích hoạt Webhook thành công.");
            }

            return new TelegramOperationResult(false, desc ?? "Cài đặt Webhook thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cài đặt Webhook Telegram: {Url}", webhookUrl);
            return new TelegramOperationResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<TelegramOperationResult> DeleteWebhookAsync(
        bool dropPendingUpdates = true,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new TelegramOperationResult(false, "Chưa cấu hình Telegram Bot Token.");
        }

        var endpoint = $"{baseUrl}/deleteWebhook";
        var payload = new { drop_pending_updates = dropPendingUpdates };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var isOk = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var desc = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            if (isOk)
            {
                return new TelegramOperationResult(true, desc ?? "Đã gỡ Webhook Telegram thành công.");
            }

            return new TelegramOperationResult(false, desc ?? "Gỡ Webhook thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi deleteWebhook từ Telegram API");
            return new TelegramOperationResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<TelegramOperationResult> SendMessageWithInlineKeyboardAsync(
        string chatId,
        string text,
        object inlineKeyboard,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogInformation("[Mô phỏng Telegram] Gửi tin nhắn duyệt deal kèm Inline Keyboard tới {ChatId}: {Text}", chatId, text);
            return new TelegramOperationResult(true, "Mô phỏng gửi tin duyệt deal thành công.");
        }

        var endpoint = $"{baseUrl}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text,
            parse_mode = "Markdown",
            reply_markup = new
            {
                inline_keyboard = inlineKeyboard
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var isOk = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var desc = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            if (isOk && doc.RootElement.TryGetProperty("result", out var resultProp))
            {
                return new TelegramOperationResult(true, "Đã gửi tin nhắn duyệt deal thành công!", resultProp.Clone());
            }

            return new TelegramOperationResult(false, desc ?? "Không gửi được tin nhắn duyệt deal.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi tin nhắn Inline Keyboard tới Telegram Chat {ChatId}", chatId);
            return new TelegramOperationResult(false, ex.Message);
        }
    }
}

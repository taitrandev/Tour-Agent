namespace TourAgent.Application.Abstractions;

/// <summary>
/// Chứa thông tin cấu hình kết nối Telegram Bot, Webhook và Admin Chat ID.
/// </summary>
public sealed class TelegramSettings
{
    /// <summary>
    /// Mã token bot do Telegram @BotFather cấp.
    /// </summary>
    public string BotToken { get; set; } = "8195824138:AAENOM5ujl4bzs9ceixW7wIggDFY8OHekAM";

    /// <summary>
    /// Đường dẫn Webhook URL công khai (Cloudflare Tunnel / Ngrok).
    /// </summary>
    public string WebhookUrl { get; set; } = "https://20a3-2001-ee0-4f0d-b5e0-500a-61bd-1f9a-bc5a.ngrok-free.app/api/v1/agent/webhook/telegram";

    /// <summary>
    /// Mã Chat ID hoặc Group ID Telegram của Quản lý nhận thông báo duyệt deal đoàn.
    /// </summary>
    public string AdminChatId { get; set; } = "5809306571";
}

/// <summary>
/// Interface quản lý lưu trữ và nạp cấu hình Telegram bền vững.
/// </summary>
public interface ITelegramSettingsStore
{
    /// <summary>
    /// Lấy bản sao cấu hình Telegram hiện tại.
    /// </summary>
    TelegramSettings GetSettings();

    /// <summary>
    /// Cập nhật Telegram Bot Token mới và lưu vào file cấu hình.
    /// </summary>
    Task UpdateBotTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Webhook URL và lưu vào file cấu hình.
    /// </summary>
    Task UpdateWebhookUrlAsync(string webhookUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Admin Chat ID và lưu vào file cấu hình.
    /// </summary>
    Task UpdateAdminChatIdAsync(string adminChatId, CancellationToken cancellationToken = default);
}

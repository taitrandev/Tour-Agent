using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Telegram;

/// <summary>
/// Quản lý lưu trữ cấu hình Telegram vào file JSON cục bộ bảo đảm dữ liệu luôn được ghi nhớ bền vững.
/// </summary>
public sealed class TelegramSettingsStore : ITelegramSettingsStore
{
    private readonly string _filePath;
    private readonly ILogger<TelegramSettingsStore> _logger;
    private TelegramSettings _settings;
    private readonly object _lock = new();

    /// <summary>
    /// Khởi tạo TelegramSettingsStore, nạp cấu hình từ file hoặc gán giá trị mặc định.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    public TelegramSettingsStore(IConfiguration configuration, ILogger<TelegramSettingsStore> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppContext.BaseDirectory, "telegram_settings.json");
        _settings = LoadSettings(configuration);
    }

    /// <summary>
    /// Nạp cấu hình từ file JSON hoặc từ biến môi trường/mặc định ban đầu.
    /// </summary>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <returns>Đối tượng TelegramSettings đã nạp.</returns>
    private TelegramSettings LoadSettings(IConfiguration configuration)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var content = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<TelegramSettings>(content);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc file {FilePath}, đang sử dụng cấu hình mặc định.", _filePath);
        }

        var token = configuration["Telegram:BotToken"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
            ?? string.Empty;

        var webhook = configuration["Telegram:WebhookUrl"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK_URL")
            ?? string.Empty;

        var adminChat = configuration["Telegram:AdminChatId"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_CHAT_ID")
            ?? string.Empty;

        var defaultSettings = new TelegramSettings
        {
            BotToken = token,
            WebhookUrl = webhook,
            AdminChatId = adminChat
        };

        if (!string.IsNullOrWhiteSpace(token) || !string.IsNullOrWhiteSpace(webhook) || !string.IsNullOrWhiteSpace(adminChat))
        {
            SaveToFile(defaultSettings);
        }

        return defaultSettings;
    }

    /// <summary>
    /// Lưu cấu hình hiện tại ra file JSON.
    /// </summary>
    /// <param name="settings">Đối tượng cấu hình cần lưu.</param>
    private void SaveToFile(TelegramSettings settings)
    {
        lock (_lock)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi cấu hình Telegram vào file {FilePath}", _filePath);
            }
        }
    }

    /// <inheritdoc />
    public TelegramSettings GetSettings()
    {
        lock (_lock)
        {
            return new TelegramSettings
            {
                BotToken = _settings.BotToken,
                WebhookUrl = _settings.WebhookUrl,
                AdminChatId = _settings.AdminChatId
            };
        }
    }

    /// <inheritdoc />
    public Task UpdateBotTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _settings.BotToken = token.Trim();
            SaveToFile(_settings);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateWebhookUrlAsync(string webhookUrl, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _settings.WebhookUrl = webhookUrl.Trim();
            SaveToFile(_settings);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAdminChatIdAsync(string adminChatId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _settings.AdminChatId = adminChatId.Trim();
            SaveToFile(_settings);
        }
        return Task.CompletedTask;
    }
}

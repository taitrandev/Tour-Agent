using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Settings;

namespace TourAgent.Infrastructure.Telegram;

/// <summary>
/// Quản lý lưu trữ cấu hình Telegram vào cơ sở dữ liệu PostgreSQL/Supabase và file JSON cục bộ,
/// bảo đảm dữ liệu luôn được ghi nhớ bền vững kể cả khi container bị restart hoặc scale to zero.
/// </summary>
public sealed class TelegramSettingsStore : ITelegramSettingsStore
{
    private const string SettingKey = "telegram_settings";
    private readonly string _filePath;
    private readonly ILogger<TelegramSettingsStore> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private TelegramSettings _settings;
    private readonly object _lock = new();

    /// <summary>
    /// Khởi tạo TelegramSettingsStore với IServiceScopeFactory để lưu trữ cấu hình bền vững vào CSDL.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    /// <param name="serviceScopeFactory">Factory cấp phát scope truy cập DbContext.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public TelegramSettingsStore(
        IConfiguration configuration,
        ILogger<TelegramSettingsStore> logger,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _filePath = Path.Combine(AppContext.BaseDirectory, "telegram_settings.json");
        _settings = LoadSettings(configuration);
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ kiểm thử đơn vị độc lập.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    public TelegramSettingsStore(
        IConfiguration configuration,
        ILogger<TelegramSettingsStore> logger)
        : this(configuration, logger, null)
    {
    }

    /// <summary>
    /// Nạp cấu hình từ CSDL PostgreSQL, file JSON hoặc từ biến môi trường/mặc định ban đầu.
    /// </summary>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <returns>Đối tượng TelegramSettings đã nạp.</returns>
    private TelegramSettings LoadSettings(IConfiguration configuration)
    {
        // 1. Thử nạp từ CSDL PostgreSQL/Supabase trước
        var loadedFromDb = TryLoadFromDatabase();
        if (loadedFromDb is not null && (!string.IsNullOrWhiteSpace(loadedFromDb.BotToken) || !string.IsNullOrWhiteSpace(loadedFromDb.AdminChatId)))
        {
            _logger.LogInformation("Đã nạp thành công cấu hình Telegram từ CSDL PostgreSQL (Bảng system_settings).");
            return loadedFromDb;
        }

        // 2. Thử nạp từ file JSON cục bộ
        TelegramSettings? loadedFromFile = null;
        try
        {
            if (File.Exists(_filePath))
            {
                var content = File.ReadAllText(_filePath);
                loadedFromFile = JsonSerializer.Deserialize<TelegramSettings>(content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc file {FilePath}, đang sử dụng cấu hình mặc định.", _filePath);
        }

        var source = loadedFromDb ?? loadedFromFile;

        var token = configuration["Telegram:BotToken"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        var webhook = configuration["Telegram:WebhookUrl"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK_URL");

        var adminChat = configuration["Telegram:AdminChatId"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_CHAT_ID");

        var resolved = new TelegramSettings
        {
            BotToken = !string.IsNullOrWhiteSpace(token) ? token : (source?.BotToken ?? string.Empty),
            WebhookUrl = !string.IsNullOrWhiteSpace(webhook) ? webhook : (source?.WebhookUrl ?? string.Empty),
            AdminChatId = !string.IsNullOrWhiteSpace(adminChat) ? adminChat : (source?.AdminChatId ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(resolved.BotToken) ||
            !string.IsNullOrWhiteSpace(resolved.WebhookUrl) ||
            !string.IsNullOrWhiteSpace(resolved.AdminChatId))
        {
            SaveToFile(resolved);
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveToDatabaseAsync(resolved, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Chưa thể đồng bộ ban đầu cấu hình Telegram vào CSDL.");
                }
            });
        }

        return resolved;
    }

    /// <summary>
    /// Thử đọc cấu hình Telegram từ bảng 'system_settings' trong CSDL PostgreSQL.
    /// </summary>
    /// <returns>Đối tượng TelegramSettings nếu tìm thấy trong CSDL, ngược lại là null.</returns>
    private TelegramSettings? TryLoadFromDatabase()
    {
        if (_serviceScopeFactory is null)
        {
            return null;
        }

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var setting = dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefault(s => s.Key == SettingKey);

            if (setting is not null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                return JsonSerializer.Deserialize<TelegramSettings>(setting.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Chưa thể nạp cấu hình Telegram từ CSDL (bảng system_settings chưa khởi tạo hoặc đang migration).");
        }

        return null;
    }

    /// <summary>
    /// Lưu cấu hình hiện tại ra file JSON cục bộ.
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

    /// <summary>
    /// Lưu cấu hình Telegram vào bảng 'system_settings' trong CSDL PostgreSQL/Supabase.
    /// </summary>
    /// <param name="settings">Đối tượng cấu hình Telegram.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    private async Task SaveToDatabaseAsync(TelegramSettings settings, CancellationToken cancellationToken)
    {
        if (_serviceScopeFactory is null)
        {
            return;
        }

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var jsonText = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            var existingSetting = await dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);

            if (existingSetting is not null)
            {
                existingSetting.UpdateValue(jsonText);
            }
            else
            {
                dbContext.SystemSettings.Add(new SystemSetting(SettingKey, jsonText));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã lưu cấu hình Telegram thành công vào CSDL PostgreSQL (Bảng system_settings).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu cấu hình Telegram vào CSDL PostgreSQL.");
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
    public async Task UpdateBotTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        TelegramSettings snapshot;
        lock (_lock)
        {
            _settings.BotToken = token.Trim();
            snapshot = new TelegramSettings
            {
                BotToken = _settings.BotToken,
                WebhookUrl = _settings.WebhookUrl,
                AdminChatId = _settings.AdminChatId
            };
        }

        SaveToFile(snapshot);
        await SaveToDatabaseAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateWebhookUrlAsync(string webhookUrl, CancellationToken cancellationToken = default)
    {
        TelegramSettings snapshot;
        lock (_lock)
        {
            _settings.WebhookUrl = webhookUrl.Trim();
            snapshot = new TelegramSettings
            {
                BotToken = _settings.BotToken,
                WebhookUrl = _settings.WebhookUrl,
                AdminChatId = _settings.AdminChatId
            };
        }

        SaveToFile(snapshot);
        await SaveToDatabaseAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAdminChatIdAsync(string adminChatId, CancellationToken cancellationToken = default)
    {
        TelegramSettings snapshot;
        lock (_lock)
        {
            _settings.AdminChatId = adminChatId.Trim();
            snapshot = new TelegramSettings
            {
                BotToken = _settings.BotToken,
                WebhookUrl = _settings.WebhookUrl,
                AdminChatId = _settings.AdminChatId
            };
        }

        SaveToFile(snapshot);
        await SaveToDatabaseAsync(snapshot, cancellationToken);
    }
}

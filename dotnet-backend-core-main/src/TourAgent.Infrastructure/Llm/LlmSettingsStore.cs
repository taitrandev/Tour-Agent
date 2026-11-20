using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Settings;

namespace TourAgent.Infrastructure.Llm;

/// <summary>
/// Thực thi lưu trữ và đồng bộ cấu hình LLM vào cơ sở dữ liệu PostgreSQL/Supabase (bền vững trên Cloud)
/// và file JSON cục bộ bảo đảm dữ liệu luôn được ghi nhớ bền vững kể cả khi container restart.
/// </summary>
public sealed class LlmSettingsStore : ILlmSettingsStore
{
    private const string SettingKey = "llm_settings";
    private readonly string _filePath;
    private readonly ILogger<LlmSettingsStore> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private LlmSettings _settings;
    private readonly object _lockObject = new();

    /// <summary>
    /// Khởi tạo LlmSettingsStore với IServiceScopeFactory để lưu trữ bền vững vào CSDL PostgreSQL/Supabase.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng IConfiguration.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    /// <param name="serviceScopeFactory">Factory cấp phát scope truy cập DbContext.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public LlmSettingsStore(
        IConfiguration configuration,
        ILogger<LlmSettingsStore> logger,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _filePath = Path.Combine(AppContext.BaseDirectory, "llm_settings.json");
        _settings = LoadSettings(configuration);
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ kiểm thử đơn vị độc lập.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng IConfiguration.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    public LlmSettingsStore(
        IConfiguration configuration,
        ILogger<LlmSettingsStore> logger)
        : this(configuration, logger, null)
    {
    }

    /// <summary>
    /// Nạp cấu hình từ CSDL PostgreSQL, file JSON cục bộ hoặc từ biến môi trường Cloud.
    /// </summary>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <returns>Đối tượng LlmSettings đã nạp.</returns>
    private LlmSettings LoadSettings(IConfiguration configuration)
    {
        // 1. Thử nạp từ Cơ Sở Dữ Liệu (PostgreSQL / Supabase) trước để bảo đảm tính bền vững trên Cloud
        var loadedFromDb = TryLoadFromDatabase();
        if (loadedFromDb is not null && !string.IsNullOrWhiteSpace(loadedFromDb.ApiKey))
        {
            _logger.LogInformation("Đã nạp thành công cấu hình LLM từ CSDL PostgreSQL: Driver={Driver}, Model={Model}", loadedFromDb.Driver, loadedFromDb.Model);
            return loadedFromDb;
        }

        // 2. Thử nạp từ file JSON cục bộ
        LlmSettings? loadedFromFile = null;
        try
        {
            if (File.Exists(_filePath))
            {
                var fileContent = File.ReadAllText(_filePath);
                loadedFromFile = JsonSerializer.Deserialize<LlmSettings>(fileContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc file {FilePath}, chuyển sang nạp từ biến môi trường.", _filePath);
        }

        var source = loadedFromDb ?? loadedFromFile;

        // 3. Đọc biến môi trường và IConfiguration trên môi trường Cloud
        var explicitDriver = configuration["Llm:Driver"]
            ?? Environment.GetEnvironmentVariable("LLM_DRIVER")
            ?? source?.Driver;

        var claudeKey = configuration["Claude:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");

        var deepseekKey = configuration["DeepSeek:ApiKey"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        var isClaude = string.Equals(explicitDriver, "claude", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(explicitDriver) && !string.IsNullOrWhiteSpace(claudeKey) && string.IsNullOrWhiteSpace(deepseekKey))
            || (source?.Model?.Contains("claude", StringComparison.OrdinalIgnoreCase) == true)
            || (source?.Model?.Contains("haiku", StringComparison.OrdinalIgnoreCase) == true)
            || (source?.BaseUrl?.Contains("fableai", StringComparison.OrdinalIgnoreCase) == true)
            || (source?.ApiKey?.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase) == true);

        var driver = isClaude ? "claude" : "deepseek";

        var apiKey = isClaude
            ? (claudeKey ?? (!string.IsNullOrWhiteSpace(source?.ApiKey) ? source.ApiKey : string.Empty))
            : (deepseekKey ?? (!string.IsNullOrWhiteSpace(source?.ApiKey) ? source.ApiKey : string.Empty));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var keyName = isClaude ? "Claude (ANTHROPIC_AUTH_TOKEN, CLAUDE_API_KEY hoặc ANTHROPIC_API_KEY)" : "DeepSeek (DEEPSEEK_API_KEY)";
            _logger.LogWarning("Chưa cấu hình API Key cho {KeyName}.", keyName);
        }

        var defaultModel = isClaude ? "claude-3-5-haiku-20241022" : "deepseek-reasoner";
        var model = isClaude
            ? (configuration["Claude:Model"] ?? Environment.GetEnvironmentVariable("CLAUDE_MODEL") ?? Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? source?.Model ?? defaultModel)
            : (configuration["DeepSeek:Model"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? source?.Model ?? defaultModel);

        var defaultBaseUrl = isClaude ? "https://api.fableaiapi.com" : "https://api.deepseek.com";
        var baseUrl = isClaude
            ? (configuration["Claude:BaseUrl"] ?? Environment.GetEnvironmentVariable("CLAUDE_BASE_URL") ?? Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? source?.BaseUrl ?? defaultBaseUrl)
            : (configuration["DeepSeek:BaseUrl"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL") ?? source?.BaseUrl ?? defaultBaseUrl);

        float temperature = source?.Temperature ?? 0.2f;
        var tempConfig = isClaude
            ? (configuration["Claude:Temperature"] ?? Environment.GetEnvironmentVariable("CLAUDE_TEMPERATURE"))
            : (configuration["DeepSeek:Temperature"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_TEMPERATURE"));

        if (float.TryParse(tempConfig, out var parsedTemp))
        {
            temperature = parsedTemp;
        }

        var resolved = new LlmSettings
        {
            Driver = driver,
            ApiKey = apiKey,
            Model = model,
            BaseUrl = baseUrl,
            Temperature = temperature
        };

        // Đồng bộ lưu xuống file và CSDL nếu có key
        if (!string.IsNullOrWhiteSpace(resolved.ApiKey))
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
                    _logger.LogDebug(ex, "Chưa thể đồng bộ ban đầu cấu hình LLM vào CSDL.");
                }
            });
        }

        return resolved;
    }

    /// <summary>
    /// Thử đọc cấu hình LLM từ bảng 'system_settings' trong CSDL PostgreSQL.
    /// </summary>
    /// <returns>Đối tượng LlmSettings nếu tìm thấy trong CSDL, ngược lại là null.</returns>
    private LlmSettings? TryLoadFromDatabase()
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
                return JsonSerializer.Deserialize<LlmSettings>(setting.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Chưa thể nạp cấu hình LLM từ CSDL PostgreSQL (có thể do bảng chưa khởi tạo hoặc đang migration).");
        }

        return null;
    }

    /// <summary>
    /// Lưu cấu hình LLM ra file JSON cục bộ.
    /// </summary>
    /// <param name="settings">Đối tượng cấu hình LLM.</param>
    private void SaveToFile(LlmSettings settings)
    {
        try
        {
            var jsonText = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, jsonText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lưu file cấu hình LLM cục bộ: {FilePath}", _filePath);
        }
    }

    /// <summary>
    /// Lưu cấu hình LLM vào bảng 'system_settings' trong CSDL PostgreSQL/Supabase.
    /// </summary>
    /// <param name="settings">Đối tượng cấu hình LLM.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    private async Task SaveToDatabaseAsync(LlmSettings settings, CancellationToken cancellationToken)
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
            _logger.LogInformation("Đã lưu cấu hình LLM thành công vào CSDL PostgreSQL (Bảng system_settings).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu cấu hình LLM vào CSDL PostgreSQL.");
        }
    }

    /// <inheritdoc />
    public LlmSettings GetSettings()
    {
        lock (_lockObject)
        {
            return new LlmSettings
            {
                Driver = _settings.Driver,
                ApiKey = _settings.ApiKey,
                Model = _settings.Model,
                BaseUrl = _settings.BaseUrl,
                Temperature = _settings.Temperature
            };
        }
    }

    /// <inheritdoc />
    public string GetMaskedApiKey()
    {
        lock (_lockObject)
        {
            var key = _settings.ApiKey?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(key) || key.Length <= 8)
            {
                return string.Empty;
            }

            var prefix = key[..4];
            var suffix = key[^4..];
            return $"{prefix}••••••••{suffix}";
        }
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(
        string? apiKey,
        string model,
        string? baseUrl,
        float? temperature,
        string? driver = null,
        CancellationToken cancellationToken = default)
    {
        LlmSettings snapshot;
        lock (_lockObject)
        {
            if (!string.IsNullOrWhiteSpace(driver))
            {
                _settings.Driver = driver.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _settings.ApiKey = apiKey.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                _settings.Model = model.Trim();
            }

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _settings.BaseUrl = baseUrl.Trim().TrimEnd('/');
            }

            if (temperature.HasValue)
            {
                _settings.Temperature = Math.Clamp(temperature.Value, 0.0f, 1.0f);
            }

            snapshot = new LlmSettings
            {
                Driver = _settings.Driver,
                ApiKey = _settings.ApiKey,
                Model = _settings.Model,
                BaseUrl = _settings.BaseUrl,
                Temperature = _settings.Temperature
            };
        }

        // 1. Lưu ra file cục bộ
        SaveToFile(snapshot);

        // 2. Lưu vào CSDL PostgreSQL/Supabase bền vững trên Cloud
        await SaveToDatabaseAsync(snapshot, cancellationToken);
    }
}

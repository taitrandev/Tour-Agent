using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Llm;

/// <summary>
/// Thực thi lưu trữ và đồng bộ cấu hình LLM vào file JSON cục bộ bảo đảm dữ liệu luôn bền vững.
/// </summary>
public sealed class LlmSettingsStore : ILlmSettingsStore
{
    private readonly string _filePath;
    private readonly ILogger<LlmSettingsStore> _logger;
    private LlmSettings _settings;
    private readonly object _lockObject = new();

    /// <summary>
    /// Khởi tạo LlmSettingsStore, nạp cấu hình từ file json hoặc từ appsettings/fallback ban đầu.
    /// </summary>
    /// <param name="configuration">Cấu hình ứng dụng IConfiguration.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    public LlmSettingsStore(IConfiguration configuration, ILogger<LlmSettingsStore> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppContext.BaseDirectory, "llm_settings.json");
        _settings = LoadSettings(configuration);
    }

    /// <summary>
    /// Nạp cấu hình từ file JSON cục bộ hoặc từ cấu hình IConfiguration.
    /// Ưu tiên biến môi trường và IConfiguration trên môi trường Cloud.
    /// </summary>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <returns>Đối tượng LlmSettings đã nạp.</returns>
    private LlmSettings LoadSettings(IConfiguration configuration)
    {
        LlmSettings? loadedSettings = null;
        try
        {
            if (File.Exists(_filePath))
            {
                var fileContent = File.ReadAllText(_filePath);
                loadedSettings = JsonSerializer.Deserialize<LlmSettings>(fileContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc file {FilePath}, đang sử dụng cấu hình mặc định.", _filePath);
        }

        var explicitDriver = configuration["Llm:Driver"]
            ?? Environment.GetEnvironmentVariable("LLM_DRIVER")
            ?? loadedSettings?.Driver;

        var claudeKey = configuration["Claude:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");

        var deepseekKey = configuration["DeepSeek:ApiKey"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        var isClaude = string.Equals(explicitDriver, "claude", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(explicitDriver) && !string.IsNullOrWhiteSpace(claudeKey) && string.IsNullOrWhiteSpace(deepseekKey))
            || (loadedSettings?.Model?.Contains("claude", StringComparison.OrdinalIgnoreCase) == true)
            || (loadedSettings?.Model?.Contains("haiku", StringComparison.OrdinalIgnoreCase) == true)
            || (loadedSettings?.BaseUrl?.Contains("fableai", StringComparison.OrdinalIgnoreCase) == true)
            || (loadedSettings?.ApiKey?.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase) == true);

        var driver = isClaude ? "claude" : "deepseek";

        var apiKey = isClaude
            ? (claudeKey ?? (!string.IsNullOrWhiteSpace(loadedSettings?.ApiKey) ? loadedSettings.ApiKey : string.Empty))
            : (deepseekKey ?? (!string.IsNullOrWhiteSpace(loadedSettings?.ApiKey) ? loadedSettings.ApiKey : string.Empty));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var keyName = isClaude ? "Claude (ANTHROPIC_AUTH_TOKEN, CLAUDE_API_KEY hoặc ANTHROPIC_API_KEY)" : "DeepSeek (DEEPSEEK_API_KEY)";
            _logger.LogWarning("Chưa cấu hình API Key cho {KeyName}.", keyName);
        }

        var defaultModel = isClaude ? "claude-3-5-haiku-20241022" : "deepseek-reasoner";
        var model = isClaude
            ? (configuration["Claude:Model"] ?? Environment.GetEnvironmentVariable("CLAUDE_MODEL") ?? Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? loadedSettings?.Model ?? defaultModel)
            : (configuration["DeepSeek:Model"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? loadedSettings?.Model ?? defaultModel);

        var defaultBaseUrl = isClaude ? "https://api.fableaiapi.com" : "https://api.deepseek.com";
        var baseUrl = isClaude
            ? (configuration["Claude:BaseUrl"] ?? Environment.GetEnvironmentVariable("CLAUDE_BASE_URL") ?? Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? loadedSettings?.BaseUrl ?? defaultBaseUrl)
            : (configuration["DeepSeek:BaseUrl"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL") ?? loadedSettings?.BaseUrl ?? defaultBaseUrl);

        float temperature = loadedSettings?.Temperature ?? 0.2f;
        var tempConfig = isClaude
            ? (configuration["Claude:Temperature"] ?? Environment.GetEnvironmentVariable("CLAUDE_TEMPERATURE"))
            : (configuration["DeepSeek:Temperature"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_TEMPERATURE"));

        if (float.TryParse(tempConfig, out var parsedTemp))
        {
            temperature = parsedTemp;
        }

        return new LlmSettings
        {
            Driver = driver,
            ApiKey = apiKey,
            Model = model,
            BaseUrl = baseUrl,
            Temperature = temperature
        };
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
        }

        try
        {
            string jsonText;
            lock (_lockObject)
            {
                jsonText = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            }

            await File.WriteAllTextAsync(_filePath, jsonText, cancellationToken);
            _logger.LogInformation("Đã lưu cấu hình LLM thành công vào file: {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lưu file cấu hình LLM {FilePath}", _filePath);
            throw;
        }
    }
}

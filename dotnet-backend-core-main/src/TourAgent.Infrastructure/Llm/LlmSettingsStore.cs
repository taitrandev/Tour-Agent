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
    /// </summary>
    /// <param name="configuration">Đối tượng IConfiguration.</param>
    /// <returns>Đối tượng LlmSettings đã nạp.</returns>
    private LlmSettings LoadSettings(IConfiguration configuration)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var fileContent = File.ReadAllText(_filePath);
                var loadedSettings = JsonSerializer.Deserialize<LlmSettings>(fileContent);
                if (loadedSettings != null)
                {
                    return loadedSettings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc file {FilePath}, đang sử dụng cấu hình mặc định.", _filePath);
        }

        var apiKey = configuration["DeepSeek:ApiKey"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
            ?? "sk-92e03f3300c7429389be0a047169e06a";

        var model = configuration["DeepSeek:Model"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_MODEL")
            ?? "deepseek-reasoner";

        var baseUrl = configuration["DeepSeek:BaseUrl"]
            ?? "https://api.deepseek.com";

        float temperature = 0.2f;
        if (float.TryParse(configuration["DeepSeek:Temperature"], out var parsedTemp))
        {
            temperature = parsedTemp;
        }

        return new LlmSettings
        {
            Driver = "deepseek",
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
        CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
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

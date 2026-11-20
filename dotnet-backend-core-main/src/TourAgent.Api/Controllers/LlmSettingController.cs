using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Abstractions;

namespace TourAgent.Api.Controllers;

/// <summary>
/// DTO chứa tham số yêu cầu lưu cấu hình cổng LLM.
/// </summary>
public sealed record SaveLlmSettingsRequest(
    string? ApiKey,
    string? Model,
    string? BaseUrl,
    float? Temperature,
    string? Driver = null);

/// <summary>
/// Controller quản lý cấu hình và kiểm tra kết nối cổng LLM (DeepSeek AI & Anthropic Claude Gateway).
/// Tương thích 100% với giao thức và UI của module LlmSetting từ phiên bản PHP.
/// </summary>
[ApiController]
[Route("api/v1/llm-setting")]
[Route("api/v1/agent/settings/llm")]
public sealed class LlmSettingController : ControllerBase
{
    private readonly ILlmSettingsStore _settingsStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmSettingController> _logger;

    /// <summary>
    /// Khởi tạo Controller với store cấu hình LLM và HttpClient để ping kiểm tra kết nối.
    /// </summary>
    /// <param name="settingsStore">Cửa hàng lưu trữ cấu hình LLM runtime.</param>
    /// <param name="httpClient">Thực thể HttpClient để thực hiện ping kiểm tra API.</param>
    /// <param name="logger">Logger ghi log hoạt động.</param>
    public LlmSettingController(
        ILlmSettingsStore settingsStore,
        HttpClient httpClient,
        ILogger<LlmSettingController> logger)
    {
        _settingsStore = settingsStore;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Lấy cấu hình LLM hiện tại kèm API Key đã được che giấu một phần an toàn.
    /// </summary>
    /// <returns>Trạng thái cấu hình LLM hiện hành.</returns>
    [HttpGet]
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var settings = _settingsStore.GetSettings();
        var isConfigured = !string.IsNullOrWhiteSpace(settings.ApiKey);
        var maskedKey = _settingsStore.GetMaskedApiKey();

        return Ok(new
        {
            success = true,
            data = new
            {
                driver = settings.Driver,
                is_configured = isConfigured,
                api_key_masked = maskedKey,
                model = settings.Model,
                base_url = settings.BaseUrl,
                temperature = settings.Temperature
            }
        });
    }

    /// <summary>
    /// Lưu cấu hình LLM mới vào kho lưu trữ bền vững.
    /// </summary>
    /// <param name="request">Dữ liệu cấu hình mới.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông báo kết quả lưu cấu hình.</returns>
    [HttpPost]
    [HttpPost("save")]
    public async Task<IActionResult> SaveSettings(
        [FromBody] SaveLlmSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var currentSettings = _settingsStore.GetSettings();

        // Tự động nhận diện driver nếu không được truyền vào rõ ràng
        var driver = request.Driver;
        if (string.IsNullOrWhiteSpace(driver))
        {
            if (request.Model?.Contains("claude", StringComparison.OrdinalIgnoreCase) == true
                || request.Model?.Contains("haiku", StringComparison.OrdinalIgnoreCase) == true
                || request.ApiKey?.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase) == true
                || request.BaseUrl?.Contains("anthropic", StringComparison.OrdinalIgnoreCase) == true
                || request.BaseUrl?.Contains("fableai", StringComparison.OrdinalIgnoreCase) == true)
            {
                driver = "claude";
            }
            else
            {
                driver = !string.IsNullOrWhiteSpace(currentSettings.Driver) ? currentSettings.Driver : "deepseek";
            }
        }
        driver = driver.Trim().ToLowerInvariant();

        var isClaude = driver == "claude";
        var defaultModel = isClaude ? "claude-3-5-haiku-20241022" : "deepseek-reasoner";
        var defaultBaseUrl = isClaude ? "https://api.fableaiapi.com" : "https://api.deepseek.com";

        var selectedModel = !string.IsNullOrWhiteSpace(request.Model)
            ? request.Model.Trim()
            : defaultModel;

        var selectedBaseUrl = !string.IsNullOrWhiteSpace(request.BaseUrl)
            ? request.BaseUrl.Trim()
            : defaultBaseUrl;

        await _settingsStore.UpdateSettingsAsync(
            apiKey: request.ApiKey,
            model: selectedModel,
            baseUrl: selectedBaseUrl,
            temperature: request.Temperature,
            driver: driver,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Đã cập nhật cấu hình LLM thành công: Driver={Driver}, Model={Model}", driver, selectedModel);

        var providerName = isClaude ? "Claude (Anthropic)" : "DeepSeek";
        return Ok(new
        {
            success = true,
            message = $"Lưu cấu hình LLM {providerName} thành công!"
        });
    }

    /// <summary>
    /// Kiểm tra kết nối trực tiếp đến API Gateway (Ping test) và đo độ trễ mạng.
    /// Hỗ trợ cả DeepSeek và Anthropic Claude API.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả đo độ trễ và trạng thái kết nối.</returns>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var settings = _settingsStore.GetSettings();
        var isClaude = string.Equals(settings.Driver, "claude", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(settings.Model) && settings.Model.Contains("claude", StringComparison.OrdinalIgnoreCase));
        var providerTitle = isClaude ? "Claude (Anthropic)" : "DeepSeek AI";

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            var keyName = isClaude ? "CLAUDE_API_KEY (Anthropic)" : "DEEPSEEK_API_KEY";
            return BadRequest(new
            {
                success = false,
                latency_ms = 0,
                message = $"{keyName} chưa được cấu hình. Vui lòng nhập API Key trước khi kiểm tra."
            });
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var defaultBaseUrl = isClaude ? "https://api.anthropic.com" : "https://api.deepseek.com";
            var baseUrl = (!string.IsNullOrWhiteSpace(settings.BaseUrl) ? settings.BaseUrl : defaultBaseUrl).TrimEnd('/');

            using var pingRequest = isClaude
                ? CreateClaudePingRequest(baseUrl, settings.ApiKey)
                : CreateDeepSeekPingRequest(baseUrl, settings.ApiKey);

            using var pingResponse = await _httpClient.SendAsync(pingRequest, cancellationToken);
            stopwatch.Stop();
            var latencyMs = (int)stopwatch.ElapsedMilliseconds;

            if (pingResponse.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    success = true,
                    latency_ms = latencyMs,
                    message = $"Kết nối {providerTitle} thành công 100%!",
                    driver = settings.Driver,
                    model = settings.Model,
                    test_output = "OK (200 Models Endpoint Reachable)"
                });
            }

            var errorBody = await pingResponse.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode((int)pingResponse.StatusCode, new
            {
                success = false,
                latency_ms = latencyMs,
                message = $"{providerTitle} trả về mã lỗi {pingResponse.StatusCode}: {errorBody}",
                driver = settings.Driver,
                model = settings.Model
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var latencyMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Lỗi kiểm tra kết nối tới {ProviderTitle} API Gateway.", providerTitle);

            return StatusCode(500, new
            {
                success = false,
                latency_ms = latencyMs,
                message = $"Không thể kết nối đến {providerTitle}: {ex.Message}",
                driver = settings.Driver,
                model = settings.Model
            });
        }
    }

    /// <summary>
    /// Tạo yêu cầu ping HTTP kiểm tra kết nối đến Anthropic Claude API models endpoint.
    /// </summary>
    private static HttpRequestMessage CreateClaudePingRequest(string baseUrl, string apiKey)
    {
        var endpoint = baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/models"
            : $"{baseUrl}/v1/models";

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("x-api-key", apiKey.Trim());
        request.Headers.Add("anthropic-version", "2023-06-01");
        return request;
    }

    /// <summary>
    /// Tạo yêu cầu ping HTTP kiểm tra kết nối đến DeepSeek API Gateway models endpoint.
    /// </summary>
    private static HttpRequestMessage CreateDeepSeekPingRequest(string baseUrl, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        return request;
    }
}

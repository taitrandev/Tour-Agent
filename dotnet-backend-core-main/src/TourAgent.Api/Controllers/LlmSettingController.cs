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
    float? Temperature);

/// <summary>
/// Controller quản lý cấu hình và kiểm tra kết nối cổng LLM DeepSeek AI Gateway.
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
        var selectedModel = !string.IsNullOrWhiteSpace(request.Model)
            ? request.Model.Trim()
            : "deepseek-reasoner";

        await _settingsStore.UpdateSettingsAsync(
            apiKey: request.ApiKey,
            model: selectedModel,
            baseUrl: request.BaseUrl,
            temperature: request.Temperature,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Đã cập nhật cấu hình LLM thành công: Model={Model}", selectedModel);

        return Ok(new
        {
            success = true,
            message = "Lưu cấu hình LLM DeepSeek thành công!"
        });
    }

    /// <summary>
    /// Kiểm tra kết nối trực tiếp đến DeepSeek API Gateway (Ping test) và đo độ trễ mạng.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả đo độ trễ và trạng thái kết nối.</returns>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var settings = _settingsStore.GetSettings();

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return BadRequest(new
            {
                success = false,
                latency_ms = 0,
                message = "DEEPSEEK_API_KEY chưa được cấu hình. Vui lòng nhập API Key trước khi kiểm tra."
            });
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');
            using var pingRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
            pingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var pingResponse = await _httpClient.SendAsync(pingRequest, cancellationToken);
            stopwatch.Stop();
            var latencyMs = (int)stopwatch.ElapsedMilliseconds;

            if (pingResponse.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    success = true,
                    latency_ms = latencyMs,
                    message = "Kết nối DeepSeek AI thành công 100%!",
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
                message = $"DeepSeek API trả về mã lỗi {pingResponse.StatusCode}: {errorBody}",
                driver = settings.Driver,
                model = settings.Model
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var latencyMs = (int)stopwatch.ElapsedMilliseconds;
            _logger.LogError(ex, "Lỗi kiểm tra kết nối tới DeepSeek API Gateway.");

            return StatusCode(500, new
            {
                success = false,
                latency_ms = latencyMs,
                message = $"Không thể kết nối đến DeepSeek: {ex.Message}",
                driver = settings.Driver,
                model = settings.Model
            });
        }
    }
}

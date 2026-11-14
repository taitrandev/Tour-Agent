using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Health;

/// <summary>
/// Health Check kiểm tra tính sẵn sàng và trạng thái kết nối tới dịch vụ DeepSeek AI API.
/// </summary>
public sealed class DeepSeekHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILlmSettingsStore _settingsStore;

    /// <summary>
    /// Khởi tạo DeepSeekHealthCheck với IHttpClientFactory và kho cấu hình LLM runtime.
    /// </summary>
    /// <param name="httpClientFactory">Factory tạo thực thể HttpClient độc lập.</param>
    /// <param name="settingsStore">Cửa hàng lưu trữ và quản lý cấu hình DeepSeek LLM.</param>
    public DeepSeekHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILlmSettingsStore settingsStore)
    {
        _httpClientFactory = httpClientFactory;
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Kiểm tra trạng thái hoạt động của DeepSeek API bằng cách truy vấn danh sách models.
    /// </summary>
    /// <param name="context">Ngữ cảnh kiểm tra sức khỏe.</param>
    /// <param name="cancellationToken">CancellationToken hủy tác vụ nếu vượt ngưỡng thời gian.</param>
    /// <returns>Kết quả kiểm tra sức khỏe: Healthy, Degraded hoặc Unhealthy.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = _settingsStore.GetSettings();

        // 1. Nếu chưa có API Key, đánh dấu Degraded (tính năng AI suy giảm, nhưng app chính vẫn chạy)
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return HealthCheckResult.Degraded(
                "DeepSeek API Key chưa được cấu hình. Tính năng AI Agent sẽ tạm thời không khả dụng.");
        }

        // 2. Giới hạn thời gian probe tối đa 5 giây để tránh làm nghẽn toàn bộ pipeline /health
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("DeepSeekHealthProbe");
            var baseUrl = (settings.BaseUrl ?? "https://api.deepseek.com").TrimEnd('/');
            var requestUri = $"{baseUrl}/models";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(
                    $"DeepSeek API kết nối thành công. Mô hình kích hoạt: {settings.Model}.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Degraded("DeepSeek API Key không hợp lệ hoặc đã hết hạn (HTTP 401). Tính năng AI tạm suy giảm.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return HealthCheckResult.Degraded("DeepSeek API vượt ngưỡng giới hạn tần suất hoặc cạn quota (HTTP 429).");
            }

            return HealthCheckResult.Degraded(
                $"DeepSeek API phản hồi mã trạng thái bất thường: {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("DeepSeek API phản hồi quá thời gian quy định (Timeout > 5s).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Không thể kết nối tới DeepSeek API (Tính năng AI tạm suy giảm): {ex.Message}", ex);
        }
    }
}


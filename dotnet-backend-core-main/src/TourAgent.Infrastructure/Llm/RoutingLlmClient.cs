using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Llm;

/// <summary>
/// Cổng điều phối đa mô hình (Routing LLM Dispatcher) giữa các nhà cung cấp DeepSeek và Anthropic Claude.
/// Tự động định tuyến các yêu cầu gọi AI dựa trên cấu hình Driver hoặc Model đã chỉ định ở runtime.
/// </summary>
public sealed class RoutingLlmClient : ILlmClient
{
    private readonly DeepSeekLlmClient _deepSeekClient;
    private readonly ClaudeLlmClient _claudeClient;
    private readonly ILlmSettingsStore _settingsStore;

    /// <summary>
    /// Khởi tạo RoutingLlmClient với các client LLM cụ thể và cửa hàng cấu hình runtime.
    /// </summary>
    /// <param name="deepSeekClient">Thực thể DeepSeekLlmClient.</param>
    /// <param name="claudeClient">Thực thể ClaudeLlmClient.</param>
    /// <param name="settingsStore">Cửa hàng lưu trữ và quản lý cấu hình LLM runtime.</param>
    public RoutingLlmClient(
        DeepSeekLlmClient deepSeekClient,
        ClaudeLlmClient claudeClient,
        ILlmSettingsStore settingsStore)
    {
        _deepSeekClient = deepSeekClient;
        _claudeClient = claudeClient;
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Xác định thực thể ILlmClient tương ứng với cấu hình Driver và Model hiện hành.
    /// </summary>
    /// <param name="requestedModel">Tên Model yêu cầu trong request nếu có.</param>
    /// <returns>Thực thể ILlmClient phù hợp (DeepSeek hoặc Claude).</returns>
    private ILlmClient ResolveClient(string? requestedModel = null)
    {
        var settings = _settingsStore.GetSettings();
        var effectiveModel = requestedModel ?? settings.Model;

        var isClaude = string.Equals(settings.Driver, "claude", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(effectiveModel) && (effectiveModel.Contains("claude", StringComparison.OrdinalIgnoreCase) || effectiveModel.Contains("haiku", StringComparison.OrdinalIgnoreCase)))
            || (!string.IsNullOrWhiteSpace(settings.BaseUrl) && settings.BaseUrl.Contains("fableai", StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(settings.ApiKey) && settings.ApiKey.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase));

        return isClaude ? _claudeClient : _deepSeekClient;
    }

    /// <inheritdoc />
    public Task<ChatCompletionResponse> CompleteChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = ResolveClient(request.Model);
        return client.CompleteChatAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatStreamChunk> StreamChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = ResolveClient(request.Model);
        return client.StreamChatAsync(request, cancellationToken);
    }
}


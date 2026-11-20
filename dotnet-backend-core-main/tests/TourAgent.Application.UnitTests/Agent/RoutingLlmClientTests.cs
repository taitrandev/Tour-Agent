using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TourAgent.Application.Abstractions;
using TourAgent.Infrastructure.Llm;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Store cấu hình LLM in-memory phục vụ kiểm thử đơn vị độc lập.
/// </summary>
internal sealed class InMemoryLlmSettingsStore : ILlmSettingsStore
{
    private LlmSettings _settings;

    /// <summary>
    /// Khởi tạo store với cấu hình chỉ định ban đầu.
    /// </summary>
    public InMemoryLlmSettingsStore(LlmSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    public LlmSettings GetSettings() => _settings;

    /// <inheritdoc />
    public string GetMaskedApiKey() => "sk-test••••••••1234";

    /// <inheritdoc />
    public Task UpdateSettingsAsync(
        string? apiKey,
        string model,
        string? baseUrl,
        float? temperature,
        string? driver = null,
        CancellationToken cancellationToken = default)
    {
        _settings.Driver = driver ?? _settings.Driver;
        _settings.Model = model;
        if (!string.IsNullOrWhiteSpace(apiKey)) _settings.ApiKey = apiKey;
        if (!string.IsNullOrWhiteSpace(baseUrl)) _settings.BaseUrl = baseUrl;
        if (temperature.HasValue) _settings.Temperature = temperature.Value;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Kiểm thử tính đúng đắn của RoutingLlmClient (Định tuyến đa mô hình linh hoạt).
/// </summary>
public sealed class RoutingLlmClientTests
{
    /// <summary>
    /// Kiểm tra khi cấu hình Driver là 'claude', RoutingLlmClient điều phối sang ClaudeLlmClient.
    /// </summary>
    [Fact]
    public async Task CompleteChatAsync_WhenDriverIsClaude_RoutesToClaudeClient()
    {
        // Arrange
        var settingsStore = new InMemoryLlmSettingsStore(new LlmSettings
        {
            Driver = "claude",
            Model = "claude-3-5-sonnet-20241022",
            ApiKey = string.Empty // Sẽ kích hoạt fallback response của Claude
        });

        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient(new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        var deepSeekClient = new DeepSeekLlmClient(httpClient, settingsStore, config, NullLogger<DeepSeekLlmClient>.Instance);
        var claudeClient = new ClaudeLlmClient(httpClient, settingsStore, config, NullLogger<ClaudeLlmClient>.Instance);

        var router = new RoutingLlmClient(deepSeekClient, claudeClient, settingsStore);

        var request = new ChatCompletionRequest(
            Messages: [ChatMessage.CreateUser("Xin chào")]);

        // Act
        var response = await router.CompleteChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("claude", response.Model, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra khi cấu hình Driver là 'deepseek', RoutingLlmClient điều phối sang DeepSeekLlmClient.
    /// </summary>
    [Fact]
    public async Task CompleteChatAsync_WhenDriverIsDeepSeek_RoutesToDeepSeekClient()
    {
        // Arrange
        var settingsStore = new InMemoryLlmSettingsStore(new LlmSettings
        {
            Driver = "deepseek",
            Model = "deepseek-chat",
            ApiKey = string.Empty // Sẽ kích hoạt fallback response của DeepSeek
        });

        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient(new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        var deepSeekClient = new DeepSeekLlmClient(httpClient, settingsStore, config, NullLogger<DeepSeekLlmClient>.Instance);
        var claudeClient = new ClaudeLlmClient(httpClient, settingsStore, config, NullLogger<ClaudeLlmClient>.Instance);

        var router = new RoutingLlmClient(deepSeekClient, claudeClient, settingsStore);

        var request = new ChatCompletionRequest(
            Messages: [ChatMessage.CreateUser("Xin chào")]);

        // Act
        var response = await router.CompleteChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("deepseek", response.Model, StringComparison.OrdinalIgnoreCase);
    }
}


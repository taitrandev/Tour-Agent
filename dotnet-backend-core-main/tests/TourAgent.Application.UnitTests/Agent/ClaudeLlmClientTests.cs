using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TourAgent.Application.Abstractions;
using TourAgent.Infrastructure.Llm;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Mock HttpMessageHandler phục vụ kiểm thử đơn vị HTTP client hermetic.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    /// <summary>
    /// Khởi tạo mock handler với delegate xử lý request.
    /// </summary>
    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Gửi request qua delegate giả lập để trả về response mong muốn.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}

/// <summary>
/// Kiểm thử tính đúng đắn của ClaudeLlmClient (Anthropic Messages API protocol).
/// </summary>
public sealed class ClaudeLlmClientTests
{
    /// <summary>
    /// Kiểm tra khi chưa có API key, client trả về phản hồi mô phỏng dự phòng an toàn.
    /// </summary>
    [Fact]
    public async Task CompleteChatAsync_WhenApiKeyIsEmpty_ReturnsFallbackResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder().Build();
        var client = new ClaudeLlmClient(httpClient, config, NullLogger<ClaudeLlmClient>.Instance);

        var request = new ChatCompletionRequest(
            Messages: [ChatMessage.CreateUser("Xin chào!")]);

        // Act
        var response = await client.CompleteChatAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.Equal("stop", response.FinishReason);
        Assert.Contains("mock", response.Model);
    }

    /// <summary>
    /// Kiểm tra gửi request thành công, kiểm tra header Anthropic và chuyển đổi phản hồi văn bản.
    /// </summary>
    [Fact]
    public async Task CompleteChatAsync_WithValidApiKey_SendsCorrectHeadersAndParsesTextResponse()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

            var jsonResponse = """
            {
                "id": "msg_test_123",
                "type": "message",
                "role": "assistant",
                "model": "claude-3-5-sonnet-20241022",
                "content": [
                    {
                        "type": "text",
                        "text": "Chào bạn, mình có thể giúp gì cho chuyến du lịch?"
                    }
                ],
                "stop_reason": "end_turn",
                "usage": {
                    "input_tokens": 40,
                    "output_tokens": 15
                }
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:ApiKey"] = "sk-ant-test-key-123456",
                ["Claude:BaseUrl"] = "https://api.anthropic.com",
                ["Claude:Model"] = "claude-3-5-sonnet-20241022"
            })
            .Build();

        var client = new ClaudeLlmClient(httpClient, config, NullLogger<ClaudeLlmClient>.Instance);

        var request = new ChatCompletionRequest(
            Messages:
            [
                ChatMessage.CreateSystem("Bạn là trợ lý du lịch."),
                ChatMessage.CreateUser("Tư vấn tour giúp tôi")
            ]);

        // Act
        var response = await client.CompleteChatAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("sk-ant-test-key-123456", capturedRequest.Headers.GetValues("x-api-key").First());
        Assert.Equal("2023-06-01", capturedRequest.Headers.GetValues("anthropic-version").First());
        Assert.NotNull(capturedBody);
        using var parsedReqDoc = JsonDocument.Parse(capturedBody);
        Assert.Equal("Bạn là trợ lý du lịch.", parsedReqDoc.RootElement.GetProperty("system").GetString());

        Assert.Equal("Chào bạn, mình có thể giúp gì cho chuyến du lịch?", response.Content);
        Assert.Equal("stop", response.FinishReason);
        Assert.Equal(40, response.Usage.PromptTokens);
        Assert.Equal(15, response.Usage.CompletionTokens);
        Assert.Equal(55, response.Usage.TotalTokens);
    }

    /// <summary>
    /// Kiểm tra Anthropic Messages API khi mô hình yêu cầu gọi công cụ (tool_use).
    /// </summary>
    [Fact]
    public async Task CompleteChatAsync_WhenClaudeRequestsTool_ParsesToolCallsCorrectly()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ =>
        {
            var jsonResponse = """
            {
                "id": "msg_tool_123",
                "type": "message",
                "role": "assistant",
                "model": "claude-3-5-sonnet-20241022",
                "content": [
                    {
                        "type": "text",
                        "text": "Để tôi tìm kiếm tour phù hợp cho bạn."
                    },
                    {
                        "type": "tool_use",
                        "id": "toolu_search_01",
                        "name": "search_travels",
                        "input": {
                            "destination": "Đà Lạt",
                            "max_budget": 5000000
                        }
                    }
                ],
                "stop_reason": "tool_use",
                "usage": {
                    "input_tokens": 100,
                    "output_tokens": 40
                }
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:ApiKey"] = "sk-ant-test-key",
                ["Claude:Model"] = "claude-3-5-sonnet-20241022"
            })
            .Build();

        var client = new ClaudeLlmClient(httpClient, config, NullLogger<ClaudeLlmClient>.Instance);

        using var schemaDoc = JsonDocument.Parse("""{"type":"object","properties":{"destination":{"type":"string"}}}""");
        var toolDef = new ToolDefinition("search_travels", "Tìm tour", schemaDoc);

        var request = new ChatCompletionRequest(
            Messages: [ChatMessage.CreateUser("Tìm tour Đà Lạt")],
            Tools: [toolDef]);

        // Act
        var response = await client.CompleteChatAsync(request);

        // Assert
        Assert.Equal("tool_calls", response.FinishReason);
        Assert.Single(response.ToolCalls);
        var toolCall = response.ToolCalls[0];
        Assert.Equal("toolu_search_01", toolCall.Id);
        Assert.Equal("search_travels", toolCall.FunctionName);
        Assert.Contains("Đà Lạt", toolCall.FunctionArguments);
    }
}

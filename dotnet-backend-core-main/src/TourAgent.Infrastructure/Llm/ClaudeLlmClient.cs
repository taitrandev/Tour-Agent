using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;

namespace TourAgent.Infrastructure.Llm;

/// <summary>
/// Client kết nối trực tiếp với dịch vụ Anthropic Claude API (giao thức Messages API).
/// Đóng gói tính năng Tool Calling (Function Calling), đo lường độ trễ mạng và truyền phát SSE.
/// </summary>
public sealed class ClaudeLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeLlmClient> _logger;
    private readonly ILlmSettingsStore? _settingsStore;
    private readonly string _fallbackApiKey;
    private readonly string _fallbackModel;
    private readonly string _fallbackBaseUrl;

    private const string DefaultAnthropicVersion = "2023-06-01";
    private const string DefaultClaudeModel = "claude-3-5-sonnet-20241022";
    private const string DefaultClaudeBaseUrl = "https://api.anthropic.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Khởi tạo ClaudeLlmClient với HttpClient, store cấu hình động và logger.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient để gửi yêu cầu mạng.</param>
    /// <param name="settingsStore">Kho lưu trữ cấu hình LLM runtime.</param>
    /// <param name="configuration">Cấu hình hệ thống IConfiguration.</param>
    /// <param name="logger">Logger ghi log hoạt động và cảnh báo.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public ClaudeLlmClient(
        HttpClient httpClient,
        ILlmSettingsStore settingsStore,
        IConfiguration configuration,
        ILogger<ClaudeLlmClient> logger)
    {
        _httpClient = httpClient;
        _settingsStore = settingsStore;
        _logger = logger;

        _fallbackApiKey = configuration["Claude:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN")
            ?? string.Empty;

        _fallbackModel = configuration["Claude:Model"]
            ?? Environment.GetEnvironmentVariable("CLAUDE_MODEL")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")
            ?? DefaultClaudeModel;

        _fallbackBaseUrl = (configuration["Claude:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("CLAUDE_BASE_URL")
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL")
            ?? DefaultClaudeBaseUrl).TrimEnd('/');
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ kiểm thử đơn vị độc lập.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient.</param>
    /// <param name="configuration">Cấu hình chứa API Key và endpoint của Claude.</param>
    /// <param name="logger">Logger ghi log hoạt động và cảnh báo.</param>
    public ClaudeLlmClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClaudeLlmClient> logger)
        : this(httpClient, null!, configuration, logger)
    {
    }

    /// <summary>
    /// Lấy API Key hiện thời từ settings store hoặc fallback configuration.
    /// </summary>
    /// <returns>Chuỗi khóa API của Anthropic Claude.</returns>
    private string GetApiKey()
    {
        var key = _settingsStore?.GetSettings().ApiKey;
        return !string.IsNullOrWhiteSpace(key) ? key : _fallbackApiKey;
    }

    /// <summary>
    /// Lấy tên model hiện thời từ settings store hoặc fallback configuration.
    /// </summary>
    /// <returns>Tên mô hình Claude đang chọn.</returns>
    private string GetModel()
    {
        var model = _settingsStore?.GetSettings().Model;
        return !string.IsNullOrWhiteSpace(model) ? model : _fallbackModel;
    }

    /// <summary>
    /// Lấy Base URL hiện thời từ settings store hoặc fallback configuration.
    /// </summary>
    /// <returns>Địa chỉ URL gốc của cổng Anthropic API.</returns>
    private string GetBaseUrl()
    {
        var baseUrl = _settingsStore?.GetSettings().BaseUrl;
        return (!string.IsNullOrWhiteSpace(baseUrl) ? baseUrl : _fallbackBaseUrl).TrimEnd('/');
    }

    /// <summary>
    /// Xây dựng URL endpoint hoàn chỉnh cho Anthropic Messages API.
    /// </summary>
    /// <param name="baseUrl">Địa chỉ URL gốc.</param>
    /// <returns>Đường dẫn API hoàn chỉnh tới Messages endpoint.</returns>
    private static string BuildMessagesEndpoint(string baseUrl)
    {
        var clean = baseUrl.TrimEnd('/');
        if (clean.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase) || clean.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
        {
            return clean;
        }

        return clean.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{clean}/messages"
            : $"{clean}/v1/messages";
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> CompleteChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var apiKey = GetApiKey();
        var baseUrl = GetBaseUrl();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Claude API Key chưa được thiết lập. Đang sử dụng chế độ mô phỏng an toàn.");
            return GenerateFallbackResponse(request, (int)stopwatch.ElapsedMilliseconds);
        }

        var requestBody = BuildClaudePayload(request, stream: false);
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var endpoint = BuildMessagesEndpoint(baseUrl);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = jsonContent
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Add("anthropic-version", DefaultAnthropicVersion);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        var latencyMs = (int)stopwatch.ElapsedMilliseconds;

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Claude API lỗi ({StatusCode}): {ErrorBody}", httpResponse.StatusCode, errorBody);
            throw new HttpRequestException($"Claude API trả về mã lỗi {httpResponse.StatusCode}: {errorBody}");
        }

        var responseString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        using var responseDoc = JsonDocument.Parse(responseString);
        var root = responseDoc.RootElement;

        var textContentBuilder = new StringBuilder();
        var toolCalls = new List<ToolCall>();

        if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArray.EnumerateArray())
            {
                var blockType = block.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                if (blockType == "text" && block.TryGetProperty("text", out var textProp))
                {
                    textContentBuilder.Append(textProp.GetString());
                }
                else if (blockType == "tool_use")
                {
                    var id = block.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
                    var name = block.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                    var arguments = block.TryGetProperty("input", out var inputProp) ? inputProp.GetRawText() : "{}";

                    toolCalls.Add(new ToolCall(id, name, arguments));
                }
            }
        }

        var rawStopReason = root.TryGetProperty("stop_reason", out var stopReasonProp) && stopReasonProp.ValueKind == JsonValueKind.String
            ? stopReasonProp.GetString()
            : "end_turn";

        var finishReason = rawStopReason switch
        {
            "tool_use" => "tool_calls",
            "end_turn" => "stop",
            "max_tokens" => "length",
            _ => rawStopReason ?? "stop"
        };

        int promptTokens = 0;
        int completionTokens = 0;
        if (root.TryGetProperty("usage", out var usageProp))
        {
            promptTokens = usageProp.TryGetProperty("input_tokens", out var inTok) ? inTok.GetInt32() : 0;
            completionTokens = usageProp.TryGetProperty("output_tokens", out var outTok) ? outTok.GetInt32() : 0;
        }

        var responseModel = root.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String
            ? modelProp.GetString() ?? GetModel()
            : GetModel();

        var content = textContentBuilder.Length > 0 ? textContentBuilder.ToString() : null;

        return new ChatCompletionResponse(
            Content: content,
            ToolCalls: toolCalls,
            FinishReason: finishReason,
            Usage: new TokenUsage(promptTokens, completionTokens, promptTokens + completionTokens),
            Model: responseModel,
            LatencyMs: latencyMs);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatStreamChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        var baseUrl = GetBaseUrl();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return new ChatStreamChunk("Dạ em chào anh/chị, em là chuyên viên tư vấn Claude AI. Em có thể hỗ trợ gì cho chuyến du lịch sắp tới ạ?", null, "stop");
            yield break;
        }

        var requestBody = BuildClaudePayload(request, stream: true);
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var endpoint = BuildMessagesEndpoint(baseUrl);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = jsonContent
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Add("anthropic-version", DefaultAnthropicVersion);

        using var httpResponse = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            var jsonLine = line["data: ".Length..].Trim();
            ChatStreamChunk? chunk = null;

            try
            {
                using var doc = JsonDocument.Parse(jsonLine);
                var root = doc.RootElement;
                var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

                if (eventType == "content_block_delta")
                {
                    if (root.TryGetProperty("delta", out var deltaProp) &&
                        deltaProp.TryGetProperty("type", out var deltaType) &&
                        deltaType.GetString() == "text_delta" &&
                        deltaProp.TryGetProperty("text", out var textProp))
                    {
                        var text = textProp.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            chunk = new ChatStreamChunk(text, null, null);
                        }
                    }
                }
                else if (eventType == "message_delta")
                {
                    if (root.TryGetProperty("delta", out var deltaProp) &&
                        deltaProp.TryGetProperty("stop_reason", out var stopReasonProp) &&
                        stopReasonProp.ValueKind == JsonValueKind.String)
                    {
                        var rawReason = stopReasonProp.GetString();
                        var finishReason = rawReason switch
                        {
                            "tool_use" => "tool_calls",
                            "end_turn" => "stop",
                            "max_tokens" => "length",
                            _ => rawReason
                        };

                        chunk = new ChatStreamChunk(null, null, finishReason);
                    }
                }
                else if (eventType == "message_stop")
                {
                    break;
                }
            }
            catch (JsonException)
            {
                // Bỏ qua dòng json không chuẩn
            }

            if (chunk is not null)
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// Chuyển đổi ChatCompletionRequest thành payload JSON tương thích Anthropic Messages API.
    /// </summary>
    /// <param name="request">Yêu cầu hoàn thành hội thoại từ tầng ứng dụng.</param>
    /// <param name="stream">Có kích hoạt cơ chế truyền phát SSE hay không.</param>
    /// <returns>Dictionary cấu trúc JSON gửi tới Anthropic Messages endpoint.</returns>
    private Dictionary<string, object?> BuildClaudePayload(ChatCompletionRequest request, bool stream)
    {
        // 1. Tách system prompt từ các tin nhắn hệ thống
        var systemPrompts = request.Messages
            .Where(m => m.Role == ChatMessageRoles.System && !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => m.Content!)
            .ToList();

        string? mergedSystemPrompt = systemPrompts.Count > 0
            ? string.Join("\n\n", systemPrompts)
            : null;

        // 2. Chuyển đổi tin nhắn hội thoại thành danh sách messages của Claude (role: user hoặc assistant)
        var claudeMessages = new List<object>();
        var nonSystemMessages = request.Messages
            .Where(m => m.Role != ChatMessageRoles.System)
            .ToList();

        int i = 0;
        while (i < nonSystemMessages.Count)
        {
            var msg = nonSystemMessages[i];

            // Nếu là kết quả thực thi công cụ (Role: tool)
            if (msg.Role == ChatMessageRoles.Tool)
            {
                // Gom tất cả các tool response liên tiếp thành 1 tin nhắn User với nhiều tool_result block
                var toolResultBlocks = new List<object>();
                while (i < nonSystemMessages.Count && nonSystemMessages[i].Role == ChatMessageRoles.Tool)
                {
                    var toolMsg = nonSystemMessages[i];
                    toolResultBlocks.Add(new
                    {
                        type = "tool_result",
                        tool_use_id = toolMsg.ToolCallId ?? string.Empty,
                        content = toolMsg.Content ?? "{}"
                    });
                    i++;
                }

                claudeMessages.Add(new
                {
                    role = "user",
                    content = toolResultBlocks
                });
                continue;
            }

            // Nếu là câu trả lời của trợ lý AI có chứa tool calls
            if (msg.Role == ChatMessageRoles.Assistant && msg.ToolCalls.Count > 0)
            {
                var contentBlocks = new List<object>();

                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    contentBlocks.Add(new
                    {
                        type = "text",
                        text = msg.Content
                    });
                }

                foreach (var call in msg.ToolCalls)
                {
                    object inputObj;
                    try
                    {
                        inputObj = JsonDocument.Parse(call.FunctionArguments).RootElement;
                    }
                    catch
                    {
                        inputObj = new { };
                    }

                    contentBlocks.Add(new
                    {
                        type = "tool_use",
                        id = call.Id,
                        name = call.FunctionName,
                        input = inputObj
                    });
                }

                claudeMessages.Add(new
                {
                    role = "assistant",
                    content = contentBlocks
                });
                i++;
                continue;
            }

            // Tin nhắn User hoặc Assistant văn bản thông thường
            var role = msg.Role == ChatMessageRoles.Assistant ? "assistant" : "user";
            claudeMessages.Add(new
            {
                role = role,
                content = msg.Content ?? string.Empty
            });
            i++;
        }

        // Đảm bảo không để danh sách tin nhắn rỗng gửi lên Claude
        if (claudeMessages.Count == 0)
        {
            claudeMessages.Add(new
            {
                role = "user",
                content = "Xin chào"
            });
        }

        var effectiveModel = request.Model ?? GetModel();
        var maxTokens = request.MaxTokens ?? 4096;

        var payload = new Dictionary<string, object?>
        {
            ["model"] = effectiveModel,
            ["max_tokens"] = maxTokens,
            ["temperature"] = Math.Clamp(request.Temperature, 0.0f, 1.0f),
            ["messages"] = claudeMessages,
            ["stream"] = stream
        };

        if (!string.IsNullOrWhiteSpace(mergedSystemPrompt))
        {
            payload["system"] = mergedSystemPrompt;
        }

        // Đóng gói danh sách công cụ theo chuẩn input_schema của Claude
        if (request.Tools is { Count: > 0 })
        {
            payload["tools"] = request.Tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = t.ParametersSchema.RootElement
            }).ToList();
        }

        return payload;
    }

    /// <summary>
    /// Phản hồi dự phòng khi chưa cấu hình Claude API Key trong môi trường development/testing.
    /// </summary>
    /// <param name="request">Yêu cầu hoàn thành hội thoại ban đầu.</param>
    /// <param name="latencyMs">Độ trễ đo lường được (ms).</param>
    /// <returns>Đối tượng ChatCompletionResponse mô phỏng câu trả lời hoặc gọi tool.</returns>
    private static ChatCompletionResponse GenerateFallbackResponse(ChatCompletionRequest request, int latencyMs)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatMessageRoles.User)?.Content?.ToLower() ?? string.Empty;

        if (lastUserMsg.Contains("tour") || lastUserMsg.Contains("danh sách") || lastUserMsg.Contains("đi đâu"))
        {
            return new ChatCompletionResponse(
                Content: null,
                ToolCalls: [new ToolCall("call_mock_search", "search_travels", "{}")],
                FinishReason: "tool_calls",
                Usage: new TokenUsage(120, 25, 145),
                Model: "claude-3-5-sonnet-mock",
                LatencyMs: latencyMs);
        }

        return new ChatCompletionResponse(
            Content: "Dạ em chào anh/chị! Em là Chuyên viên tư vấn AI (Claude) của TourAgent. Anh/chị đang quan tâm đến tour du lịch nào để em hỗ trợ tư vấn và kiểm tra ngày khởi hành ưu đãi nhất ạ?",
            ToolCalls: [],
            FinishReason: "stop",
            Usage: new TokenUsage(160, 50, 210),
            Model: "claude-3-5-sonnet-mock",
            LatencyMs: latencyMs);
    }
}

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
/// Client kết nối trực tiếp với dịch vụ DeepSeek V3 API (tương thích giao thức OpenAI Chat Completions).
/// Đóng gói tính năng Function Calling / Tool Calling và đo lường độ trễ mạng.
/// </summary>
public sealed class DeepSeekLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekLlmClient> _logger;
    private readonly ILlmSettingsStore? _settingsStore;
    private readonly string _fallbackApiKey;
    private readonly string _fallbackModel;
    private readonly string _fallbackBaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Khởi tạo DeepSeekLlmClient với HttpClient, store cấu hình động và logger.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient để gửi yêu cầu mạng.</param>
    /// <param name="settingsStore">Kho lưu trữ cấu hình LLM runtime.</param>
    /// <param name="configuration">Cấu hình hệ thống IConfiguration.</param>
    /// <param name="logger">Logger ghi log hoạt động và cảnh báo.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public DeepSeekLlmClient(
        HttpClient httpClient,
        ILlmSettingsStore settingsStore,
        IConfiguration configuration,
        ILogger<DeepSeekLlmClient> logger)
    {
        _httpClient = httpClient;
        _settingsStore = settingsStore;
        _logger = logger;

        _fallbackApiKey = configuration["DeepSeek:ApiKey"]
            ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
            ?? "sk-92e03f3300c7429389be0a047169e06a";

        _fallbackModel = configuration["DeepSeek:Model"] ?? "deepseek-reasoner";
        _fallbackBaseUrl = (configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com").TrimEnd('/');
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ kiểm thử đơn vị.
    /// </summary>
    /// <param name="httpClient">Thực thể HttpClient.</param>
    /// <param name="configuration">Cấu hình chứa API Key và endpoint của DeepSeek.</param>
    /// <param name="logger">Logger ghi log hoạt động và cảnh báo.</param>
    public DeepSeekLlmClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DeepSeekLlmClient> logger)
        : this(httpClient, null!, configuration, logger)
    {
    }

    /// <summary>
    /// Lấy API Key hiện thời từ settings store hoặc fallback.
    /// </summary>
    private string GetApiKey()
    {
        var key = _settingsStore?.GetSettings().ApiKey;
        return !string.IsNullOrWhiteSpace(key) ? key : _fallbackApiKey;
    }

    /// <summary>
    /// Lấy tên model hiện thời từ settings store hoặc fallback.
    /// </summary>
    private string GetModel()
    {
        var model = _settingsStore?.GetSettings().Model;
        return !string.IsNullOrWhiteSpace(model) ? model : _fallbackModel;
    }

    /// <summary>
    /// Lấy Base URL hiện thời từ settings store hoặc fallback.
    /// </summary>
    private string GetBaseUrl()
    {
        var baseUrl = _settingsStore?.GetSettings().BaseUrl;
        return (!string.IsNullOrWhiteSpace(baseUrl) ? baseUrl : _fallbackBaseUrl).TrimEnd('/');
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
            _logger.LogWarning("DeepSeek API Key chưa được thiết lập. Đang sử dụng chế độ mô phỏng an toàn.");
            return GenerateFallbackResponse(request, (int)stopwatch.ElapsedMilliseconds);
        }

        var requestBody = BuildOpenAiPayload(request, stream: false);
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = jsonContent
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        var latencyMs = (int)stopwatch.ElapsedMilliseconds;

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("DeepSeek API lỗi ({StatusCode}): {ErrorBody}", httpResponse.StatusCode, errorBody);
            throw new HttpRequestException($"DeepSeek API trả về mã lỗi {httpResponse.StatusCode}: {errorBody}");
        }

        var responseString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        using var responseDoc = JsonDocument.Parse(responseString);
        var root = responseDoc.RootElement;

        var choices = root.GetProperty("choices");
        var firstChoice = choices[0];
        var messageProp = firstChoice.GetProperty("message");

        string? content = null;
        if (messageProp.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
        {
            content = contentProp.GetString();
        }

        string finishReason = "stop";
        if (firstChoice.TryGetProperty("finish_reason", out var finishProp) && finishProp.ValueKind == JsonValueKind.String)
        {
            finishReason = finishProp.GetString() ?? "stop";
        }

        var toolCalls = new List<ToolCall>();
        if (messageProp.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in toolCallsProp.EnumerateArray())
            {
                var id = call.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                var functionProp = call.GetProperty("function");
                var name = functionProp.GetProperty("name").GetString() ?? string.Empty;
                var arguments = functionProp.GetProperty("arguments").GetString() ?? "{}";

                toolCalls.Add(new ToolCall(id, name, arguments));
            }
        }

        int promptTokens = 0;
        int completionTokens = 0;
        int totalTokens = 0;

        if (root.TryGetProperty("usage", out var usageProp))
        {
            promptTokens = usageProp.TryGetProperty("prompt_tokens", out var pTok) ? pTok.GetInt32() : 0;
            completionTokens = usageProp.TryGetProperty("completion_tokens", out var cTok) ? cTok.GetInt32() : 0;
            totalTokens = usageProp.TryGetProperty("total_tokens", out var tTok) ? tTok.GetInt32() : (promptTokens + completionTokens);
        }

        string responseModel = GetModel();
        if (root.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String)
        {
            responseModel = modelProp.GetString() ?? GetModel();
        }

        return new ChatCompletionResponse(
            Content: content,
            ToolCalls: toolCalls,
            FinishReason: finishReason,
            Usage: new TokenUsage(promptTokens, completionTokens, totalTokens),
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
            yield return new ChatStreamChunk("Dạ em chào anh/chị, em có thể hỗ trợ gì cho chuyến du lịch sắp tới ạ?", null, "stop");
            yield break;
        }

        var requestBody = BuildOpenAiPayload(request, stream: true);
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = jsonContent
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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
            if (jsonLine == "[DONE]")
            {
                break;
            }

            ChatStreamChunk? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(jsonLine);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    string? deltaContent = null;
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentProp) &&
                        contentProp.ValueKind == JsonValueKind.String)
                    {
                        deltaContent = contentProp.GetString();
                    }

                    string? finishReason = null;
                    if (choice.TryGetProperty("finish_reason", out var finishProp) &&
                        finishProp.ValueKind == JsonValueKind.String)
                    {
                        finishReason = finishProp.GetString();
                    }

                    chunk = new ChatStreamChunk(deltaContent, null, finishReason);
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
    /// Chuyển đổi ChatCompletionRequest thành đối tượng JSON payload tương thích chuẩn OpenAI.
    /// </summary>
    private object BuildOpenAiPayload(ChatCompletionRequest request, bool stream)
    {
        var messagesPayload = new List<object>();

        foreach (var msg in request.Messages)
        {
            if (msg.Role == ChatMessageRoles.Tool)
            {
                messagesPayload.Add(new
                {
                    role = "tool",
                    tool_call_id = msg.ToolCallId,
                    content = msg.Content ?? "{}"
                });
            }
            else if (msg.Role == ChatMessageRoles.Assistant && msg.ToolCalls.Count > 0)
            {
                messagesPayload.Add(new
                {
                    role = "assistant",
                    content = msg.Content,
                    tool_calls = msg.ToolCalls.Select(t => new
                    {
                        id = t.Id,
                        type = "function",
                        function = new
                        {
                            name = t.FunctionName,
                            arguments = t.FunctionArguments
                        }
                    }).ToList()
                });
            }
            else
            {
                messagesPayload.Add(new
                {
                    role = msg.Role,
                    content = msg.Content ?? string.Empty
                });
            }
        }

        var effectiveModel = request.Model ?? GetModel();
        var isReasoner = effectiveModel.Contains("reasoner", StringComparison.OrdinalIgnoreCase);

        // Với deepseek-reasoner: Nếu có tools thì chuyển sang deepseek-chat vì deepseek-reasoner không hỗ trợ tool calling
        var finalModel = (isReasoner && request.Tools is { Count: > 0 }) ? "deepseek-chat" : effectiveModel;

        var payload = new Dictionary<string, object?>
        {
            ["model"] = finalModel,
            ["messages"] = messagesPayload,
            ["stream"] = stream
        };

        // deepseek-reasoner không chấp nhận tham số temperature
        if (!finalModel.Contains("reasoner", StringComparison.OrdinalIgnoreCase))
        {
            payload["temperature"] = request.Temperature;
        }

        if (request.Tools is { Count: > 0 } && !finalModel.Contains("reasoner", StringComparison.OrdinalIgnoreCase))
        {
            payload["tools"] = request.Tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.ParametersSchema.RootElement
                }
            }).ToList();
        }

        return payload;
    }

    /// <summary>
    /// Phản hồi dự phòng khi chưa cấu hình DeepSeek API Key trong môi trường development.
    /// </summary>
    private static ChatCompletionResponse GenerateFallbackResponse(ChatCompletionRequest request, int latencyMs)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == ChatMessageRoles.User)?.Content?.ToLower() ?? "";

        // Nếu người dùng hỏi danh sách tour, mô phỏng gọi tool search_travels
        if (lastUserMsg.Contains("tour") || lastUserMsg.Contains("danh sách") || lastUserMsg.Contains("đi đâu"))
        {
            return new ChatCompletionResponse(
                Content: null,
                ToolCalls: [new ToolCall("call_mock_search", "search_travels", "{}")],
                FinishReason: "tool_calls",
                Usage: new TokenUsage(100, 20, 120),
                Model: "deepseek-chat-mock",
                LatencyMs: latencyMs);
        }

        return new ChatCompletionResponse(
            Content: "Dạ em chào anh/chị! Em là Chuyên viên tư vấn AI của TourAgent. Anh/chị đang quan tâm đến tour du lịch nào để em hỗ trợ tư vấn và kiểm tra ngày khởi hành ưu đãi nhất ạ?",
            ToolCalls: [],
            FinishReason: "stop",
            Usage: new TokenUsage(150, 45, 195),
            Model: "deepseek-chat-mock",
            LatencyMs: latencyMs);
    }
}

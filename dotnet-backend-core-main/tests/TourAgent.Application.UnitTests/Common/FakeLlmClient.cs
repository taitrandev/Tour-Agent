using TourAgent.Application.Abstractions;

namespace TourAgent.Application.UnitTests.Common;

/// <summary>
/// Client LLM giả lập phục vụ viết Unit Test độc lập và có tính lặp lại (Hermetic Tests).
/// Cho phép lập trình trước các phản hồi và kiểm tra chuỗi lệnh gọi công cụ.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<ChatCompletionResponse> _responses = new();
    private readonly List<ChatCompletionRequest> _receivedRequests = [];

    /// <summary>
    /// Danh sách các yêu cầu LLM đã nhận được.
    /// </summary>
    public IReadOnlyList<ChatCompletionRequest> ReceivedRequests => _receivedRequests.AsReadOnly();

    /// <summary>
    /// Hàm xử lý tùy biến để sinh phản hồi động theo request.
    /// </summary>
    public Func<ChatCompletionRequest, ChatCompletionResponse>? CustomHandler { get; set; }

    /// <summary>
    /// Thêm một phản hồi hoàn chỉnh vào hàng đợi phản hồi giả lập.
    /// </summary>
    /// <param name="response">Đối tượng ChatCompletionResponse sẽ trả về.</param>
    public void EnqueueResponse(ChatCompletionResponse response)
    {
        _responses.Enqueue(response);
    }

    /// <summary>
    /// Thêm nhanh một câu trả lời văn bản vào hàng đợi.
    /// </summary>
    /// <param name="content">Nội dung phản hồi của trợ lý AI.</param>
    public void EnqueueTextResponse(string content)
    {
        _responses.Enqueue(new ChatCompletionResponse(
            Content: content,
            ToolCalls: [],
            FinishReason: "stop",
            Usage: new TokenUsage(50, 20, 70),
            Model: "fake-model",
            LatencyMs: 50));
    }

    /// <summary>
    /// Thêm nhanh một phản hồi yêu cầu gọi công cụ (Tool Call) vào hàng đợi.
    /// </summary>
    /// <param name="toolName">Tên công cụ AI muốn gọi.</param>
    /// <param name="argumentsJson">Chuỗi JSON đối số.</param>
    /// <param name="callId">Mã định danh tool call.</param>
    public void EnqueueToolCallResponse(string toolName, string argumentsJson, string callId = "call_test_01")
    {
        _responses.Enqueue(new ChatCompletionResponse(
            Content: null,
            ToolCalls: [new ToolCall(callId, toolName, argumentsJson)],
            FinishReason: "tool_calls",
            Usage: new TokenUsage(60, 15, 75),
            Model: "fake-model",
            LatencyMs: 40));
    }

    /// <inheritdoc />
    public Task<ChatCompletionResponse> CompleteChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        _receivedRequests.Add(request);

        if (CustomHandler is not null)
        {
            return Task.FromResult(CustomHandler(request));
        }

        if (_responses.Count > 0)
        {
            return Task.FromResult(_responses.Dequeue());
        }

        return Task.FromResult(new ChatCompletionResponse(
            Content: "Dạ em là AI du lịch giả lập.",
            ToolCalls: [],
            FinishReason: "stop",
            Usage: new TokenUsage(10, 10, 20),
            Model: "fake-model",
            LatencyMs: 10));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatStreamChunk> StreamChatAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _receivedRequests.Add(request);

        yield return new ChatStreamChunk("Dạ ", null, null);
        yield return new ChatStreamChunk("chào ", null, null);
        yield return new ChatStreamChunk("anh/chị!", null, "stop");
        await Task.CompletedTask;
    }
}

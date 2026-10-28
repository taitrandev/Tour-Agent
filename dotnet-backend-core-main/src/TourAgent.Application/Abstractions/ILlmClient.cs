using System.Text.Json;
using System.Text.Json.Serialization;

namespace TourAgent.Application.Abstractions;

/// <summary>
/// Đại diện cho vai trò của người gửi trong tin nhắn ngữ cảnh AI.
/// </summary>
public static class ChatMessageRoles
{
    /// <summary>
    /// Chỉ thị hệ thống (System Prompt).
    /// </summary>
    public const string System = "system";

    /// <summary>
    /// Tin nhắn từ người dùng (User Message).
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Câu trả lời từ trợ lý AI (Assistant Message).
    /// </summary>
    public const string Assistant = "assistant";

    /// <summary>
    /// Kết quả trả về sau khi thực thi công cụ (Tool Output).
    /// </summary>
    public const string Tool = "tool";
}

/// <summary>
/// Đại diện cho một tin nhắn đơn lẻ trong chuỗi hội thoại gửi tới mô hình ngôn ngữ lớn (LLM).
/// </summary>
public sealed record ChatMessage
{
    /// <summary>
    /// Khởi tạo một tin nhắn chat.
    /// </summary>
    /// <param name="role">Vai trò (system, user, assistant, tool).</param>
    /// <param name="content">Nội dung văn bản.</param>
    /// <param name="name">Tên định danh công cụ hoặc người gửi nếu có.</param>
    /// <param name="toolCallId">Mã định danh lệnh gọi công cụ tương ứng (khi role là tool).</param>
    /// <param name="toolCalls">Danh sách các lệnh gọi công cụ mà AI yêu cầu (khi role là assistant).</param>
    public ChatMessage(
        string role,
        string? content,
        string? name = null,
        string? toolCallId = null,
        IReadOnlyList<ToolCall>? toolCalls = null)
    {
        Role = role;
        Content = content;
        Name = name;
        ToolCallId = toolCallId;
        ToolCalls = toolCalls ?? [];
    }

    /// <summary>
    /// Vai trò của người gửi tin nhắn (system / user / assistant / tool).
    /// </summary>
    public string Role { get; init; }

    /// <summary>
    /// Nội dung văn bản của tin nhắn.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Tên công cụ (khi Role là "tool").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Mã định danh của tool call mà tin nhắn này phản hồi (khi Role là "tool").
    /// </summary>
    public string? ToolCallId { get; init; }

    /// <summary>
    /// Danh sách các lệnh gọi công cụ do AI đưa ra (khi Role là "assistant").
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; init; }

    /// <summary>
    /// Tạo nhanh tin nhắn hệ thống (System message).
    /// </summary>
    public static ChatMessage CreateSystem(string content) =>
        new(ChatMessageRoles.System, content);

    /// <summary>
    /// Tạo nhanh tin nhắn người dùng (User message).
    /// </summary>
    public static ChatMessage CreateUser(string content) =>
        new(ChatMessageRoles.User, content);

    /// <summary>
    /// Tạo nhanh tin nhắn từ trợ lý AI (Assistant message).
    /// </summary>
    public static ChatMessage CreateAssistant(string? content, IReadOnlyList<ToolCall>? toolCalls = null) =>
        new(ChatMessageRoles.Assistant, content, toolCalls: toolCalls);

    /// <summary>
    /// Tạo nhanh tin nhắn kết quả thực thi công cụ (Tool response message).
    /// </summary>
    public static ChatMessage CreateTool(string toolCallId, string toolName, string content) =>
        new(ChatMessageRoles.Tool, content, name: toolName, toolCallId: toolCallId);
}

/// <summary>
/// Đại diện cho một yêu cầu thực thi công cụ do AI trả về.
/// </summary>
public sealed record ToolCall(
    string Id,
    string FunctionName,
    string FunctionArguments);

/// <summary>
/// Định nghĩa cấu trúc và schema của một công cụ (Tool) đăng ký với mô hình AI.
/// </summary>
public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonDocument ParametersSchema);

/// <summary>
/// Thống kê lượng token đã tiêu thụ trong một lượt tương tác với mô hình AI.
/// </summary>
public sealed record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);

/// <summary>
/// Yêu cầu tương tác hoàn chỉnh gửi tới mô hình AI (Chat Completion Request).
/// </summary>
public sealed record ChatCompletionRequest(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ToolDefinition>? Tools = null,
    float Temperature = 0.3f,
    int? MaxTokens = null,
    string? Model = null);

/// <summary>
/// Phản hồi hoàn chỉnh nhận được từ mô hình AI (Chat Completion Response).
/// </summary>
public sealed record ChatCompletionResponse(
    string? Content,
    IReadOnlyList<ToolCall> ToolCalls,
    string FinishReason,
    TokenUsage Usage,
    string Model,
    int LatencyMs);

/// <summary>
/// Một phân đoạn văn bản hoặc dữ liệu streaming trả về từ mô hình AI.
/// </summary>
public sealed record ChatStreamChunk(
    string? DeltaContent,
    IReadOnlyList<ToolCall>? ToolCalls,
    string? FinishReason);

/// <summary>
/// Cổng giao tiếp trừu tượng tương tác với Mô hình Ngôn ngữ Lớn (LLM - DeepSeek / OpenAI).
/// Hỗ trợ cả cơ chế gọi thông thường (Request-Response) và truyền phát trực tiếp (Server-Sent Streaming).
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Gửi chuỗi tin nhắn và nhận câu trả lời hoàn chỉnh cùng các yêu cầu gọi công cụ (nếu có).
    /// </summary>
    /// <param name="request">Yêu cầu hoàn thành hội thoại kèm danh sách công cụ.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Đối tượng ChatCompletionResponse chứa nội dung và thống kê token.</returns>
    Task<ChatCompletionResponse> CompleteChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Truyền phát trực tiếp từng token (Streaming) khi mô hình đang sinh câu trả lời.
    /// </summary>
    /// <param name="request">Yêu cầu hoàn thành hội thoại.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Luồng bất đồng bộ IAsyncEnumerable trả về từng phân đoạn token.</returns>
    IAsyncEnumerable<ChatStreamChunk> StreamChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}

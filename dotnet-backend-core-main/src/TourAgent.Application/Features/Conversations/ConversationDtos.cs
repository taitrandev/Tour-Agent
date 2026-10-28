using System.Text.Json.Serialization;

namespace TourAgent.Application.Features.Conversations;

/// <summary>
/// DTO tóm tắt thông tin một phiên hội thoại phục vụ danh sách hiển thị và lọc.
/// </summary>
public sealed record ConversationSummaryDto
{
    /// <summary>
    /// Mã định danh hội thoại.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Kênh hội thoại ('telegram', 'web_widget', 'zalo').
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// Mã người dùng trên kênh ngoại bộ.
    /// </summary>
    [JsonPropertyName("channel_user_id")]
    public string ChannelUserId { get; init; } = string.Empty;

    /// <summary>
    /// Trạng thái phiên hội thoại ('bot_active', 'human_active', 'resolved').
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Mã định danh Contact CRM liên kết nếu có.
    /// </summary>
    [JsonPropertyName("contact_id")]
    public long? ContactId { get; init; }

    /// <summary>
    /// Thời điểm diễn ra tin nhắn gần nhất.
    /// </summary>
    [JsonPropertyName("last_message_at")]
    public DateTime? LastMessageAt { get; init; }

    /// <summary>
    /// Thời điểm khởi tạo phiên hội thoại.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Dữ liệu metadata bổ sung dưới dạng đối tượng (Agent State, sđt, deal info).
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }

    /// <summary>
    /// Tổng số lượng tin nhắn đã gửi trong hội thoại.
    /// </summary>
    [JsonPropertyName("messages_count")]
    public int MessagesCount { get; init; }

    /// <summary>
    /// Tổng số lượt gọi mô hình LLM DeepSeek trong hội thoại.
    /// </summary>
    [JsonPropertyName("llm_calls_count")]
    public int LlmCallsCount { get; init; }
}

/// <summary>
/// DTO chứa thông tin tiêu đề và metadata chi tiết của một phiên hội thoại.
/// </summary>
public sealed record ConversationHeaderDto
{
    /// <summary>
    /// Mã định danh hội thoại.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Kênh hội thoại.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// Mã người dùng trên kênh.
    /// </summary>
    [JsonPropertyName("channel_user_id")]
    public string ChannelUserId { get; init; } = string.Empty;

    /// <summary>
    /// Trạng thái phiên hội thoại.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Mã Contact CRM liên kết.
    /// </summary>
    [JsonPropertyName("contact_id")]
    public long? ContactId { get; init; }

    /// <summary>
    /// Dữ liệu metadata phân tích agent state.
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }

    /// <summary>
    /// Thời điểm tạo phiên hội thoại.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Thời điểm cập nhật cuối cùng.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO chi tiết một tin nhắn trong dòng thời gian hội thoại.
/// </summary>
public sealed record MessageItemDto
{
    /// <summary>
    /// Mã tin nhắn.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Mã phiên hội thoại cha.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public long ConversationId { get; init; }

    /// <summary>
    /// Vai trò người gửi ('user', 'assistant', 'tool').
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Nội dung tin nhắn.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>
    /// Tên công cụ đã thực thi nếu vai trò là Tool.
    /// </summary>
    [JsonPropertyName("tool_name")]
    public string? ToolName { get; init; }

    /// <summary>
    /// Payload kết quả thực thi của công cụ dưới dạng JSON object.
    /// </summary>
    [JsonPropertyName("tool_payload")]
    public object? ToolPayload { get; init; }

    /// <summary>
    /// Mã tin nhắn trên kênh ngoài.
    /// </summary>
    [JsonPropertyName("channel_message_id")]
    public string? ChannelMessageId { get; init; }

    /// <summary>
    /// Thời điểm gửi tin nhắn.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO chi tiết một lượt gọi mô hình LLM DeepSeek với đầy đủ telemetry và snapshot.
/// </summary>
public sealed record LlmCallItemDto
{
    /// <summary>
    /// Mã bản ghi lượt gọi.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Mã phiên hội thoại liên quan.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public long? ConversationId { get; init; }

    /// <summary>
    /// Tên mô hình AI được sử dụng.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Nhà cung cấp dịch vụ LLM.
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    /// <summary>
    /// Số lượng token ngữ cảnh đầu vào (Prompt Tokens).
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    /// <summary>
    /// Số lượng token do mô hình sinh ra (Completion Tokens).
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Chi phí tiền mặt tính bằng USD.
    /// </summary>
    [JsonPropertyName("cost_usd")]
    public decimal CostUsd { get; init; }

    /// <summary>
    /// Độ trễ phản hồi tính bằng mili-giây.
    /// </summary>
    [JsonPropertyName("latency_ms")]
    public int LatencyMs { get; init; }

    /// <summary>
    /// Danh sách tên các công cụ đã được gọi.
    /// </summary>
    [JsonPropertyName("tools_called")]
    public List<string>? ToolsCalled { get; init; }

    /// <summary>
    /// Lý do dừng sinh văn bản ('stop', 'tool_calls').
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }

    /// <summary>
    /// Snapshot thanh trạng thái XML tại lượt gọi.
    /// </summary>
    [JsonPropertyName("status_bar_snapshot")]
    public string? StatusBarSnapshot { get; init; }

    /// <summary>
    /// Snapshot trạng thái Agent State tại thời điểm gọi.
    /// </summary>
    [JsonPropertyName("state_snapshot")]
    public object? StateSnapshot { get; init; }

    /// <summary>
    /// Chi tiết arguments và kết quả của các công cụ.
    /// </summary>
    [JsonPropertyName("tool_payloads")]
    public object? ToolPayloads { get; init; }

    /// <summary>
    /// Thời điểm thực hiện cuộc gọi.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO chứa toàn bộ dữ liệu chi tiết của phiên hội thoại phục vụ màn hình Inspector.
/// </summary>
public sealed record ConversationDetailDto
{
    /// <summary>
    /// Thông tin tiêu đề phiên hội thoại.
    /// </summary>
    [JsonPropertyName("conversation")]
    public ConversationHeaderDto Conversation { get; init; } = null!;

    /// <summary>
    /// Danh sách các tin nhắn đã trao đổi theo thứ tự thời gian tăng dần.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<MessageItemDto> Messages { get; init; } = [];

    /// <summary>
    /// Danh sách các lượt gọi LLM kiểm toán theo thứ tự mới nhất trước.
    /// </summary>
    [JsonPropertyName("llm_calls")]
    public List<LlmCallItemDto> LlmCalls { get; init; } = [];

    /// <summary>
    /// Nội dung System Prompt nghiêm ngặt đang kích hoạt cho AI.
    /// </summary>
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = string.Empty;
}

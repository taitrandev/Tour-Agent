using TourAgent.Domain.Common;

namespace TourAgent.Domain.Conversations;

/// <summary>
/// Đại diện cho bản ghi kiểm toán cuộc gọi LLM (DeepSeek / OpenAI).
/// Dùng để theo dõi chi phí tiền mặt (USD), độ trễ phản hồi (ms) và số lượng token tiêu thụ.
/// </summary>
public sealed class LlmCall : Entity
{
    /// <summary>
    /// Constructor rỗng phục vụ ORM EF Core.
    /// </summary>
    private LlmCall()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng LlmCall với đầy đủ số liệu đo lường và thông tin nhà cung cấp.
    /// </summary>
    private LlmCall(
        long id,
        long? conversationId,
        string model,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        decimal costUsd,
        int latencyMs,
        string? toolsCalledJson,
        string provider = "deepseek",
        string finishReason = "stop",
        string? toolPayloadsJson = null,
        string? statusBarSnapshot = null,
        string? stateSnapshotJson = null)
        : base(id)
    {
        ConversationId = conversationId;
        Model = model;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = totalTokens;
        CostUsd = costUsd;
        LatencyMs = latencyMs;
        ToolsCalledJson = toolsCalledJson;
        Provider = string.IsNullOrWhiteSpace(provider) ? "deepseek" : provider;
        FinishReason = string.IsNullOrWhiteSpace(finishReason) ? "stop" : finishReason;
        ToolPayloadsJson = toolPayloadsJson;
        StatusBarSnapshot = statusBarSnapshot;
        StateSnapshotJson = stateSnapshotJson;
    }

    /// <summary>
    /// Mã phiên hội thoại liên quan (nếu có).
    /// </summary>
    public long? ConversationId { get; private set; }

    /// <summary>
    /// Tên model LLM đã gọi (ví dụ: 'deepseek-chat').
    /// </summary>
    public string Model { get; private set; } = string.Empty;

    /// <summary>
    /// Số lượng token đầu vào (Prompt Tokens).
    /// </summary>
    public int PromptTokens { get; private set; }

    /// <summary>
    /// Số lượng token đầu ra (Completion Tokens).
    /// </summary>
    public int CompletionTokens { get; private set; }

    /// <summary>
    /// Tổng số token tiêu thụ cho lượt gọi này.
    /// </summary>
    public int TotalTokens { get; private set; }

    /// <summary>
    /// Ước tính chi phí tính bằng USD cho lượt gọi này.
    /// </summary>
    public decimal CostUsd { get; private set; }

    /// <summary>
    /// Thời gian phản hồi đo bằng mili-giây (Latency).
    /// </summary>
    public int LatencyMs { get; private set; }

    /// <summary>
    /// Danh sách tên các công cụ đã được gọi trong lượt tương tác này (dạng JSON).
    /// </summary>
    public string? ToolsCalledJson { get; private set; }

    /// <summary>
    /// Nhà cung cấp dịch vụ LLM (ví dụ: 'deepseek', 'openai').
    /// </summary>
    public string Provider { get; private set; } = "deepseek";

    /// <summary>
    /// Lý do kết thúc sinh văn bản của LLM (ví dụ: 'stop', 'tool_calls').
    /// </summary>
    public string FinishReason { get; private set; } = "stop";

    /// <summary>
    /// Dữ liệu đầu vào và kết quả chi tiết của các tool đã thực thi (dạng JSON).
    /// </summary>
    public string? ToolPayloadsJson { get; private set; }

    /// <summary>
    /// Ảnh chụp trạng thái thanh trạng thái hệ thống XML tại thời điểm gọi LLM.
    /// </summary>
    public string? StatusBarSnapshot { get; private set; }

    /// <summary>
    /// Ảnh chụp trạng thái Agent (Goal, Facts, Workflow Status) tại thời điểm gọi LLM (dạng JSON).
    /// </summary>
    public string? StateSnapshotJson { get; private set; }

    /// <summary>
    /// Phương thức Factory ghi nhận một lượt gọi LLM mới.
    /// </summary>
    /// <param name="conversationId">Mã phiên hội thoại.</param>
    /// <param name="model">Tên model AI.</param>
    /// <param name="promptTokens">Token đầu vào.</param>
    /// <param name="completionTokens">Token đầu ra.</param>
    /// <param name="latencyMs">Độ trễ mili-giây.</param>
    /// <param name="costUsd">Chi phí USD.</param>
    /// <param name="toolsCalledJson">JSON danh sách công cụ đã gọi.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <param name="provider">Nhà cung cấp dịch vụ LLM.</param>
    /// <param name="finishReason">Lý do kết thúc của LLM.</param>
    /// <param name="toolPayloadsJson">Payload của các công cụ.</param>
    /// <param name="statusBarSnapshot">Ảnh chụp status bar XML.</param>
    /// <param name="stateSnapshotJson">Ảnh chụp state JSON.</param>
    /// <returns>Result chứa LlmCall mới.</returns>
    public static Result<LlmCall> Create(
        long? conversationId,
        string model,
        int promptTokens,
        int completionTokens,
        int latencyMs,
        decimal costUsd,
        string? toolsCalledJson = null,
        long id = 0,
        string provider = "deepseek",
        string finishReason = "stop",
        string? toolPayloadsJson = null,
        string? statusBarSnapshot = null,
        string? stateSnapshotJson = null)
    {
        var call = new LlmCall(
            id,
            conversationId,
            model.Trim(),
            promptTokens,
            completionTokens,
            promptTokens + completionTokens,
            costUsd,
            latencyMs,
            toolsCalledJson,
            string.IsNullOrWhiteSpace(provider) ? "deepseek" : provider,
            string.IsNullOrWhiteSpace(finishReason) ? "stop" : finishReason,
            toolPayloadsJson,
            statusBarSnapshot,
            stateSnapshotJson);

        return Result.Success(call);
    }
}

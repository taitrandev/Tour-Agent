namespace TourAgent.Application.Agent.Contracts;

/// <summary>
/// Dữ liệu đầu vào yêu cầu phiên chat với AI Agent.
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại trong CSDL.</param>
/// <param name="UserMessage">Nội dung tin nhắn người dùng gửi đến.</param>
/// <param name="CurrentTravelId">Mã tour người dùng đang trực tiếp xem trên trang web (nếu có).</param>
/// <param name="CurrentTravelTitle">Tiêu đề tour đang xem.</param>
/// <param name="Country">Quốc gia của tour đang xem.</param>
public sealed record AgentChatInput(
    long ConversationId,
    string UserMessage,
    long? CurrentTravelId = null,
    string? CurrentTravelTitle = null,
    string? Country = null);

/// <summary>
/// Kết quả trả về sau khi Agent hoàn thành toàn bộ chu trình suy luận và gọi công cụ (Agent Loop).
/// </summary>
/// <param name="ResponseText">Nội dung câu trả lời cuối cùng dành cho khách hàng.</param>
/// <param name="ToolsExecuted">Danh sách tên các công cụ đã được AI chủ động kích hoạt.</param>
/// <param name="TotalTokens">Tổng số lượng token tiêu thụ.</param>
/// <param name="EstimatedCostUsd">Chi phí ước tính (USD).</param>
/// <param name="TotalLatencyMs">Tổng thời gian xử lý (mili-giây).</param>
/// <param name="TurnsCount">Số lượt gọi mô hình (Turns) trong phiên.</param>
/// <param name="IsEscalated">Trạng thái đã chuyển tiếp cho nhân viên hay chưa.</param>
public sealed record AgentChatResult(
    string ResponseText,
    IReadOnlyList<string> ToolsExecuted,
    int TotalTokens,
    decimal EstimatedCostUsd,
    int TotalLatencyMs,
    int TurnsCount,
    bool IsEscalated,
    long? ConversationId = null);

/// <summary>
/// Đại diện cho một sự kiện phát ra trong chuỗi truyền phát trực tiếp (Server-Sent Event).
/// </summary>
/// <param name="EventType">Loại sự kiện (token, tool_start, tool_end, completed, error).</param>
/// <param name="Content">Nội dung token hoặc thông báo chi tiết.</param>
/// <param name="ToolName">Tên công cụ liên quan nếu có.</param>
public sealed record AgentStreamEvent(
    string EventType,
    string? Content,
    string? ToolName = null);

/// <summary>
/// Trình điều phối chu trình suy luận đa lượt tự động (Autonomous Multi-Turn Agent Loop).
/// Tự động nạp lịch sử chat, phân tích ý định, kích hoạt công cụ (Tools), kiểm soát an toàn (Guardrails)
/// và ghi nhận nhật ký kiểm toán (LlmCalls).
/// </summary>
public interface IAgentLoop
{
    /// <summary>
    /// Chạy trọn vẹn chu trình suy luận và trả về kết quả cuối cùng.
    /// </summary>
    /// <param name="input">Dữ liệu yêu cầu hội thoại.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ.</param>
    /// <returns>Đối tượng AgentChatResult chứa câu trả lời và số liệu thống kê.</returns>
    Task<AgentChatResult> RunAsync(
        AgentChatInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chạy chu trình suy luận kèm truyền phát trực tiếp từng token và sự kiện gọi tool (Streaming).
    /// </summary>
    /// <param name="input">Dữ liệu yêu cầu hội thoại.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ.</param>
    /// <returns>Luồng bất đồng bộ IAsyncEnumerable phát ra các sự kiện AgentStreamEvent.</returns>
    IAsyncEnumerable<AgentStreamEvent> StreamRunAsync(
        AgentChatInput input,
        CancellationToken cancellationToken = default);
}

namespace TourAgent.Application.Agent.Contracts;

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
/// <param name="ConversationId">Mã định danh phiên hội thoại.</param>
public sealed record AgentChatResult(
    string ResponseText,
    IReadOnlyList<string> ToolsExecuted,
    int TotalTokens,
    decimal EstimatedCostUsd,
    int TotalLatencyMs,
    int TurnsCount,
    bool IsEscalated,
    long? ConversationId = null);


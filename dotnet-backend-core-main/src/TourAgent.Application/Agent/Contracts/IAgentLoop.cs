namespace TourAgent.Application.Agent.Contracts;

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

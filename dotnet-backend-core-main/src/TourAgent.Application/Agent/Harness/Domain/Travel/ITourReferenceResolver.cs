namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using TourAgent.Application.Agent.Harness.State;
using TourAgent.Domain.Conversations;

/// <summary>
/// Giao diện bộ phân giải định danh Tour du lịch 5 tầng (Kỹ thuật D2: Tour Reference Resolver).
/// Phân giải chính xác Tour mà khách hàng đang quan tâm hoặc muốn chốt với độ tin cậy và kiểm toán tuyệt đối.
/// </summary>
public interface ITourReferenceResolver
{
    /// <summary>
    /// Phân giải Tour từ các nguồn ngữ cảnh qua 5 tầng ưu tiên (Explicit ➔ State ➔ Metadata ➔ Text Matching ➔ None).
    /// </summary>
    /// <param name="conversation">Phiên hội thoại hiện tại (nếu có).</param>
    /// <param name="argumentsJson">Chuỗi JSON đối số truyền vào tool (nếu có).</param>
    /// <param name="state">Trạng thái AgentState hiện tại từ State Store.</param>
    /// <param name="contextText">Văn bản ngữ cảnh bổ sung (tin nhắn khách, lý do chuyển tiếp...).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng TourResolutionResult chứa TravelId, Confidence và Method.</returns>
    Task<TourResolutionResult> ResolveAsync(
        Conversation? conversation,
        string? argumentsJson,
        AgentState? state,
        string? contextText = null,
        CancellationToken cancellationToken = default);
}

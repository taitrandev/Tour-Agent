namespace TourAgent.Application.Agent.Harness.State;

using TourAgent.Domain.Common;

/// <summary>
/// Giao diện kho lưu trữ trạng thái có cấu trúc của Agent (Kỹ thuật G1: Agent State Store).
/// Hỗ trợ đọc, cập nhật từng phần và kiểm soát xung đột lạc quan (Optimistic Concurrency Control - OCC) qua Version.
/// </summary>
public interface IAgentStateStore
{
    /// <summary>
    /// Lấy trạng thái hiện tại của phiên hội thoại từ cơ sở dữ liệu.
    /// Nếu phiên chưa từng có trạng thái, tự động khởi tạo trạng thái mặc định với Version = 1.
    /// </summary>
    /// <param name="conversationId">Mã định danh phiên hội thoại.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng AgentState có cấu trúc.</returns>
    Task<AgentState> GetAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu trạng thái mới với cơ chế kiểm tra phiên bản kỳ vọng (OCC Check).
    /// </summary>
    /// <param name="conversationId">Mã định danh phiên hội thoại.</param>
    /// <param name="state">Trạng thái mới cần lưu.</param>
    /// <param name="expectedVersion">Phiên bản kỳ vọng hiện tại trong CSDL để phát hiện xung đột.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result chứa AgentState sau khi tăng version thành công, hoặc lỗi Conflict nếu có xung đột.</returns>
    Task<Result<AgentState>> SaveAsync(long conversationId, AgentState state, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực hiện cập nhật nhanh các thuộc tính trạng thái (Patch) trong một giao dịch an toàn.
    /// </summary>
    /// <param name="conversationId">Mã định danh phiên hội thoại.</param>
    /// <param name="updateAction">Hành động cập nhật các trường trên AgentState.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Trạng thái mới nhất sau khi cập nhật.</returns>
    Task<AgentState> PatchAsync(long conversationId, Action<AgentState> updateAction, CancellationToken cancellationToken = default);
}

namespace TourAgent.Application.Agent.Harness.StatusBar;

using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Giao diện cắm ghép (Plugin Interface) cho phép các module nghiệp vụ (Tour, Khách sạn, Visa...)
/// tự động cung cấp dữ liệu phản xạ trạng thái để nhúng vào thanh trạng thái Agent Status Bar (Kỹ thuật G2).
/// </summary>
public interface IAgentStatusProvider
{
    /// <summary>
    /// Kiểm tra provider có hỗ trợ domain tương ứng không (ví dụ: 'travel', 'general').
    /// </summary>
    /// <param name="domain">Tên định danh domain.</param>
    /// <returns>True nếu hỗ trợ domain này, ngược lại là False.</returns>
    bool Supports(string domain);

    /// <summary>
    /// Cung cấp từ điển các thuộc tính phản xạ trạng thái thực tế để đóng gói vào XML.
    /// </summary>
    /// <param name="state">Trạng thái hiện tại của phiên hội thoại từ State Store.</param>
    /// <returns>Từ điển các thuộc tính cần nhúng vào thẻ domain_context.</returns>
    IReadOnlyDictionary<string, object> ProvideStatusBarData(AgentState state);
}

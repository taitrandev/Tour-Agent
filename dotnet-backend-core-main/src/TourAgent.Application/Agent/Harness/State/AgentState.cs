namespace TourAgent.Application.Agent.Harness.State;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Đại diện cho trạng thái có cấu trúc và phiên bản của phiên AI Agent (Versioned Agent State Container).
/// Lưu trữ các sự thật đã xác nhận (Confirmed Facts), trạng thái nghiệp vụ (Domain State)
/// và phục vụ kiểm soát xung đột lạc quan (Optimistic Concurrency Control - OCC) qua Version.
/// </summary>
public sealed class AgentState
{
    /// <summary>
    /// Mã định danh phiên hội thoại liên quan.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public long ConversationId { get; set; }

    /// <summary>
    /// Phiên bản của trạng thái, tự động tăng dần để phòng chống xung đột ghi đè dữ liệu.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Mục tiêu hiện tại của Agent trong cuộc trò chuyện (ví dụ: 'consulting', 'booking', 'handling_complaint').
    /// </summary>
    [JsonPropertyName("current_goal")]
    public string CurrentGoal { get; set; } = "consulting";

    /// <summary>
    /// Trạng thái tiến trình nghiệp vụ (ví dụ: 'in_progress', 'escalated', 'completed').
    /// </summary>
    [JsonPropertyName("workflow_status")]
    public string WorkflowStatus { get; set; } = "in_progress";

    /// <summary>
    /// Bộ nhớ bất biến lưu các sự thật đã được xác nhận chắc chắn (SĐT, tên khách, ngày khởi hành, số khách).
    /// </summary>
    [JsonPropertyName("confirmed_facts")]
    public Dictionary<string, string> ConfirmedFacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Trạng thái dữ liệu nghiệp vụ theo từng domain (ví dụ: domain 'travel' lưu danh sách tour đã xem, tour đang chốt).
    /// </summary>
    [JsonPropertyName("domain_state")]
    public Dictionary<string, object> DomainState { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Thời điểm cập nhật trạng thái gần nhất theo chuẩn UTC.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Khởi tạo một đối tượng AgentState mặc định cho phiên hội thoại mới.
    /// </summary>
    /// <param name="conversationId">Mã định danh phiên hội thoại.</param>
    /// <returns>Đối tượng AgentState ban đầu với version = 1.</returns>
    public static AgentState CreateInitial(long conversationId)
    {
        return new AgentState
        {
            ConversationId = conversationId,
            Version = 1,
            CurrentGoal = "consulting",
            WorkflowStatus = "in_progress",
            ConfirmedFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            DomainState = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Thêm hoặc cập nhật một sự thật đã được xác thực vào bộ nhớ bất biến của Agent.
    /// </summary>
    /// <param name="key">Tên thuộc tính (ví dụ: 'customer_phone', 'customer_name', 'group_size').</param>
    /// <param name="value">Giá trị của thuộc tính.</param>
    public void SetFact(string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
        {
            ConfirmedFacts[key.Trim()] = value.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Lấy giá trị của một sự thật đã xác nhận nếu tồn tại.
    /// </summary>
    /// <param name="key">Tên thuộc tính cần tra cứu.</param>
    /// <returns>Giá trị chuỗi hoặc null nếu chưa có.</returns>
    public string? GetFact(string key)
    {
        return ConfirmedFacts.TryGetValue(key, out var val) ? val : null;
    }
}

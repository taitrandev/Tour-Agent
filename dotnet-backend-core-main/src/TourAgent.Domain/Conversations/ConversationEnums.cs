namespace TourAgent.Domain.Conversations;

/// <summary>
/// Trạng thái điều phối phiên đàm thoại giữa Khách hàng và AI Agent.
/// </summary>
public enum ConversationStatus
{
    /// <summary>
    /// AI Agent tự động tiếp khách 24/7.
    /// </summary>
    BotActive = 1,

    /// <summary>
    /// Đã chuyển giao cho nhân viên tư vấn con người tiếp quản (Escalated).
    /// </summary>
    HumanActive = 2,

    /// <summary>
    /// Phiên tư vấn đã kết thúc/chốt đơn thành công.
    /// </summary>
    Resolved = 3
}

/// <summary>
/// Vai trò của người gửi trong dòng tin nhắn.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Tin nhắn từ người dùng/khách hàng.
    /// </summary>
    User = 1,

    /// <summary>
    /// Câu trả lời phản hồi từ AI Assistant.
    /// </summary>
    Assistant = 2,

    /// <summary>
    /// Kết quả thực thi công cụ kỹ thuật từ hệ thống (Tool calling result).
    /// </summary>
    Tool = 3,

    /// <summary>
    /// Chỉ thị hệ thống (System Prompt / Guardrails).
    /// </summary>
    System = 4
}

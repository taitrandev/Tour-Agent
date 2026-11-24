namespace TourAgent.Application.Agent.Contracts;

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


namespace TourAgent.Application.Agent.Contracts;

/// <summary>
/// Định nghĩa các vai trò (Persona / Scope) của AI Agent trong hệ thống TourAgent.
/// Giúp phân tách quyền hạn công cụ (Tool Scoping) và cá tính phản hồi,
/// ngăn chặn lỗ hổng leo thang đặc quyền (Privilege Escalation) từ người dùng công cộng.
/// </summary>
public enum AgentRole
{
    /// <summary>
    /// Chuyên viên tư vấn du lịch dành cho khách hàng trên Landing Page, Tour Page và Telegram.
    /// Chỉ được phép sử dụng các công cụ tra cứu (Read Tools) và chuyển tiếp CSKH (escalate_to_human).
    /// </summary>
    CustomerConsultant = 1,

    /// <summary>
    /// Trợ lý điều hành AI Commander dành riêng cho Quản trị viên tại trang /admin.
    /// Có toàn quyền sử dụng cả công cụ tra cứu lẫn công cụ cập nhật dữ liệu (Write Tools).
    /// </summary>
    AdminCommander = 2
}


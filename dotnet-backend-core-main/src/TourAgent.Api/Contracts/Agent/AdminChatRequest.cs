namespace TourAgent.Api.Contracts.Agent;

/// <summary>
/// Yêu cầu gửi câu lệnh điều hành cho AI Agent Commander (dành riêng cho Quản trị viên).
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại điều hành (nếu có, để trống hệ thống sẽ tự cấp mới).</param>
/// <param name="Message">Nội dung câu lệnh hoặc yêu cầu của Quản trị viên.</param>
/// <param name="AdminUsername">Tên định danh của Quản trị viên (nếu có).</param>
/// <param name="PageContext">Ngữ cảnh màn hình trang hiện tại (ví dụ: Products, Offers, Orders...).</param>
public sealed record AdminChatRequest(
    long? ConversationId = null,
    string Message = "",
    string? AdminUsername = null,
    string? PageContext = null);


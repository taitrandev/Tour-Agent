namespace TourAgent.Application.Agent.Contracts;

/// <summary>
/// Dữ liệu đầu vào yêu cầu phiên chat với AI Agent.
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại trong CSDL.</param>
/// <param name="UserMessage">Nội dung tin nhắn người dùng gửi đến.</param>
/// <param name="CurrentTravelId">Mã tour người dùng đang trực tiếp xem trên trang web (nếu có).</param>
/// <param name="CurrentTravelTitle">Tiêu đề tour đang xem.</param>
/// <param name="Country">Quốc gia của tour đang xem.</param>
/// <param name="Role">Vai trò của Agent trong phiên hội thoại (Mặc định: CustomerConsultant).</param>
/// <param name="AdminUsername">Tên định danh của Quản trị viên (chỉ dùng khi Role = AdminCommander).</param>
/// <param name="PageContext">Ngữ cảnh màn hình trang hiện tại (chỉ dùng khi Role = AdminCommander).</param>
public sealed record AgentChatInput(
    long ConversationId,
    string UserMessage,
    long? CurrentTravelId = null,
    string? CurrentTravelTitle = null,
    string? Country = null,
    AgentRole Role = AgentRole.CustomerConsultant,
    string? AdminUsername = null,
    string? PageContext = null);


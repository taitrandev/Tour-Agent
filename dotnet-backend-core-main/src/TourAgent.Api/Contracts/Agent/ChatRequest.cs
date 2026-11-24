namespace TourAgent.Api.Contracts.Agent;

/// <summary>
/// Yêu cầu gửi tin nhắn trò chuyện với AI Agent tư vấn du lịch (dành cho khách hàng).
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại (nếu có, để trống hệ thống sẽ tự cấp mới).</param>
/// <param name="Message">Nội dung câu hỏi của khách hàng.</param>
/// <param name="CurrentTravelId">Mã tour khách hàng đang xem trên trang (nếu có).</param>
/// <param name="CurrentTravelTitle">Tiêu đề tour đang xem.</param>
/// <param name="Country">Quốc gia của tour đang xem.</param>
public sealed record ChatRequest(
    long? ConversationId = null,
    string Message = "",
    long? CurrentTravelId = null,
    string? CurrentTravelTitle = null,
    string? Country = null);


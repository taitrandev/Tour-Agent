namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Giao diện bộ thu nhỏ và đồng bộ trạng thái Tour du lịch (Kỹ thuật D1: Tour State Reducer).
/// Tự động cập nhật domain_state['travel'] dựa trên các sự kiện kích hoạt công cụ và phản hồi của khách.
/// </summary>
public interface ITourStateReducer
{
    /// <summary>
    /// Lấy hoặc khởi tạo cấu trúc TravelDomainState từ AgentState.
    /// </summary>
    /// <param name="state">Trạng thái tổng thể của Agent.</param>
    /// <returns>Đối tượng TravelDomainState có cấu trúc.</returns>
    TravelDomainState GetTravelState(AgentState state);

    /// <summary>
    /// Lưu trạng thái TravelDomainState vào từ điển DomainState của AgentState.
    /// </summary>
    /// <param name="state">Trạng thái tổng thể của Agent.</param>
    /// <param name="travelState">Trạng thái nghiệp vụ Tour cần lưu.</param>
    void SaveTravelState(AgentState state, TravelDomainState travelState);

    /// <summary>
    /// Điều chỉnh máy trạng thái Tour sau khi một công cụ vừa được thực thi qua Guardrail Pipeline.
    /// </summary>
    /// <param name="state">Trạng thái tổng thể của Agent.</param>
    /// <param name="toolName">Tên công cụ vừa chạy (search_travels, get_travel_itinerary...).</param>
    /// <param name="argumentsJson">Chuỗi JSON tham số gọi công cụ.</param>
    /// <param name="resultJson">Chuỗi JSON kết quả công cụ trả về.</param>
    void ReduceAfterToolExecuted(AgentState state, string toolName, string argumentsJson, string resultJson);

    /// <summary>
    /// Phân tích tin nhắn của khách hàng để nhận diện các thay đổi ý định (từ chối tour, đổi tour).
    /// </summary>
    /// <param name="state">Trạng thái tổng thể của Agent.</param>
    /// <param name="userMessage">Nội dung tin nhắn khách hàng gửi.</param>
    void ReduceFromUserMessage(AgentState state, string userMessage);
}

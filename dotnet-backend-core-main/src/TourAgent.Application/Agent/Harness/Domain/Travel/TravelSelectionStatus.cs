namespace TourAgent.Application.Agent.Harness.Domain.Travel;

/// <summary>
/// Các trạng thái trong máy trạng thái lựa chọn tour du lịch (Travel Selection State Machine - Kỹ thuật D1).
/// Theo dõi hành trình khách hàng từ lúc nhắc tên đến khi chốt cọc tour.
/// </summary>
public enum TravelSelectionStatus
{
    /// <summary>
    /// Khách chỉ mới nhắc đến tên tour hoặc địa danh (chưa tìm kiếm).
    /// </summary>
    Mentioned,

    /// <summary>
    /// Đã thực thi tìm kiếm và có danh sách 2-5 tour ứng viên.
    /// </summary>
    Candidate,

    /// <summary>
    /// Đang tập trung đề xuất/xem chi tiết 1 tour cụ thể (lịch trình, chính sách, ngày đi).
    /// </summary>
    Proposed,

    /// <summary>
    /// Khách hàng đã đồng ý chốt tour hoặc chuyển sang nhân viên làm thủ tục cọc.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Khách từ chối tour (chê giá đắt, không hợp lịch) và chuyển sang tìm tour khác.
    /// </summary>
    Rejected
}

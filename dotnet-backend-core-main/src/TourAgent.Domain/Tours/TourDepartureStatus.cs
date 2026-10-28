namespace TourAgent.Domain.Tours;

/// <summary>
/// Trạng thái mở bán của một đợt khởi hành tour.
/// </summary>
public enum TourDepartureStatus
{
    /// <summary>
    /// Đang mở bán, còn chỗ trống.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Đã hết chỗ nhận khách.
    /// </summary>
    Full = 2,

    /// <summary>
    /// Đã đóng đợt khởi hành (hết hạn hoặc dừng bay).
    /// </summary>
    Closed = 3,

    /// <summary>
    /// Chắc chắn khởi hành.
    /// </summary>
    Guaranteed = 4
}

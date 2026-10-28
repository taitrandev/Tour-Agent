namespace TourAgent.Domain.Offers;

/// <summary>
/// Trạng thái hoạt động của gói Grand Slam Offer.
/// </summary>
public enum OfferStatus
{
    /// <summary>
    /// Bản nháp, chưa công khai cho khách hàng.
    /// </summary>
    Draft,

    /// <summary>
    /// Đang hoạt động, được hiển thị và cho phép khách đăng ký.
    /// </summary>
    Active,

    /// <summary>
    /// Đã lưu trữ hoặc tạm ngừng cung cấp.
    /// </summary>
    Archived
}

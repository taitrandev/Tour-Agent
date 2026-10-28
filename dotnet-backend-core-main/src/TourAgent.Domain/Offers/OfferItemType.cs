namespace TourAgent.Domain.Offers;

/// <summary>
/// Loại hạng mục trong Bảng xếp chồng giá trị (Value Stack).
/// </summary>
public enum OfferItemType
{
    /// <summary>
    /// Hạng mục cốt lõi của chuyến đi (Vé máy bay, Khách sạn, v.v.).
    /// </summary>
    Core,

    /// <summary>
    /// Quà tặng gia tăng giá trị (Bonus, Voucher, Cẩm nang, Trợ lý AI).
    /// </summary>
    Bonus
}

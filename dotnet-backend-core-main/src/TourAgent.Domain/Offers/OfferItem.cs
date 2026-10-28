using TourAgent.Domain.Common;

namespace TourAgent.Domain.Offers;

/// <summary>
/// Thực thể biểu diễn một hạng mục trong Bảng xếp chồng giá trị (Value Stack Item).
/// Thuộc về một Grand Slam Offer duy nhất.
/// </summary>
public sealed class OfferItem : Entity<long>
{
    /// <summary>
    /// Khởi tạo một OfferItem rỗng cho EF Core materialization.
    /// </summary>
    private OfferItem()
    {
    }

    /// <summary>
    /// Mã định danh của Offer sở hữu hạng mục này.
    /// </summary>
    public long OfferId { get; private set; }

    /// <summary>
    /// Tên hạng mục (ví dụ: "Vé máy bay khứ hồi Vietnam Airlines", "[BONUS 1] Bản đồ ẩm thực").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết giá trị của hạng mục.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Loại hạng mục: Hạng mục chính (Core) hoặc Quà tặng (Bonus).
    /// </summary>
    public OfferItemType Type { get; private set; } = OfferItemType.Core;

    /// <summary>
    /// Giá trị niêm yết độc lập của hạng mục khi chưa đóng gói vào combo (VNĐ).
    /// </summary>
    public decimal OriginalValue { get; private set; }

    /// <summary>
    /// Thứ tự hiển thị sắp xếp trong Value Stack.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Cờ đánh dấu làm nổi bật hạng mục (thường dành cho các Bonus hấp dẫn).
    /// </summary>
    public bool IsHighlighted { get; private set; }

    /// <summary>
    /// Thời điểm khởi tạo bản ghi.
    /// </summary>
    public DateTime CreatedAt => CreatedAtUtc;

    /// <summary>
    /// Thời điểm cập nhật lần cuối.
    /// </summary>
    public DateTime? UpdatedAt => UpdatedAtUtc;


    /// <summary>
    /// Navigation property trỏ về Offer cha.
    /// </summary>
    public Offer? Offer { get; private set; }

    /// <summary>
    /// Factory method khởi tạo một hạng mục Value Stack mới.
    /// </summary>
    /// <param name="offerId">Mã định danh gói Offer.</param>
    /// <param name="name">Tên hạng mục.</param>
    /// <param name="description">Mô tả chi tiết giá trị.</param>
    /// <param name="type">Loại hạng mục (Core hoặc Bonus).</param>
    /// <param name="originalValue">Giá trị niêm yết.</param>
    /// <param name="sortOrder">Thứ tự hiển thị.</param>
    /// <param name="isHighlighted">Đánh dấu nổi bật.</param>
    /// <returns>Thực thể OfferItem mới.</returns>
    public static OfferItem Create(
        long offerId,
        string name,
        string? description,
        OfferItemType type,
        decimal originalValue,
        int sortOrder = 0,
        bool isHighlighted = false)
    {
        return new OfferItem
        {
            OfferId = offerId,
            Name = name,
            Description = description,
            Type = type,
            OriginalValue = originalValue,
            SortOrder = sortOrder,
            IsHighlighted = isHighlighted,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của hạng mục Value Stack.
    /// </summary>
    /// <param name="name">Tên hạng mục.</param>
    /// <param name="description">Mô tả giá trị.</param>
    /// <param name="type">Loại hạng mục.</param>
    /// <param name="originalValue">Giá trị định lượng.</param>
    /// <param name="sortOrder">Thứ tự hiển thị.</param>
    /// <param name="isHighlighted">Có làm nổi bật không.</param>
    public void Update(
        string name,
        string? description,
        OfferItemType type,
        decimal originalValue,
        int sortOrder,
        bool isHighlighted)
    {
        Name = name;
        Description = description;
        Type = type;
        OriginalValue = originalValue;
        SortOrder = sortOrder;
        IsHighlighted = isHighlighted;
        UpdatedAtUtc = DateTime.UtcNow;
    }

}

using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Domain.Offers;

/// <summary>
/// Thực thể biểu diễn một gói Grand Slam Offer (theo triết lý $100M Offers của Alex Hormozi và Sabri Suby).
/// Đóng gói từ một sản phẩm Tour gốc (Travel) cùng với Bảng xếp chồng giá trị (Value Stack).
/// </summary>
public sealed class Offer : Entity<long>
{
    private readonly List<OfferItem> _items = new();

    /// <summary>
    /// Khởi tạo một Offer rỗng cho EF Core materialization.
    /// </summary>
    private Offer()
    {
    }

    /// <summary>
    /// Mã định danh người dùng quản trị viên tạo offer (nếu có).
    /// </summary>
    public long? UserId { get; private set; }

    /// <summary>
    /// Mã định danh Tour sản phẩm gốc liên kết (Travel).
    /// </summary>
    public long? TravelId { get; private set; }

    /// <summary>
    /// Tiêu đề gói Offer (ví dụ: "Gói Combo Nghỉ Dưỡng 4 Chạm Thảnh Thơi (3N2Đ)").
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Đường dẫn định danh duy nhất (URL Slug).
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Khẩu hiệu ngắn gọn, thu hút (Tagline).
    /// </summary>
    public string? Tagline { get; private set; }

    /// <summary>
    /// Chân dung khách hàng mục tiêu của gói ưu đãi.
    /// </summary>
    public string? TargetAudience { get; private set; }

    /// <summary>
    /// Cơ chế độc quyền giải quyết vấn đề (Hero Mechanism).
    /// </summary>
    public string? HeroMechanism { get; private set; }

    /// <summary>
    /// Lời hứa cốt lõi cam kết mang lại cho khách hàng.
    /// </summary>
    public string? CorePromise { get; private set; }

    /// <summary>
    /// Tổng giá trị niêm yết ban đầu của toàn bộ các hạng mục Value Stack (VNĐ).
    /// </summary>
    public decimal OriginalPrice { get; private set; }

    /// <summary>
    /// Mức giá ưu đãi đặc quyền khi chốt combo trọn gói (VNĐ).
    /// </summary>
    public decimal OfferPrice { get; private set; }

    /// <summary>
    /// Tỷ lệ tiền cọc đợt 1 (Mặc định 30%).
    /// </summary>
    public int DepositPercentage { get; private set; } = 30;

    /// <summary>
    /// Số tiền cọc cần thanh toán đợt 1 (Tự động tính: OfferPrice * DepositPercentage / 100).
    /// </summary>
    public decimal DepositAmount { get; private set; }

    /// <summary>
    /// Thời lượng số ngày của tour.
    /// </summary>
    public int DurationDays { get; private set; } = 3;

    /// <summary>
    /// Thời lượng số đêm nghỉ dưỡng.
    /// </summary>
    public int DurationNights { get; private set; } = 2;

    /// <summary>
    /// Số lượng nhóm/slot tối đa mở bán trong một tuần (Tạo tính khan hiếm có thật).
    /// </summary>
    public int MaxSlotsPerWeek { get; private set; } = 4;

    /// <summary>
    /// Số lượng slot còn trống hiện tại trong tuần.
    /// </summary>
    public int AvailableSlots { get; private set; } = 4;

    /// <summary>
    /// Số giờ giới hạn giữ mức giá ưu đãi (Tạo tính khẩn cấp Urgency).
    /// </summary>
    public int UrgencyHours { get; private set; } = 24;

    /// <summary>
    /// Chuỗi JSON lưu trữ danh sách các cam kết bảo hành không thể từ chối (Guarantees).
    /// </summary>
    public string? Guarantees { get; private set; }

    /// <summary>
    /// Chuỗi JSON lưu trữ các điều khoản thanh toán chia theo từng đợt (Payment Terms).
    /// </summary>
    public string? PaymentTerms { get; private set; }

    /// <summary>
    /// Kịch bản bán hàng và kịch bản tư vấn nhanh cho AI Agent (Sales Script).
    /// </summary>
    public string? SalesScript { get; private set; }

    /// <summary>
    /// Trạng thái phát hành của gói Offer (Draft, Active, Archived).
    /// </summary>
    public OfferStatus Status { get; private set; } = OfferStatus.Draft;

    /// <summary>
    /// Đánh dấu là Offer nổi bật trên trang chủ / landing page.
    /// </summary>
    public bool IsFeatured { get; private set; }

    /// <summary>
    /// Thời điểm khởi tạo bản ghi.
    /// </summary>
    public DateTime CreatedAt => CreatedAtUtc;

    /// <summary>
    /// Thời điểm cập nhật lần cuối.
    /// </summary>
    public DateTime? UpdatedAt => UpdatedAtUtc;


    /// <summary>
    /// Thời điểm xóa mềm (nếu có).
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Navigation property tới sản phẩm Tour liên kết.
    /// </summary>
    public Tour? Travel { get; private set; }

    /// <summary>
    /// Danh sách các hạng mục chi tiết trong Bảng xếp chồng giá trị (Value Stack Items).
    /// </summary>
    public IReadOnlyCollection<OfferItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Factory method tạo mới một gói Grand Slam Offer.
    /// </summary>
    public static Offer Create(
        string title,
        string slug,
        decimal originalPrice,
        decimal offerPrice,
        long? travelId = null,
        string? tagline = null,
        string? targetAudience = null,
        string? heroMechanism = null,
        string? corePromise = null,
        int depositPercentage = 30,
        int durationDays = 3,
        int durationNights = 2,
        int maxSlotsPerWeek = 4,
        int availableSlots = 4,
        int urgencyHours = 24,
        string? guarantees = null,
        string? paymentTerms = null,
        string? salesScript = null,
        OfferStatus status = OfferStatus.Draft,
        bool isFeatured = false,
        long? userId = null)
    {
        var depositAmount = Math.Round((offerPrice * depositPercentage) / 100m, 2);

        return new Offer
        {
            Title = title,
            Slug = slug,
            OriginalPrice = originalPrice,
            OfferPrice = offerPrice,
            TravelId = travelId,
            Tagline = tagline,
            TargetAudience = targetAudience,
            HeroMechanism = heroMechanism,
            CorePromise = corePromise,
            DepositPercentage = depositPercentage,
            DepositAmount = depositAmount,
            DurationDays = durationDays,
            DurationNights = durationNights,
            MaxSlotsPerWeek = maxSlotsPerWeek,
            AvailableSlots = availableSlots,
            UrgencyHours = urgencyHours,
            Guarantees = guarantees,
            PaymentTerms = paymentTerms,
            SalesScript = salesScript,
            Status = status,
            IsFeatured = isFeatured,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    }

    /// <summary>
    /// Cập nhật thông tin chi tiết gói Offer.
    /// </summary>
    public void Update(
        string title,
        string slug,
        decimal originalPrice,
        decimal offerPrice,
        long? travelId,
        string? tagline,
        string? targetAudience,
        string? heroMechanism,
        string? corePromise,
        int depositPercentage,
        int durationDays,
        int durationNights,
        int maxSlotsPerWeek,
        int availableSlots,
        int urgencyHours,
        string? guarantees,
        string? paymentTerms,
        string? salesScript,
        OfferStatus status,
        bool isFeatured)
    {
        Title = title;
        Slug = slug;
        OriginalPrice = originalPrice;
        OfferPrice = offerPrice;
        TravelId = travelId;
        Tagline = tagline;
        TargetAudience = targetAudience;
        HeroMechanism = heroMechanism;
        CorePromise = corePromise;
        DepositPercentage = depositPercentage;
        DepositAmount = Math.Round((offerPrice * depositPercentage) / 100m, 2);
        DurationDays = durationDays;
        DurationNights = durationNights;
        MaxSlotsPerWeek = maxSlotsPerWeek;
        AvailableSlots = availableSlots;
        UrgencyHours = urgencyHours;
        Guarantees = guarantees;
        PaymentTerms = paymentTerms;
        SalesScript = salesScript;
        Status = status;
        IsFeatured = isFeatured;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật mức giá ưu đãi và tính lại số tiền cọc tương ứng.
    /// </summary>
    /// <param name="newPrice">Mức giá ưu đãi mới.</param>
    public void UpdatePrice(decimal newPrice)
    {
        OfferPrice = newPrice;
        DepositAmount = Math.Round((newPrice * DepositPercentage) / 100m, 2);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật số lượng slot khả dụng phục vụ AI Tool Calling.
    /// </summary>
    /// <param name="availableSlots">Số slot còn lại.</param>
    /// <param name="maxSlots">Số slot tối đa mỗi tuần.</param>
    public void UpdateSlots(int availableSlots, int? maxSlots = null)
    {
        AvailableSlots = availableSlots;
        if (maxSlots.HasValue)
        {
            MaxSlotsPerWeek = maxSlots.Value;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu xóa mềm gói Offer.
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        Status = OfferStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Thêm một hạng mục vào Bảng xếp chồng giá trị.
    /// </summary>
    /// <param name="item">Hạng mục Value Stack.</param>
    public void AddItem(OfferItem item)
    {
        _items.Add(item);
    }

    /// <summary>
    /// Xóa toàn bộ hạng mục hiện tại để nạp danh sách mới.
    /// </summary>
    public void ClearItems()
    {
        _items.Clear();
    }
}

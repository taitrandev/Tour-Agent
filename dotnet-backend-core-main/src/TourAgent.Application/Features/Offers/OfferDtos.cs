using TourAgent.Domain.Offers;

namespace TourAgent.Application.Features.Offers;

/// <summary>
/// DTO thông tin giá cả và tiền cọc của gói Offer (khớp cấu trúc pricing của PHP OfferResource).
/// </summary>
public sealed record OfferPricingDto(
    decimal OriginalPrice,
    decimal OfferPrice,
    decimal SavingsAmount,
    double SavingsPercentage,
    int DepositPercentage,
    decimal DepositAmount);

/// <summary>
/// DTO thời lượng ngày đêm của chuyến đi.
/// </summary>
public sealed record OfferDurationDto(
    int Days,
    int Nights);

/// <summary>
/// DTO thông số khan hiếm và khẩn cấp (Scarcity & Urgency).
/// </summary>
public sealed record OfferScarcityDto(
    int MaxSlotsPerWeek,
    int AvailableSlots,
    int UrgencyHours);

/// <summary>
/// DTO tóm tắt sản phẩm Tour du lịch liên kết với Offer.
/// </summary>
public sealed record OfferTravelDto(
    long Id,
    string Title,
    string Code,
    string Country,
    decimal PriceFrom,
    int DurationDays,
    int DurationNights);

/// <summary>
/// DTO chi tiết một hạng mục trong Bảng xếp chồng giá trị (Value Stack Item).
/// </summary>
public sealed record OfferItemDto(
    long Id,
    long OfferId,
    string Name,
    string? Description,
    string Type,
    decimal OriginalValue,
    int SortOrder,
    bool IsHighlighted);

/// <summary>
/// DTO đầy đủ của một Grand Slam Offer trả về cho Client, tương thích 100% với PHP OfferResource.
/// </summary>
public sealed record OfferDto(
    long Id,
    string Title,
    string Slug,
    string? Tagline,
    string? TargetAudience,
    string? HeroMechanism,
    string? CorePromise,
    OfferPricingDto Pricing,
    OfferDurationDto Duration,
    OfferScarcityDto ScarcityUrgency,
    object? Guarantees,
    object? PaymentTerms,
    string? SalesScript,
    string Status,
    bool IsFeatured,
    long? TravelId,
    OfferTravelDto? Travel,
    IReadOnlyList<OfferItemDto> Items,
    string? CreatedAt,
    string? UpdatedAt);

/// <summary>
/// DTO đầu vào của một hạng mục Value Stack khi tạo mới hoặc cập nhật Offer.
/// </summary>
public sealed record ValueStackItemInputDto(
    string Name,
    string? Description = null,
    string? Type = "core",
    decimal OriginalValue = 0,
    int SortOrder = 0,
    bool IsHighlighted = false);

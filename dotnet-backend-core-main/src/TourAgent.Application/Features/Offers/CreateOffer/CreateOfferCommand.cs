using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.CreateOffer;

/// <summary>
/// Command yêu cầu tạo mới một Grand Slam Offer kèm các mục trong Bảng xếp chồng giá trị (Value Stack).
/// </summary>
public sealed record CreateOfferCommand(
    string Title,
    decimal OriginalPrice,
    decimal OfferPrice,
    long? TravelId = null,
    string? Slug = null,
    string? Tagline = null,
    string? TargetAudience = null,
    string? HeroMechanism = null,
    string? CorePromise = null,
    int DepositPercentage = 30,
    int DurationDays = 3,
    int DurationNights = 2,
    int MaxSlotsPerWeek = 4,
    int AvailableSlots = 4,
    int UrgencyHours = 24,
    object? Guarantees = null,
    object? PaymentTerms = null,
    string? SalesScript = null,
    string? Status = "active",
    bool IsFeatured = false,
    IReadOnlyList<ValueStackItemInputDto>? Items = null) : ICommand<Result<OfferDto>>;

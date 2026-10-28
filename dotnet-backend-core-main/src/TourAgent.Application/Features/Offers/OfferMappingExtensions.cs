using System.Text.Json;
using TourAgent.Domain.Offers;

namespace TourAgent.Application.Features.Offers;

/// <summary>
/// Các hàm tiện ích ánh xạ từ Entity Offer sang OfferDto chuẩn REST API.
/// </summary>
public static class OfferMappingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Chuyển đổi thực thể Offer và các Items liên kết thành OfferDto.
    /// </summary>
    /// <param name="offer">Thực thể Offer từ database.</param>
    /// <returns>Đối tượng OfferDto chứa đầy đủ thông số cấu trúc pricing và value stack.</returns>
    public static OfferDto ToDto(this Offer offer)
    {
        var savingsAmount = Math.Max(0, offer.OriginalPrice - offer.OfferPrice);
        var savingsPercentage = offer.OriginalPrice > 0
            ? Math.Round((double)(savingsAmount / offer.OriginalPrice) * 100, 1)
            : 0;

        var pricingDto = new OfferPricingDto(
            OriginalPrice: offer.OriginalPrice,
            OfferPrice: offer.OfferPrice,
            SavingsAmount: savingsAmount,
            SavingsPercentage: savingsPercentage,
            DepositPercentage: offer.DepositPercentage,
            DepositAmount: offer.DepositAmount);

        var durationDto = new OfferDurationDto(
            Days: offer.DurationDays,
            Nights: offer.DurationNights);

        var scarcityDto = new OfferScarcityDto(
            MaxSlotsPerWeek: offer.MaxSlotsPerWeek,
            AvailableSlots: offer.AvailableSlots,
            UrgencyHours: offer.UrgencyHours);

        object? guaranteesObj = null;
        if (!string.IsNullOrWhiteSpace(offer.Guarantees))
        {
            try
            {
                guaranteesObj = JsonSerializer.Deserialize<object>(offer.Guarantees, JsonOptions);
            }
            catch
            {
                guaranteesObj = offer.Guarantees;
            }
        }

        object? paymentTermsObj = null;
        if (!string.IsNullOrWhiteSpace(offer.PaymentTerms))
        {
            try
            {
                paymentTermsObj = JsonSerializer.Deserialize<object>(offer.PaymentTerms, JsonOptions);
            }
            catch
            {
                paymentTermsObj = offer.PaymentTerms;
            }
        }

        OfferTravelDto? travelDto = null;
        if (offer.Travel is not null)
        {
            travelDto = new OfferTravelDto(
                Id: offer.Travel.Id,
                Title: offer.Travel.Name,
                Code: offer.Travel.Code,
                Country: offer.Travel.Country,
                PriceFrom: offer.Travel.PriceFrom,
                DurationDays: offer.Travel.DurationDays,
                DurationNights: offer.Travel.DurationNights);
        }

        var itemsDto = offer.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => new OfferItemDto(
                Id: i.Id,
                OfferId: i.OfferId,
                Name: i.Name,
                Description: i.Description,
                Type: i.Type.ToString().ToLowerInvariant(),
                OriginalValue: i.OriginalValue,
                SortOrder: i.SortOrder,
                IsHighlighted: i.IsHighlighted))
            .ToList();

        return new OfferDto(
            Id: offer.Id,
            Title: offer.Title,
            Slug: offer.Slug,
            Tagline: offer.Tagline,
            TargetAudience: offer.TargetAudience,
            HeroMechanism: offer.HeroMechanism,
            CorePromise: offer.CorePromise,
            Pricing: pricingDto,
            Duration: durationDto,
            ScarcityUrgency: scarcityDto,
            Guarantees: guaranteesObj,
            PaymentTerms: paymentTermsObj,
            SalesScript: offer.SalesScript,
            Status: offer.Status.ToString().ToLowerInvariant(),
            IsFeatured: offer.IsFeatured,
            TravelId: offer.TravelId,
            Travel: travelDto,
            Items: itemsDto,
            CreatedAt: offer.CreatedAt.ToString("o"),
            UpdatedAt: offer.UpdatedAt?.ToString("o"));

    }
}

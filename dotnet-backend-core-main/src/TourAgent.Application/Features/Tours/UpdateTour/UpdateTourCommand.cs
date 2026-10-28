using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.CreateTour;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.UpdateTour;

/// <summary>
/// Command yêu cầu cập nhật thông tin sản phẩm Tour du lịch theo Id.
/// </summary>
public sealed record UpdateTourCommand(
    long Id,
    string Title,
    string? Code,
    string Country,
    string DepartureCity,
    int DurationDays,
    int DurationNights,
    string? Route,
    IReadOnlyList<string>? Destinations,
    IReadOnlyList<string>? Tags,
    decimal? PriceFrom,
    string? Status,
    bool IsFeatured,
    IReadOnlyList<DepartureInputDto>? Departures,
    object? Itinerary,
    object? Inclusions,
    object? Exclusions,
    string? ChildPolicy,
    string? PaymentPolicy,
    string? CancellationPolicy,
    string? VisaPolicy,
    string? Notes) : ICommand<Result<TourAdminItemDto>>;

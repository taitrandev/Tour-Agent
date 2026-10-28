using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.CreateTour;

/// <summary>
/// DTO chứa dữ liệu một đợt khởi hành gửi lên khi tạo hoặc cập nhật Tour.
/// </summary>
public sealed record DepartureInputDto(
    string DepartureDate,
    decimal PriceAdult,
    decimal? PriceChild,
    decimal? SingleSupplement,
    string? Status,
    string? Notes);

/// <summary>
/// Command yêu cầu tạo mới một Tour sản phẩm cùng với lịch khởi hành và lịch trình.
/// </summary>
public sealed record CreateTourCommand(
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

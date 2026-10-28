using System.Text.Json;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.GetAllTours;

/// <summary>
/// Query yêu cầu lấy toàn bộ danh mục sản phẩm tour phục vụ bảng quản trị và catalog.
/// </summary>
/// <param name="Search">Từ khóa tìm kiếm theo tên, mã, điểm đến, quốc gia.</param>
/// <param name="Status">Trạng thái lọc (published, draft, all).</param>
public sealed record GetAllToursQuery(string? Search = null, string? Status = null) : IQuery<Result<IReadOnlyList<TourAdminItemDto>>>;

/// <summary>
/// DTO chi tiết đầy đủ cho từng mục Tour trả về cho UI quản trị và API tương thích 100% PHP.
/// </summary>
public sealed record TourAdminItemDto(
    long Id,
    string Title,
    string? Slug,
    string Code,
    int DurationDays,
    int DurationNights,
    string? Route,
    string Country,
    IReadOnlyList<string> Destinations,
    string DepartureCity,
    IReadOnlyList<string> Tags,
    decimal PriceFrom,
    decimal OriginalPrice,
    string Status,
    bool IsFeatured,
    JsonElement? Itinerary,
    JsonElement? Inclusions,
    JsonElement? Exclusions,
    string? ChildPolicy,
    string? PaymentPolicy,
    string? CancellationPolicy,
    string? VisaPolicy,
    string? Notes,
    IReadOnlyList<TourDepartureDto> Departures,
    int OffersCount,
    int ReviewsCount,
    int FaqsCount);

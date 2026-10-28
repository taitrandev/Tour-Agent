using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.GetAllTours;

/// <summary>
/// Handler xử lý câu truy vấn lấy danh sách toàn bộ tour du lịch cho trang quản trị.
/// </summary>
public sealed class GetAllToursHandler : IQueryHandler<GetAllToursQuery, Result<IReadOnlyList<TourAdminItemDto>>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetAllToursHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetAllToursHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện truy vấn danh sách tour từ bảng travels kèm theo các đợt khởi hành.
    /// </summary>
    /// <param name="query">Tham số tìm kiếm và lọc.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách TourAdminItemDto.</returns>
    public async Task<Result<IReadOnlyList<TourAdminItemDto>>> HandleAsync(
        GetAllToursQuery query,
        CancellationToken cancellationToken = default)
    {
        var toursQuery = _dbContext.Tours
            .Include(t => t.Departures)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "all")
        {
            var isPublished = query.Status == "published" || query.Status == "active";
            toursQuery = toursQuery.Where(t => t.IsActive == isPublished);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            toursQuery = toursQuery.Where(t =>
                t.Name.ToLower().Contains(keyword) ||
                t.Code.ToLower().Contains(keyword) ||
                t.Country.ToLower().Contains(keyword) ||
                t.Destination.ToLower().Contains(keyword));
        }

        var tours = await toursQuery
            .OrderByDescending(t => t.Id)
            .ToListAsync(cancellationToken);

        var result = new List<TourAdminItemDto>(tours.Count);

        foreach (var tour in tours)
        {
            var departures = tour.Departures
                .OrderBy(d => d.DepartureDate)
                .Select(d => new TourDepartureDto(
                    d.Id,
                    d.DepartureDate.ToString("yyyy-MM-dd"),
                    d.PriceAdult,
                    d.PriceChild,
                    d.SingleSupplement,
                    d.Status.ToString().ToLowerInvariant(),
                    null))
                .ToList();

            var destinations = ParseStringList(tour.DestinationsJson);
            var tags = ParseStringList(tour.TagsJson);

            JsonElement? itineraryJson = ParseJsonElement(tour.Itinerary);
            JsonElement? inclusionsJson = ParseJsonElement(tour.Inclusions);
            JsonElement? exclusionsJson = ParseJsonElement(tour.Exclusions);

            result.Add(new TourAdminItemDto(
                tour.Id,
                tour.Name,
                tour.Slug,
                tour.Code,
                tour.DurationDays,
                tour.DurationNights,
                tour.Destination,
                tour.Country,
                destinations,
                tour.DepartureCity,
                tags,
                tour.PriceFrom,
                tour.PriceFrom,
                tour.IsActive ? "published" : "draft",
                tour.IsFeatured,
                itineraryJson,
                inclusionsJson,
                exclusionsJson,
                tour.ChildPolicy,
                tour.PaymentPolicy,
                tour.CancellationPolicy,
                tour.VisaPolicy,
                tour.Highlights,
                departures,
                0,
                0,
                0));
        }

        return Result.Success<IReadOnlyList<TourAdminItemDto>>(result);
    }

    /// <summary>
    /// Hàm trợ giúp phân giải mảng chuỗi từ chuỗi JSON an toàn.
    /// </summary>
    private static IReadOnlyList<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? [];
        }
        catch
        {
            return [json];
        }
    }

    /// <summary>
    /// Hàm trợ giúp phân giải JsonElement từ chuỗi JSON.
    /// </summary>
    private static JsonElement? ParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}

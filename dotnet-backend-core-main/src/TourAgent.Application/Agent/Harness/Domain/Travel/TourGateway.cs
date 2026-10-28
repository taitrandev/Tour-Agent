namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Tours;

/// <summary>
/// Cài đặt ITourGateway truy vấn dữ liệu Tour từ DbContext một cách tối ưu và an toàn.
/// Đảm bảo cách ly tầng nghiệp vụ và tự động lọc các tour đang hoạt động.
/// </summary>
public sealed class TourGateway : ITourGateway
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo TourGateway với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện cơ sở dữ liệu ứng dụng.</param>
    public TourGateway(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<TourDto?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);

        return tour is null ? null : MapToDto(tour);
    }

    /// <inheritdoc />
    public async Task<TourDto?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var tour = await _dbContext.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code.ToUpper() == normalizedCode && t.IsActive, cancellationToken);

        return tour is null ? null : MapToDto(tour);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TourDto>> SearchToursAsync(
        string? keyword = null,
        string? country = null,
        string? departureCity = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tours
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countryNormalized = country.Trim().ToLower();
            query = query.Where(t => t.Country.ToLower().Contains(countryNormalized));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordNormalized = keyword.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(keywordNormalized) ||
                                     t.Destination.ToLower().Contains(keywordNormalized));
        }

        if (!string.IsNullOrWhiteSpace(departureCity))
        {
            var cityNormalized = departureCity.Trim().ToLower();
            query = query.Where(t => t.DepartureCity.ToLower().Contains(cityNormalized));
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(t => t.PriceFrom <= maxPrice.Value);
        }

        var tours = await query
            .OrderBy(t => t.PriceFrom)
            .Take(10)
            .ToListAsync(cancellationToken);

        return tours.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TourDto>> GetAllPublishedToursAsync(CancellationToken cancellationToken = default)
    {
        var tours = await _dbContext.Tours
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.PriceFrom)
            .ToListAsync(cancellationToken);

        return tours.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Chuyển đổi thực thể Domain Tour sang DTO TourDto độc lập.
    /// </summary>
    /// <param name="tour">Thực thể Tour trong cơ sở dữ liệu.</param>
    /// <returns>Đối tượng TourDto.</returns>
    private static TourDto MapToDto(Tour tour)
    {
        return new TourDto(
            Id: tour.Id,
            Code: tour.Code,
            Title: tour.Name,
            Country: tour.Country,
            Destination: tour.Destination,
            DepartureCity: tour.DepartureCity,
            DurationDays: tour.DurationDays,
            DurationNights: tour.DurationNights,
            PriceFrom: tour.PriceFrom,
            Highlights: tour.Highlights
        );
    }
}

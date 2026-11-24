using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Offers;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;
using TourAgent.Domain.Offers;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.GetTourBySlug;

/// <summary>
/// Handler xử lý câu truy vấn lấy thông tin chi tiết tour du lịch theo Slug (hoặc Code nếu không trùng slug).
/// </summary>
public sealed class GetTourBySlugHandler : IQueryHandler<GetTourBySlugQuery, Result<TourDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetTourBySlugHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetTourBySlugHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý lấy thông tin chi tiết tour bao gồm các đợt khởi hành theo Slug.
    /// </summary>
    /// <param name="query">Query chứa Slug cần tìm kiếm.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result chứa TourDetailDto nếu tìm thấy, hoặc lỗi NotFound.</returns>
    public async Task<Result<TourDetailDto>> HandleAsync(
        GetTourBySlugQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Slug))
        {
            return Result.Failure<TourDetailDto>(Error.Validation("Tour.SlugEmpty", "Slug không được để trống."));
        }

        var normalizedSlug = query.Slug.Trim().ToLowerInvariant();

        // Tìm theo Slug trước, nếu không có thì tìm theo Code (in hoa/thường)
        var tour = await _dbContext.Tours
            .Include(t => t.Departures)
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                (t.Slug != null && t.Slug.ToLower() == normalizedSlug) ||
                t.Code.ToLower() == normalizedSlug,
                cancellationToken);

        // Nếu vẫn không thấy và slug là số nguyên thì thử tìm theo Id
        if (tour is null && long.TryParse(normalizedSlug, out var tourId))
        {
            tour = await _dbContext.Tours
                .Include(t => t.Departures)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);
        }

        if (tour is null)
        {
            return Result.Failure<TourDetailDto>(TourErrors.NotFoundBySlug(query.Slug));
        }

        var departureDtos = tour.Departures
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

        // Nạp danh sách các gói Grand Slam Offer đang mở bán (Active) liên kết với Tour này
        var activeOffers = await _dbContext.Offers
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.TravelId == tour.Id && o.Status == OfferStatus.Active)
            .OrderByDescending(o => o.IsFeatured)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);

        var offerDtos = activeOffers.Select(o => o.ToDto()).ToList();

        var detailDto = new TourDetailDto(
            tour.Id,
            tour.Code,
            tour.Name,
            tour.Destination,
            tour.Country,
            tour.DepartureCity,
            tour.DurationDays,
            tour.DurationNights,
            tour.PriceFrom,
            tour.Currency,
            tour.Highlights,
            tour.Inclusions,
            tour.Exclusions,
            tour.VisaPolicy,
            tour.CancellationPolicy,
            tour.ChildPolicy,
            tour.PaymentPolicy,
            tour.Itinerary,
            tour.IsActive,
            departureDtos,
            tour.Slug,
            offerDtos);

        return Result.Success(detailDto);
    }
}

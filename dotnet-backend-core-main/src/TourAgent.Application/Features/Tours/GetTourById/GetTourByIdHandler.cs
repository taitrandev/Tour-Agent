using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Offers;
using TourAgent.Domain.Common;
using TourAgent.Domain.Offers;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.GetTourById;

/// <summary>
/// Handler xử lý câu truy vấn lấy thông tin chi tiết một sản phẩm tour theo Id.
/// </summary>
public sealed class GetTourByIdHandler : IQueryHandler<GetTourByIdQuery, Result<TourDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetTourByIdHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetTourByIdHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý lấy thông tin chi tiết tour bao gồm các đợt khởi hành.
    /// </summary>
    /// <param name="query">Query chứa Id của tour cần lấy.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result chứa TourDetailDto nếu tìm thấy, hoặc lỗi NotFound.</returns>
    public async Task<Result<TourDetailDto>> HandleAsync(
        GetTourByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours
            .Include(t => t.Departures)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);

        if (tour is null)
        {
            return Result.Failure<TourDetailDto>(TourErrors.NotFound(query.Id));
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

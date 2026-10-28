using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Offers;

namespace TourAgent.Application.Features.Offers.GetAllOffers;

/// <summary>
/// Handler xử lý câu truy vấn danh sách Grand Slam Offers kèm theo Value Stack Items và thông tin Tour.
/// </summary>
public sealed class GetAllOffersHandler : IQueryHandler<GetAllOffersQuery, Result<IReadOnlyList<OfferDto>>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetAllOffersHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetAllOffersHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện lọc và lấy danh sách các Offer từ database.
    /// </summary>
    /// <param name="query">Điều kiện tìm kiếm, trạng thái và nổi bật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách OfferDto tương thích 100% với PHP core.</returns>
    public async Task<Result<IReadOnlyList<OfferDto>>> HandleAsync(
        GetAllOffersQuery query,
        CancellationToken cancellationToken = default)
    {
        var offersQuery = _dbContext.Offers
            .Include(o => o.Items)
            .Include(o => o.Travel)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<OfferStatus>(query.Status, true, out var parsedStatus))
            {
                offersQuery = offersQuery.Where(o => o.Status == parsedStatus);
            }
        }

        if (query.IsFeatured.HasValue)
        {
            offersQuery = offersQuery.Where(o => o.IsFeatured == query.IsFeatured.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            offersQuery = offersQuery.Where(o =>
                o.Title.ToLower().Contains(keyword) ||
                (o.Tagline != null && o.Tagline.ToLower().Contains(keyword)) ||
                (o.HeroMechanism != null && o.HeroMechanism.ToLower().Contains(keyword)));
        }

        var offers = await offersQuery
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);

        var dtos = offers.Select(o => o.ToDto()).ToList();
        return Result.Success<IReadOnlyList<OfferDto>>(dtos);
    }
}

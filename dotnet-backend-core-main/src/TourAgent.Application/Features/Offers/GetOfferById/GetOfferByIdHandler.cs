using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.GetOfferById;

/// <summary>
/// Handler xử lý câu truy vấn chi tiết một gói Offer theo Id.
/// </summary>
public sealed class GetOfferByIdHandler : IQueryHandler<GetOfferByIdQuery, Result<OfferDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetOfferByIdHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetOfferByIdHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Tìm kiếm Offer theo Id trong database.
    /// </summary>
    /// <param name="query">Chứa Id gói Offer cần lấy.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>OfferDto nếu tìm thấy, hoặc lỗi NotFound nếu không tồn tại.</returns>
    public async Task<Result<OfferDto>> HandleAsync(
        GetOfferByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var offer = await _dbContext.Offers
            .Include(o => o.Items)
            .Include(o => o.Travel)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (offer is null)
        {
            return Result.Failure<OfferDto>(
                Error.NotFound("Offer.NotFound", $"Không tìm thấy Offer có Id {query.Id}."));
        }

        return Result.Success(offer.ToDto());
    }
}

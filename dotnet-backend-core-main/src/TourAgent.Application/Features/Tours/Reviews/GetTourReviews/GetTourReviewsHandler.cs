using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.Reviews.GetTourReviews;

/// <summary>
/// DTO chứa thông tin một đánh giá của khách hàng cho Tour.
/// </summary>
public sealed record TourReviewDto(
    long Id,
    long TravelId,
    string ReviewerName,
    string? ReviewerAvatarUrl,
    short Rating,
    string Content,
    string? TravelDate,
    bool IsVisible,
    string CreatedAt);

/// <summary>
/// Query yêu cầu lấy danh sách đánh giá của khách hàng cho một Tour cụ thể.
/// </summary>
/// <param name="TourId">Mã định danh của Tour.</param>
public sealed record GetTourReviewsQuery(long TourId) : IQuery<Result<IReadOnlyList<TourReviewDto>>>;

/// <summary>
/// Handler xử lý câu truy vấn lấy danh sách đánh giá của Tour.
/// </summary>
public sealed class GetTourReviewsHandler : IQueryHandler<GetTourReviewsQuery, Result<IReadOnlyList<TourReviewDto>>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public GetTourReviewsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện truy vấn danh sách đánh giá sắp xếp theo Id giảm dần.
    /// </summary>
    /// <param name="request">Câu truy vấn chứa TourId.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách các đánh giá dưới dạng TourReviewDto.</returns>
    public async Task<Result<IReadOnlyList<TourReviewDto>>> HandleAsync(
        GetTourReviewsQuery request,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _dbContext.TourReviews
            .AsNoTracking()
            .Where(r => r.TourId == request.TourId)
            .OrderByDescending(r => r.Id)
            .Select(r => new TourReviewDto(
                r.Id,
                r.TourId,
                r.ReviewerName,
                r.ReviewerAvatarUrl,
                r.Rating,
                r.Content,
                r.TravelDate,
                r.IsVisible,
                r.CreatedAtUtc.ToString("o")))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TourReviewDto>>(reviews);
    }
}

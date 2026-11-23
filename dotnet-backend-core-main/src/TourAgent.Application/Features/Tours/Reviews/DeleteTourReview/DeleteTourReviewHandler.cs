using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Reviews.DeleteTourReview;

/// <summary>
/// Command yêu cầu xóa một đánh giá Tour.
/// </summary>
/// <param name="Id">Mã định danh của đánh giá cần xóa.</param>
public sealed record DeleteTourReviewCommand(long Id) : ICommand<Result>;

/// <summary>
/// Handler xử lý xóa đánh giá của Tour khỏi CSDL.
/// </summary>
public sealed class DeleteTourReviewHandler : ICommandHandler<DeleteTourReviewCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public DeleteTourReviewHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xóa đánh giá theo Id khỏi CSDL.
    /// </summary>
    /// <param name="command">Lệnh chứa Id cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        DeleteTourReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.TourReviews
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (review is null)
        {
            return Result.Failure(TourErrors.ReviewNotFound(command.Id));
        }

        _dbContext.TourReviews.Remove(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Reviews.UpdateTourReviewVisibility;

/// <summary>
/// Command yêu cầu cập nhật trạng thái hiển thị của đánh giá.
/// </summary>
/// <param name="Id">Mã định danh của đánh giá.</param>
/// <param name="IsVisible">Trạng thái hiển thị mới.</param>
public sealed record UpdateTourReviewVisibilityCommand(long Id, bool IsVisible) : ICommand<Result>;

/// <summary>
/// Handler xử lý cập nhật trạng thái ẩn/hiện của đánh giá.
/// </summary>
public sealed class UpdateTourReviewVisibilityHandler : ICommandHandler<UpdateTourReviewVisibilityCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public UpdateTourReviewVisibilityHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Cập nhật trường IsVisible của đánh giá trong CSDL.
    /// </summary>
    /// <param name="command">Lệnh cập nhật trạng thái hiển thị.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        UpdateTourReviewVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.TourReviews
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (review is null)
        {
            return Result.Failure(TourErrors.ReviewNotFound(command.Id));
        }

        review.UpdateVisibility(command.IsVisible);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


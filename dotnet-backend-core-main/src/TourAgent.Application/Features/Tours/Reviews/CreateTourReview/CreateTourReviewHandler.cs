using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Reviews.CreateTourReview;

/// <summary>
/// Command yêu cầu tạo mới một đánh giá khách hàng cho Tour.
/// </summary>
public sealed record CreateTourReviewCommand(
    long TourId,
    string ReviewerName,
    string? TravelDate,
    int Rating,
    string Content,
    bool IsVisible) : ICommand<Result<long>>;

/// <summary>
/// Handler xử lý việc tạo mới đánh giá cho Tour.
/// </summary>
public sealed class CreateTourReviewHandler : ICommandHandler<CreateTourReviewCommand, Result<long>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public CreateTourReviewHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện kiểm tra tính hợp lệ và thêm mới đánh giá vào CSDL.
    /// </summary>
    /// <param name="command">Dữ liệu đánh giá cần tạo.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Id của đánh giá vừa tạo nếu thành công, ngược lại trả về Failure.</returns>
    public async Task<Result<long>> HandleAsync(
        CreateTourReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var reviewResult = TourReview.Create(
            command.TourId,
            command.ReviewerName,
            null,
            (short)command.Rating,
            command.Content,
            command.TravelDate,
            command.IsVisible);

        if (reviewResult.IsFailure)
        {
            return Result.Failure<long>(reviewResult.Error);
        }

        var review = reviewResult.Value;
        _dbContext.TourReviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(review.Id);
    }
}

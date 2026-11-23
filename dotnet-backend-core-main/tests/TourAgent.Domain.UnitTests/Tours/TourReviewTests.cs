using TourAgent.Domain.Tours;
using Xunit;

namespace TourAgent.Domain.UnitTests.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Entity TourReview và các quy tắc bất biến.
/// </summary>
public class TourReviewTests
{
    /// <summary>
    /// Kiểm tra tạo TourReview thành công khi dữ liệu hợp lệ.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        // Act
        var result = TourReview.Create(
            tourId: 10,
            reviewerName: "Nguyễn Văn A",
            reviewerAvatarUrl: "https://example.com/avatar.jpg",
            rating: 5,
            content: "Chuyến đi tuyệt vời, hướng dẫn viên nhiệt tình!",
            travelDate: "Tháng 10/2025",
            isVisible: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.TourId);
        Assert.Equal("Nguyễn Văn A", result.Value.ReviewerName);
        Assert.Equal(5, result.Value.Rating);
        Assert.Equal("Chuyến đi tuyệt vời, hướng dẫn viên nhiệt tình!", result.Value.Content);
        Assert.True(result.Value.IsVisible);
    }

    /// <summary>
    /// Kiểm tra tạo TourReview thất bại nếu TourId không hợp lệ (&lt;= 0).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidTourId_ShouldReturnFailure(long invalidTourId)
    {
        var result = TourReview.Create(
            tourId: invalidTourId,
            reviewerName: "Nguyễn Văn A",
            reviewerAvatarUrl: null,
            rating: 5,
            content: "Nội dung tốt",
            travelDate: null);

        Assert.True(result.IsFailure);
    }

    /// <summary>
    /// Kiểm tra tạo TourReview thất bại nếu ReviewerName để trống.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyReviewerName_ShouldReturnFailure(string emptyName)
    {
        var result = TourReview.Create(
            tourId: 1,
            reviewerName: emptyName,
            reviewerAvatarUrl: null,
            rating: 5,
            content: "Nội dung tốt",
            travelDate: null);

        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.ReviewerNameEmpty.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo TourReview thất bại nếu Rating nằm ngoài khoảng 1-5.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_WithInvalidRating_ShouldReturnFailure(short invalidRating)
    {
        var result = TourReview.Create(
            tourId: 1,
            reviewerName: "Khách hàng",
            reviewerAvatarUrl: null,
            rating: invalidRating,
            content: "Nội dung tốt",
            travelDate: null);

        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.InvalidRating.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo TourReview thất bại nếu Content để trống.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyContent_ShouldReturnFailure(string emptyContent)
    {
        var result = TourReview.Create(
            tourId: 1,
            reviewerName: "Khách hàng",
            reviewerAvatarUrl: null,
            rating: 4,
            content: emptyContent,
            travelDate: null);

        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.ReviewContentEmpty.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra cập nhật trạng thái hiển thị hoạt động chính xác.
    /// </summary>
    [Fact]
    public void UpdateVisibility_ShouldChangeIsVisibleAndSetUpdatedAt()
    {
        var review = TourReview.Create(
            tourId: 1,
            reviewerName: "Khách hàng",
            reviewerAvatarUrl: null,
            rating: 5,
            content: "Nội dung tốt",
            travelDate: null,
            isVisible: true).Value;

        review.UpdateVisibility(false);

        Assert.False(review.IsVisible);
        Assert.NotNull(review.UpdatedAtUtc);
    }
}


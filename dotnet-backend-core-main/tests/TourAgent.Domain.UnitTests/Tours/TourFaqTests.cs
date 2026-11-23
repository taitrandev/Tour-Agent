using TourAgent.Domain.Tours;
using Xunit;

namespace TourAgent.Domain.UnitTests.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Entity TourFaq và các quy tắc bất biến.
/// </summary>
public class TourFaqTests
{
    /// <summary>
    /// Kiểm tra tạo TourFaq thành công khi dữ liệu hợp lệ.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        // Act
        var result = TourFaq.Create(
            tourId: 15,
            question: "Tour có bao gồm vé máy bay khứ hồi không?",
            answer: "Có, giá tour đã bao gồm vé máy bay khứ hồi Vietjet Air.",
            sortOrder: 1,
            isVisible: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(15, result.Value.TourId);
        Assert.Equal("Tour có bao gồm vé máy bay khứ hồi không?", result.Value.Question);
        Assert.Equal("Có, giá tour đã bao gồm vé máy bay khứ hồi Vietjet Air.", result.Value.Answer);
        Assert.Equal(1, result.Value.SortOrder);
        Assert.True(result.Value.IsVisible);
    }

    /// <summary>
    /// Kiểm tra tạo TourFaq thất bại nếu TourId &lt;= 0.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithInvalidTourId_ShouldReturnFailure(long invalidTourId)
    {
        var result = TourFaq.Create(
            tourId: invalidTourId,
            question: "Câu hỏi?",
            answer: "Câu trả lời.");

        Assert.True(result.IsFailure);
    }

    /// <summary>
    /// Kiểm tra tạo TourFaq thất bại nếu Question để trống.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyQuestion_ShouldReturnFailure(string emptyQuestion)
    {
        var result = TourFaq.Create(
            tourId: 1,
            question: emptyQuestion,
            answer: "Câu trả lời.");

        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.FaqQuestionEmpty.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo TourFaq thất bại nếu Answer để trống.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyAnswer_ShouldReturnFailure(string emptyAnswer)
    {
        var result = TourFaq.Create(
            tourId: 1,
            question: "Câu hỏi?",
            answer: emptyAnswer);

        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.FaqAnswerEmpty.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra cập nhật trạng thái ẩn/hiện của FAQ.
    /// </summary>
    [Fact]
    public void UpdateVisibility_ShouldChangeIsVisibleAndSetUpdatedAt()
    {
        var faq = TourFaq.Create(
            tourId: 1,
            question: "Câu hỏi?",
            answer: "Câu trả lời.",
            sortOrder: 0,
            isVisible: true).Value;

        faq.UpdateVisibility(false);

        Assert.False(faq.IsVisible);
        Assert.NotNull(faq.UpdatedAtUtc);
    }
}


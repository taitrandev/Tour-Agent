using TourAgent.Domain.Common;
using Xunit;

namespace TourAgent.Domain.UnitTests.Common;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho cấu trúc Result và Error.
/// </summary>
public class ResultTests
{
    /// <summary>
    /// Kiểm tra Result.Success trả về IsSuccess = true và không có lỗi.
    /// </summary>
    [Fact]
    public void Success_ShouldHaveIsSuccessTrueAndNoError()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    /// <summary>
    /// Kiểm tra Result.Failure trả về IsFailure = true và chứa đúng lỗi.
    /// </summary>
    [Fact]
    public void Failure_ShouldHaveIsFailureTrueAndContainError()
    {
        // Arrange
        var testError = Error.Validation("Test.Code", "Mô tả lỗi kiểm thử");

        // Act
        var result = Result.Failure(testError);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(testError, result.Error);
    }

    /// <summary>
    /// Kiểm tra Result generic có thể truy xuất thuộc tính Value khi thành công.
    /// </summary>
    [Fact]
    public void GenericResult_WhenSuccess_ValueShouldBeAccessible()
    {
        // Act
        var result = Result.Success("Test Value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Value", result.Value);
    }

    /// <summary>
    /// Kiểm tra Result generic ném ngoại lệ InvalidOperationException khi truy cập Value lúc thất bại.
    /// </summary>
    [Fact]
    public void GenericResult_WhenFailure_AccessingValueShouldThrowInvalidOperationException()
    {
        // Arrange
        var error = Error.Validation("Test.Error", "Thao tác thất bại");
        var result = Result.Failure<string>(error);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}

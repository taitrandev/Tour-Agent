using TourAgent.Infrastructure.Services;
using Xunit;

namespace TourAgent.Application.UnitTests.Channels;

/// <summary>
/// Các bài kiểm thử đơn vị cho bộ bóc tách số điện thoại Việt Nam tự động (ContactResolver.DetectPhoneNumber).
/// </summary>
public class ContactResolverTests
{
    /// <summary>
    /// Kiểm tra nhận diện số điện thoại 10 chữ số tiêu chuẩn viết liền.
    /// </summary>
    [Fact]
    public void DetectPhoneNumber_Standard10Digits_ShouldExtractCorrectly()
    {
        // Arrange
        var text = "mình muốn được tư vấn trực tiếp 0909349909";

        // Act
        var result = ContactResolver.DetectPhoneNumber(text);

        // Assert
        Assert.Equal("0909349909", result);
    }

    /// <summary>
    /// Kiểm tra nhận diện số điện thoại có mã quốc gia +84 hoặc 84.
    /// </summary>
    [Fact]
    public void DetectPhoneNumber_WithCountryCode84_ShouldNormalizeToLeadingZero()
    {
        // Arrange
        var text = "Gọi lại cho mình qua số +84988776655 nha";

        // Act
        var result = ContactResolver.DetectPhoneNumber(text);

        // Assert
        Assert.Equal("0988776655", result);
    }

    /// <summary>
    /// Kiểm tra nhận diện số điện thoại có dấu cách hoặc dấu chấm ngăn cách.
    /// </summary>
    [Fact]
    public void DetectPhoneNumber_WithSpacesOrDots_ShouldExtractClean10Digits()
    {
        // Arrange
        var text1 = "Số em là 091 234 5678";
        var text2 = "Zalo em 093.456.7890 nhé";

        // Act
        var result1 = ContactResolver.DetectPhoneNumber(text1);
        var result2 = ContactResolver.DetectPhoneNumber(text2);

        // Assert
        Assert.Equal("0912345678", result1);
        Assert.Equal("0934567890", result2);
    }

    /// <summary>
    /// Kiểm tra văn bản không có số điện thoại hợp lệ thì trả về null.
    /// </summary>
    [Fact]
    public void DetectPhoneNumber_NoPhoneInText_ShouldReturnNull()
    {
        // Arrange
        var text = "mình muốn tìm tour Nhật cho 3 người ngày gần nhất là ngày mấy?";

        // Act
        var result = ContactResolver.DetectPhoneNumber(text);

        // Assert
        Assert.Null(result);
    }
}

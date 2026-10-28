using TourAgent.Domain.Tours;
using Xunit;

namespace TourAgent.Domain.UnitTests.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Entity TourDeparture và logic giữ chỗ (Reservation).
/// </summary>
public class TourDepartureTests
{
    /// <summary>
    /// Kiểm tra tạo TourDeparture thành công khi dữ liệu hợp lệ.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        // Act
        var result = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: 32900000m,
            priceChild: 28000000m,
            singleSupplement: 7000000m,
            totalSlots: 25);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(25, result.Value.AvailableSlots);
        Assert.Equal(TourDepartureStatus.Open, result.Value.Status);
    }

    /// <summary>
    /// Kiểm tra tạo TourDeparture thất bại khi giá người lớn <= 0.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Create_WithInvalidAdultPrice_ShouldReturnFailureResult(decimal invalidPrice)
    {
        // Act
        var result = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: invalidPrice,
            priceChild: 28000000m,
            singleSupplement: 7000000m,
            totalSlots: 25);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.InvalidAdultPrice.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo TourDeparture thất bại khi số chỗ trống lớn hơn tổng số chỗ.
    /// </summary>
    [Fact]
    public void Create_WithAvailableSlotsGreaterThanTotalSlots_ShouldReturnFailureResult()
    {
        // Act
        var result = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: 30000000m,
            priceChild: 25000000m,
            singleSupplement: 5000000m,
            totalSlots: 20,
            availableSlots: 25);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.InvalidSlots.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra giữ chỗ thành công khi còn đủ chỗ trống.
    /// </summary>
    [Fact]
    public void ReserveSlots_WithSufficientSlots_ShouldDecreaseAvailableSlots()
    {
        // Arrange
        var departure = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: 30000000m,
            priceChild: 25000000m,
            singleSupplement: 5000000m,
            totalSlots: 10).Value;

        // Act
        var reserveResult = departure.ReserveSlots(4);

        // Assert
        Assert.True(reserveResult.IsSuccess);
        Assert.Equal(6, departure.AvailableSlots);
        Assert.Equal(TourDepartureStatus.Open, departure.Status);
    }

    /// <summary>
    /// Kiểm tra giữ hết chỗ sẽ tự động chuyển trạng thái đợt bay sang Full.
    /// </summary>
    [Fact]
    public void ReserveSlots_WhenAllSlotsReserved_ShouldSetStatusToFull()
    {
        // Arrange
        var departure = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: 30000000m,
            priceChild: 25000000m,
            singleSupplement: 5000000m,
            totalSlots: 5).Value;

        // Act
        var reserveResult = departure.ReserveSlots(5);

        // Assert
        Assert.True(reserveResult.IsSuccess);
        Assert.Equal(0, departure.AvailableSlots);
        Assert.Equal(TourDepartureStatus.Full, departure.Status);
    }

    /// <summary>
    /// Kiểm tra giữ chỗ vượt quá số lượng còn lại sẽ báo lỗi.
    /// </summary>
    [Fact]
    public void ReserveSlots_WhenExceedingAvailableSlots_ShouldReturnFailureResult()
    {
        // Arrange
        var departure = TourDeparture.Create(
            tourId: 1L,
            departureDate: new DateOnly(2026, 11, 20),
            priceAdult: 30000000m,
            priceChild: 25000000m,
            singleSupplement: 5000000m,
            totalSlots: 5).Value;

        // Act
        var reserveResult = departure.ReserveSlots(6);

        // Assert
        Assert.True(reserveResult.IsFailure);
        Assert.Equal("TourDeparture.NotEnoughSlots", reserveResult.Error.Code);
    }
}

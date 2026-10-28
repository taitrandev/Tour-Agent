using TourAgent.Domain.Tours;
using Xunit;

namespace TourAgent.Domain.UnitTests.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Entity Tour và các quy tắc bất biến (Invariants).
/// </summary>
public class TourTests
{
    /// <summary>
    /// Kiểm tra tạo Tour thành công khi cung cấp đầy đủ thông tin hợp lệ.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        // Act
        var result = Tour.Create(
            code: "JP-TOK-6N5D",
            name: "Hành Trình Mùa Thu Vàng Tokyo - Núi Phú Sĩ",
            destination: "Tokyo - Núi Phú Sĩ",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990000m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("JP-TOK-6N5D", result.Value.Code);
        Assert.Equal(6, result.Value.DurationDays);
        Assert.Equal(35990000m, result.Value.PriceFrom);
        Assert.True(result.Value.IsActive);
    }

    /// <summary>
    /// Kiểm tra tạo Tour thất bại nếu mã tour bị để trống.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_ShouldReturnFailureResult(string emptyCode)
    {
        // Act
        var result = Tour.Create(
            code: emptyCode,
            name: "Tour Nhật Bản",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 20000000m);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.EmptyCode.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo Tour thất bại nếu số ngày tour nhỏ hơn 1.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidDurationDays_ShouldReturnFailureResult(int invalidDays)
    {
        // Act
        var result = Tour.Create(
            code: "JP-TOK",
            name: "Tour Nhật Bản",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: invalidDays,
            durationNights: 0,
            priceFrom: 20000000m);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.InvalidDurationDays.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra tạo Tour thất bại nếu giá sàn nhỏ hơn hoặc bằng 0.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-500000)]
    public void Create_WithInvalidPriceFrom_ShouldReturnFailureResult(decimal invalidPrice)
    {
        // Act
        var result = Tour.Create(
            code: "JP-TOK",
            name: "Tour Nhật Bản",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: invalidPrice);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.InvalidPriceFrom.Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra thêm đợt khởi hành thành công vào Tour.
    /// </summary>
    [Fact]
    public void AddDeparture_WithValidDeparture_ShouldSucceed()
    {
        // Arrange
        var tour = Tour.Create(
            code: "JP-TOK",
            name: "Tour Nhật Bản",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 20000000m).Value;

        var departure = TourDeparture.Create(
            tourId: tour.Id,
            departureDate: new DateOnly(2026, 10, 15),
            priceAdult: 35990000m,
            priceChild: 30000000m,
            singleSupplement: 8000000m,
            totalSlots: 20).Value;

        // Act
        var addResult = tour.AddDeparture(departure);

        // Assert
        Assert.True(addResult.IsSuccess);
        Assert.Single(tour.Departures);
    }

    /// <summary>
    /// Kiểm tra thêm đợt khởi hành trùng ngày trong cùng một Tour sẽ thất bại.
    /// </summary>
    [Fact]
    public void AddDeparture_WithDuplicateDate_ShouldReturnConflictFailure()
    {
        // Arrange
        var tour = Tour.Create(
            code: "JP-TOK",
            name: "Tour Nhật Bản",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 20000000m).Value;

        var departure1 = TourDeparture.Create(
            tourId: tour.Id,
            departureDate: new DateOnly(2026, 10, 15),
            priceAdult: 35990000m,
            priceChild: 30000000m,
            singleSupplement: 8000000m,
            totalSlots: 20).Value;

        var departure2 = TourDeparture.Create(
            tourId: tour.Id,
            departureDate: new DateOnly(2026, 10, 15),
            priceAdult: 36990000m,
            priceChild: 31000000m,
            singleSupplement: 8000000m,
            totalSlots: 15).Value;

        tour.AddDeparture(departure1);

        // Act
        var addResult = tour.AddDeparture(departure2);

        // Assert
        Assert.True(addResult.IsFailure);
        Assert.Equal("Tour.DuplicateDepartureDate", addResult.Error.Code);
    }
}

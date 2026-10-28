using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Features.Tours.SearchTours;
using TourAgent.Domain.Tours;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Features.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Use Case SearchToursHandler (Slice mẫu).
/// Sử dụng InMemory DbContext để giả lập truy vấn cơ sở dữ liệu.
/// </summary>
public class SearchToursHandlerTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Kiểm tra tìm kiếm trả về danh sách tour thành công khi không có bộ lọc (lấy tất cả tour active).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithoutFilters_ShouldReturnAllActiveTours()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var tour1 = Tour.Create(
            code: "JP-TOK-01",
            name: "Tour Tokyo Mùa Thu",
            destination: "Tokyo",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 25000000m,
            id: 1L).Value;

        var tour2 = Tour.Create(
            code: "VN-DN-01",
            name: "Tour Đà Nẵng Phố Cổ",
            destination: "Đà Nẵng",
            country: "Việt Nam",
            departureCity: "Hà Nội",
            durationDays: 3,
            durationNights: 2,
            priceFrom: 5000000m,
            id: 2L).Value;

        dbContext.Tours.AddRange(tour1, tour2);
        await dbContext.SaveChangesAsync();

        var handler = new SearchToursHandler(dbContext);
        var query = new SearchToursQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    /// <summary>
    /// Kiểm tra bộ lọc theo điểm đến (Destination) chỉ trả về các tour khớp điểm đến.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithDestinationFilter_ShouldReturnMatchingToursOnly()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var tourJapan = Tour.Create(
            code: "JP-TOK-01",
            name: "Tour Tokyo",
            destination: "Tokyo - Núi Phú Sĩ",
            country: "Japan",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 25000000m,
            id: 1L).Value;

        var tourKorea = Tour.Create(
            code: "KR-SEO-01",
            name: "Tour Seoul",
            destination: "Seoul - Đảo Nami",
            country: "Korea",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 18000000m,
            id: 2L).Value;

        dbContext.Tours.AddRange(tourJapan, tourKorea);
        await dbContext.SaveChangesAsync();

        var handler = new SearchToursHandler(dbContext);
        var query = new SearchToursQuery(Destination: "Tokyo");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("JP-TOK-01", result.Value.Items[0].Code);
    }

    /// <summary>
    /// Kiểm tra bộ lọc theo giá trần (MaxPrice) chỉ trả về các tour có giá sàn nhỏ hơn hoặc bằng mức giá trần.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithMaxPriceFilter_ShouldExcludeExpensiveTours()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var cheapTour = Tour.Create(
            code: "VN-01",
            name: "Tour Giá Rẻ",
            destination: "Đà Lạt",
            country: "Việt Nam",
            departureCity: "TP.HCM",
            durationDays: 3,
            durationNights: 2,
            priceFrom: 3000000m,
            id: 1L).Value;

        var luxuryTour = Tour.Create(
            code: "EU-01",
            name: "Tour Châu Âu VIP",
            destination: "Paris - Rome",
            country: "France",
            departureCity: "Hà Nội",
            durationDays: 10,
            durationNights: 9,
            priceFrom: 85000000m,
            id: 2L).Value;

        dbContext.Tours.AddRange(cheapTour, luxuryTour);
        await dbContext.SaveChangesAsync();

        var handler = new SearchToursHandler(dbContext);
        var query = new SearchToursQuery(MaxPrice: 10000000m);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("VN-01", result.Value.Items[0].Code);
    }

    /// <summary>
    /// Kiểm tra phân trang không hợp lệ (Page nhỏ hơn 1) sẽ trả về lỗi xác thực.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithInvalidPage_ShouldReturnValidationFailure()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var handler = new SearchToursHandler(dbContext);
        var query = new SearchToursQuery(Page: 0);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("SearchTours.InvalidPage", result.Error.Code);
    }
}

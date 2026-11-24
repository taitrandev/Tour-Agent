using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Features.Tours.GetTourBySlug;
using TourAgent.Domain.Tours;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Features.Tours;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Use Case GetTourBySlugHandler.
/// </summary>
public class GetTourBySlugHandlerTests
{
    /// <summary>
    /// Khởi tạo DbContext InMemory cho môi trường kiểm thử độc lập.
    /// </summary>
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Kiểm tra khi tìm kiếm theo Slug đã tồn tại thì trả về thông tin chi tiết tour thành công.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithExistingSlug_ShouldReturnTourDetail()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var tour = Tour.Create(
            code: "TOUR-JP-8635",
            name: "Nhật - Osaka - Phú Sỹ - Tokyo 2",
            destination: "Osaka, Kyoto, Phú Sỹ, Tokyo",
            country: "Nhật Bản",
            departureCity: "TP.HCM",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990000m,
            id: 10L,
            slug: "nhat-osaka-phu-sy-tokyo-2").Value;

        dbContext.Tours.Add(tour);
        await dbContext.SaveChangesAsync();

        var handler = new GetTourBySlugHandler(dbContext);
        var query = new GetTourBySlugQuery("nhat-osaka-phu-sy-tokyo-2");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TOUR-JP-8635", result.Value.Code);
        Assert.Equal("Nhật - Osaka - Phú Sỹ - Tokyo 2", result.Value.Name);
        Assert.Equal("nhat-osaka-phu-sy-tokyo-2", result.Value.Slug);
    }

    /// <summary>
    /// Kiểm tra khi tìm kiếm theo Mã Code (fallback khi không truyền slug) thì trả về tour thành công.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithCodeFallback_ShouldReturnTourDetail()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var tour = Tour.Create(
            code: "TOUR-JP-0348",
            name: "Tour Nhật Tháng 12",
            destination: "Tokyo, Osaka",
            country: "Nhật Bản",
            departureCity: "TP.HCM",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990001m,
            id: 20L).Value;

        dbContext.Tours.Add(tour);
        await dbContext.SaveChangesAsync();

        var handler = new GetTourBySlugHandler(dbContext);
        var query = new GetTourBySlugQuery("tour-jp-0348");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TOUR-JP-0348", result.Value.Code);
        Assert.Equal("Tour Nhật Tháng 12", result.Value.Name);
    }

    /// <summary>
    /// Kiểm tra khi tìm kiếm Slug không tồn tại trong hệ thống thì trả về NotFound.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithNonExistentSlug_ShouldReturnNotFound()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var handler = new GetTourBySlugHandler(dbContext);
        var query = new GetTourBySlugQuery("slug-khong-ton-tai");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(TourErrors.NotFoundBySlug("slug-khong-ton-tai").Code, result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra khi truyền Slug rỗng thì trả về lỗi Validation.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithEmptySlug_ShouldReturnValidationError()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var handler = new GetTourBySlugHandler(dbContext);
        var query = new GetTourBySlugQuery("");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Tour.SlugEmpty", result.Error.Code);
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Agent.Tools;
using TourAgent.Domain.Offers;
using TourAgent.Domain.Tours;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho 2 Write Tools với chuẩn camelCase:
/// - UpdateTravelTitleTool: Cập nhật tên tour kèm cơ chế làm rõ Disambiguation Pattern.
/// - UpdateOfferPriceTool: Cập nhật tiêu đề và giá ưu đãi gói Grand Slam Offer kèm Disambiguation Pattern.
/// </summary>
public class AgentWriteToolTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<AppDbContext> SeedSampleDataAsync()
    {
        var db = CreateInMemoryDbContext();

        // Seed 3 Tour sản phẩm
        var tour1 = Tour.Create(
            code: "JP-TOK-01",
            name: "Cung Đường Vàng Nhật Bản 6N5Đ",
            destination: "Tokyo - Núi Phú Sĩ - Kyoto",
            country: "Nhật Bản",
            departureCity: "TP.HCM",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990000m,
            id: 1L).Value;

        var tour2 = Tour.Create(
            code: "JP-OSA-02",
            name: "Khám Phá Cung Đường Mùa Thu Nhật Bản",
            destination: "Osaka - Kyoto - Kobe",
            country: "Nhật Bản",
            departureCity: "Hà Nội",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 29990000m,
            id: 2L).Value;

        var tour3 = Tour.Create(
            code: "KR-SEO-01",
            name: "Hành Trình Xứ Kim Chi Seoul 5N4Đ",
            destination: "Seoul - Nami - Everland",
            country: "Hàn Quốc",
            departureCity: "TP.HCM",
            durationDays: 5,
            durationNights: 4,
            priceFrom: 18990000m,
            id: 3L).Value;

        db.Tours.AddRange(tour1, tour2, tour3);

        // Seed 3 Gói Grand Slam Offer
        var offer1 = Offer.Create(
            title: "Combo Phú Quốc Nghỉ Dưỡng 4 Chạm Thảnh Thơi",
            slug: "combo-phu-quoc-nghi-duong-4-cham",
            originalPrice: 12000000m,
            offerPrice: 7990000m,
            travelId: 1L);

        var offer2 = Offer.Create(
            title: "Combo Phú Quốc Đón Hè Rực Rỡ",
            slug: "combo-phu-quoc-don-he-ruc-ro",
            originalPrice: 15000000m,
            offerPrice: 9990000m,
            travelId: 1L);

        var offer3 = Offer.Create(
            title: "Trọn Gói Khám Phá Côn Đảo Tâm Linh",
            slug: "tron-goi-con-dao-tam-linh",
            originalPrice: 9000000m,
            offerPrice: 5990000m,
            travelId: 2L);

        db.Offers.AddRange(offer1, offer2, offer3);

        await db.SaveChangesAsync();
        return db;
    }

    #region UpdateTravelTitleTool Tests

    [Fact]
    public async Task UpdateTravelTitleTool_WithValidTravelId_ShouldUpdateTitleSuccessfully()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateTravelTitleTool(db);
        var args = JsonSerializer.Serialize(new
        {
            travelId = 1,
            newTitle = "Cung Đường Vàng Nhật Bản Mùa Hoa Anh Đào 6N5Đ"
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var updatedTour = root.GetProperty("updatedTour");
        Assert.Equal("Cung Đường Vàng Nhật Bản Mùa Hoa Anh Đào 6N5Đ", updatedTour.GetProperty("newName").GetString());

        var dbTour = await db.Tours.FindAsync(1L);
        Assert.NotNull(dbTour);
        Assert.Equal("Cung Đường Vàng Nhật Bản Mùa Hoa Anh Đào 6N5Đ", dbTour.Name);
    }

    [Fact]
    public async Task UpdateTravelTitleTool_WithKeyword_SingleMatch_ShouldUpdateTitleSuccessfully()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateTravelTitleTool(db);
        var args = JsonSerializer.Serialize(new
        {
            searchKeyword = "Kim Chi",
            newTitle = "Seoul Mùa Thu Lãng Mạn 5N4Đ"
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var dbTour = await db.Tours.FindAsync(3L);
        Assert.NotNull(dbTour);
        Assert.Equal("Seoul Mùa Thu Lãng Mạn 5N4Đ", dbTour.Name);
    }

    [Fact]
    public async Task UpdateTravelTitleTool_WithKeyword_MultipleMatches_ShouldTriggerDisambiguationPattern()
    {
        // Arrange (từ khóa "Nhật Bản" khớp với 2 tour: tour 1 và tour 2)
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateTravelTitleTool(db);
        var args = JsonSerializer.Serialize(new
        {
            searchKeyword = "Nhật Bản",
            newTitle = "Tour Nhật Bản Mới 2026"
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("disambiguationNeeded").GetBoolean());
        var candidates = root.GetProperty("candidates");
        Assert.Equal(2, candidates.GetArrayLength());
    }

    [Fact]
    public async Task UpdateTravelTitleTool_WithKeyword_NoMatch_ShouldReturnError()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateTravelTitleTool(db);
        var args = JsonSerializer.Serialize(new
        {
            searchKeyword = "Châu Âu Tuyết Trắng",
            newTitle = "Tên Mới"
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("Không tìm thấy", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UpdateTravelTitleTool_EmptyNewTitle_ShouldReturnError()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateTravelTitleTool(db);
        var args = JsonSerializer.Serialize(new
        {
            travelId = 1,
            newTitle = ""
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("không được để trống", root.GetProperty("error").GetString());
    }

    #endregion

    #region UpdateOfferPriceTool Tests

    [Fact]
    public async Task UpdateOfferPriceTool_WithValidOfferId_ShouldUpdateTitleAndPrice()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var firstOffer = await db.Offers.FirstAsync();
        var tool = new UpdateOfferPriceTool(db);
        var args = JsonSerializer.Serialize(new
        {
            offerId = firstOffer.Id,
            newTitle = "Combo Phú Quốc Nghỉ Dưỡng VIP",
            newOfferPrice = 6500000m
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var updatedOffer = root.GetProperty("updatedOffer");
        Assert.Equal("Combo Phú Quốc Nghỉ Dưỡng VIP", updatedOffer.GetProperty("newTitle").GetString());
        Assert.Equal(6500000m, updatedOffer.GetProperty("newPrice").GetDecimal());

        var dbOffer = await db.Offers.FindAsync(firstOffer.Id);
        Assert.NotNull(dbOffer);
        Assert.Equal("Combo Phú Quốc Nghỉ Dưỡng VIP", dbOffer.Title);
        Assert.Equal(6500000m, dbOffer.OfferPrice);
        Assert.Equal(1950000m, dbOffer.DepositAmount); // 30% cọc
    }

    [Fact]
    public async Task UpdateOfferPriceTool_WithKeyword_MultipleMatches_ShouldTriggerDisambiguationPattern()
    {
        // Arrange (từ khóa "Phú Quốc" khớp với 2 offer)
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateOfferPriceTool(db);
        var args = JsonSerializer.Serialize(new
        {
            searchKeyword = "Phú Quốc",
            newOfferPrice = 5000000m
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("disambiguationNeeded").GetBoolean());
        var candidates = root.GetProperty("candidates");
        Assert.Equal(2, candidates.GetArrayLength());
    }

    [Fact]
    public async Task UpdateOfferPriceTool_PriceGreaterThanOriginal_ShouldReturnValidationError()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var offer = await db.Offers.FirstAsync(o => o.OriginalPrice == 12000000m);
        var tool = new UpdateOfferPriceTool(db);
        var args = JsonSerializer.Serialize(new
        {
            offerId = offer.Id,
            newOfferPrice = 13000000m // Lớn hơn original 12tr
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("không được lớn hơn hoặc bằng giá gốc", root.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UpdateOfferPriceTool_NoUpdateFields_ShouldReturnError()
    {
        // Arrange
        await using var db = await SeedSampleDataAsync();
        var tool = new UpdateOfferPriceTool(db);
        var args = JsonSerializer.Serialize(new
        {
            offerId = 1
        });

        // Act
        var resultJson = await tool.ExecuteAsync(args);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("Vui lòng cung cấp ít nhất 'newTitle' hoặc 'newOfferPrice'", root.GetProperty("error").GetString());
    }

    #endregion
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Agent.Tools;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Tours;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho 5 Agent Tools tra cứu dữ liệu tour du lịch.
/// </summary>
public class AgentToolTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<AppDbContext> SeedSampleTourDataAsync()
    {
        var db = CreateInMemoryDbContext();

        var sampleItinerary = """
        [
            {"day": 1, "title": "TP.HCM - Tokyo", "content": "Đáp chuyến bay tới Narita lúc 08:00."},
            {"day": 2, "title": "Khám phá Tokyo", "content": "Tham quan Chùa Asakusa Kannon và Tháp Tokyo Skytree."}
        ]
        """;

        var tour = Tour.Create(
            code: "JP-TOK-01",
            name: "Cung Đường Vàng Nhật Bản 6N5Đ",
            destination: "Tokyo - Núi Phú Sĩ - Kyoto",
            country: "Nhật Bản",
            departureCity: "TP.HCM",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990000m,
            highlights: "Trải nghiệm tắm Onsen truyền thống và ngắm núi Phú Sĩ.",
            inclusions: "[\"Vé máy bay khứ hồi\", \"Khách sạn 4 sao\", \"Xe đưa đón\"]",
            exclusions: "[\"Chi phí cá nhân\", \"Tiền tip\"]",
            visaPolicy: "Hộ chiếu còn hạn trên 6 tháng.",
            cancellationPolicy: "Hủy trước 30 ngày hoàn 100%.",
            childPolicy: "Trẻ dưới 2 tuổi tính 10% giá vé.",
            paymentPolicy: "Đặt cọc 50% ngay khi đăng ký.",
            itinerary: sampleItinerary,
            id: 1L).Value;

        var dep1 = TourDeparture.Create(1L, new DateOnly(2026, 10, 15), 35990000m, 28000000m, 8000000m, 20, 10, TourDepartureStatus.Open, id: 101L).Value;
        var dep2 = TourDeparture.Create(1L, new DateOnly(2026, 11, 20), 36990000m, 29000000m, 8000000m, 20, 5, TourDepartureStatus.Open, id: 102L).Value;

        tour.AddDeparture(dep1);
        tour.AddDeparture(dep2);

        db.Tours.Add(tour);
        db.TourDepartures.AddRange(dep1, dep2);

        var conv = Conversation.Create("web", "user_123", id: 10L).Value;
        db.Conversations.Add(conv);

        await db.SaveChangesAsync();
        return db;
    }

    /// <summary>
    /// Kiểm tra SearchTravelsTool trả về danh sách tour và lọc theo quốc gia chính xác.
    /// </summary>
    [Fact]
    public async Task SearchTravelsTool_WithCountryFilter_ShouldReturnMatchingTours()
    {
        // Arrange
        await using var db = await SeedSampleTourDataAsync();
        var tool = new SearchTravelsTool(db);

        // Act
        var resultJson = await tool.ExecuteAsync("{\"country\": \"Nhật Bản\"}");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("total_found").GetInt32());
        var travels = root.GetProperty("travels");
        Assert.Equal("JP-TOK-01", travels[0].GetProperty("code").GetString());
    }

    /// <summary>
    /// Kiểm tra CheckDeparturesTool trả về các đợt khởi hành của tour.
    /// </summary>
    [Fact]
    public async Task CheckDeparturesTool_WithValidTourId_ShouldReturnDepartures()
    {
        // Arrange
        await using var db = await SeedSampleTourDataAsync();
        var tool = new CheckDeparturesTool(db);

        // Act
        var resultJson = await tool.ExecuteAsync("{\"travel_id\": 1}");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(2, root.GetProperty("total_departures").GetInt32());
    }

    /// <summary>
    /// Kiểm tra GetTravelItineraryTool trả về đúng lịch trình của ngày được chọn.
    /// </summary>
    [Fact]
    public async Task GetTravelItineraryTool_WithSpecificDay_ShouldReturnThatDay()
    {
        // Arrange
        await using var db = await SeedSampleTourDataAsync();
        var tool = new GetTravelItineraryTool(db);

        // Act
        var resultJson = await tool.ExecuteAsync("{\"travel_id\": 1, \"day\": 2}");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var day = root.GetProperty("day");
        Assert.Equal(2, day.GetProperty("day").GetInt32());
        Assert.Contains("Asakusa", day.GetProperty("content").GetString());
    }

    /// <summary>
    /// Kiểm tra GetTravelPoliciesTool trả về đúng thông tin chính sách.
    /// </summary>
    [Fact]
    public async Task GetTravelPoliciesTool_WithVisaPolicy_ShouldReturnVisaInfo()
    {
        // Arrange
        await using var db = await SeedSampleTourDataAsync();
        var tool = new GetTravelPoliciesTool(db);

        // Act
        var resultJson = await tool.ExecuteAsync("{\"travel_id\": 1, \"topic\": \"visa_policy\"}");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var policies = root.GetProperty("policies");
        Assert.Contains("Hộ chiếu", policies.GetProperty("visa_policy").GetString());
    }

    /// <summary>
    /// Kiểm tra EscalateToHumanTool cập nhật trạng thái conversation và trả về mã phiếu #ESC.
    /// </summary>
    [Fact]
    public async Task EscalateToHumanTool_ShouldEscalateConversationAndReturnTicket()
    {
        // Arrange
        await using var db = await SeedSampleTourDataAsync();
        var tool = new EscalateToHumanTool(db);

        // Act
        var resultJson = await tool.ExecuteAsync("""
        {
            "reason": "Khách để lại số điện thoại xin đặt cọc",
            "customer_phone": "0987654321",
            "conversation_id": 10,
            "travel_id": 1
        }
        """);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("escalated", root.GetProperty("status").GetString());
        Assert.StartsWith("#ESC-", root.GetProperty("ticket_code").GetString());

        var conv = await db.Conversations.FindAsync(10L);
        Assert.NotNull(conv);
        Assert.Equal(ConversationStatus.HumanActive, conv.Status);
    }
}

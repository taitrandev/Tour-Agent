using TourAgent.Application.Agent.Harness.Domain.Travel;
using TourAgent.Application.Agent.Harness.State;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent.Harness.Domain.Travel;

/// <summary>
/// Các bài kiểm tra đơn vị cho Kỹ thuật D2: Tour Status Provider cung cấp dữ liệu cho Status Bar G2.
/// </summary>
public class TourStatusProviderTests
{
    /// <summary>
    /// Kiểm tra Provider chỉ hỗ trợ domain 'travel'.
    /// </summary>
    [Fact]
    public void Supports_ShouldReturnTrueOnlyForTravelDomain()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var provider = new TourStatusProvider(reducer);

        // Assert
        Assert.True(provider.Supports("travel"));
        Assert.True(provider.Supports("TRAVEL"));
        Assert.False(provider.Supports("hotel"));
        Assert.False(provider.Supports("general"));
    }

    /// <summary>
    /// Kiểm tra Provider trích xuất đúng các thuộc tính của tour đang xem và số lượng ứng viên.
    /// </summary>
    [Fact]
    public void ProvideStatusBarData_ShouldExtractActiveTourAndCandidates()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var provider = new TourStatusProvider(reducer);
        var state = new AgentState();

        var travelState = reducer.GetTravelState(state);
        travelState.ActiveTravel = new ActiveTravelState
        {
            Id = 100,
            Code = "JP-TOK-01",
            Title = "Cung Đường Vàng Nhật Bản 6N5Đ",
            PriceFrom = 35990000m,
            Status = "proposed"
        };
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 100,
            Code = "JP-TOK-01",
            Title = "Cung Đường Vàng Nhật Bản 6N5Đ",
            PriceFrom = 35990000m
        });
        reducer.SaveTravelState(state, travelState);

        // Act
        var data = provider.ProvideStatusBarData(state);

        // Assert
        Assert.Equal(100L, data["active_tour_id"]);
        Assert.Equal("JP-TOK-01", data["active_tour_code"]);
        Assert.Equal("Cung Đường Vàng Nhật Bản 6N5Đ", data["active_tour_title"]);
        Assert.Equal(35990000m, data["active_tour_price"]);
        Assert.Equal("proposed", data["selection_status"]);
        Assert.Equal(1, data["candidates_count"]);
    }
}

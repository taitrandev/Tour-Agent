using TourAgent.Application.Agent.Harness.Domain.Travel;
using TourAgent.Application.Agent.Harness.State;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent.Harness.Domain.Travel;

/// <summary>
/// Các bài kiểm tra đơn vị cho Kỹ thuật D1: Tour State Reducer và máy trạng thái Tour du lịch.
/// </summary>
public class TourStateReducerTests
{
    /// <summary>
    /// Kiểm tra khi gọi tool search_travels trả về danh sách, Reducer tự động trích xuất các candidates.
    /// </summary>
    [Fact]
    public void ReduceAfterToolExecuted_SearchTravels_ShouldPopulateCandidates()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var state = new AgentState();
        var searchResultJson = """
        {
            "total_found": 2,
            "travels": [
                {
                    "id": 1,
                    "code": "JP-TOK-01",
                    "name": "Cung Đường Vàng Nhật Bản 6N5Đ",
                    "country": "Nhật Bản",
                    "price_from": 35990000
                },
                {
                    "id": 2,
                    "code": "JP-OSA-02",
                    "name": "Khám Phá Osaka Mùa Hoa Anh Đào 5N4Đ",
                    "country": "Nhật Bản",
                    "price_from": 28990000
                }
            ]
        }
        """;

        // Act
        reducer.ReduceAfterToolExecuted(state, "search_travels", "{}", searchResultJson);
        var travelState = reducer.GetTravelState(state);

        // Assert
        Assert.Equal(2, travelState.Candidates.Count);
        Assert.Equal("JP-TOK-01", travelState.Candidates[0].Code);
        Assert.Equal("JP-OSA-02", travelState.Candidates[1].Code);
    }

    /// <summary>
    /// Kiểm tra khi tìm kiếm chỉ ra đúng 1 tour, tour đó tự động được gán làm ActiveTravel ở trạng thái candidate.
    /// </summary>
    [Fact]
    public void ReduceAfterToolExecuted_SearchTravels_SingleCandidate_ShouldSetCandidateActiveTravel()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var state = new AgentState();
        var searchResultJson = """
        {
            "total_found": 1,
            "travels": [
                {
                    "id": 10,
                    "code": "THAI-BKK-01",
                    "name": "Bangkok - Pattaya 5N4Đ",
                    "country": "Thái Lan",
                    "price_from": 6990000
                }
            ]
        }
        """;

        // Act
        reducer.ReduceAfterToolExecuted(state, "search_travels", "{}", searchResultJson);
        var travelState = reducer.GetTravelState(state);

        // Assert
        Assert.Single(travelState.Candidates);
        Assert.NotNull(travelState.ActiveTravel);
        Assert.Equal(10, travelState.ActiveTravel!.Id);
        Assert.Equal("candidate", travelState.ActiveTravel.Status);
    }

    /// <summary>
    /// Kiểm tra khi xem lịch trình (get_travel_itinerary), trạng thái của tour chuyển sang 'proposed'.
    /// </summary>
    [Fact]
    public void ReduceAfterToolExecuted_GetTravelItinerary_ShouldTransitionToProposed()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var state = new AgentState();
        var argumentsJson = "{\"travel_id\": 1}";
        var resultJson = "{\"success\": true, \"travel_id\": 1, \"travel_title\": \"Tour Nhật Bản 6N5Đ\"}";

        // Act
        reducer.ReduceAfterToolExecuted(state, "get_travel_itinerary", argumentsJson, resultJson);
        var travelState = reducer.GetTravelState(state);

        // Assert
        Assert.NotNull(travelState.ActiveTravel);
        Assert.Equal(1, travelState.ActiveTravel!.Id);
        Assert.Equal("proposed", travelState.ActiveTravel.Status);
        Assert.Equal("Tour Nhật Bản 6N5Đ", travelState.ActiveTravel.Title);
    }

    /// <summary>
    /// Kiểm tra khi kích hoạt escalate_to_human, tour chuyển sang trạng thái 'confirmed'.
    /// </summary>
    [Fact]
    public void ReduceAfterToolExecuted_EscalateToHuman_ShouldTransitionToConfirmed()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var state = new AgentState();
        var travelState = reducer.GetTravelState(state);
        travelState.ActiveTravel = new ActiveTravelState
        {
            Id = 1,
            Code = "JP-01",
            Title = "Tour Nhật Bản",
            Status = "proposed"
        };
        reducer.SaveTravelState(state, travelState);

        // Act
        reducer.ReduceAfterToolExecuted(state, "escalate_to_human", "{\"reason\": \"Khách đặt cọc\"}", "{}");
        var updated = reducer.GetTravelState(state);

        // Assert
        Assert.NotNull(updated.ActiveTravel);
        Assert.Equal("confirmed", updated.ActiveTravel!.Status);
    }

    /// <summary>
    /// Kiểm tra khi khách hàng thể hiện ý định từ chối tour đang xem, tour bị loại và lưu vào danh sách Rejected.
    /// </summary>
    [Fact]
    public void ReduceFromUserMessage_WhenUserRejectsTour_ShouldResetActiveTravelAndAddRejectedHistory()
    {
        // Arrange
        var reducer = new TourStateReducer();
        var state = new AgentState();
        var travelState = reducer.GetTravelState(state);
        travelState.ActiveTravel = new ActiveTravelState
        {
            Id = 5,
            Code = "EU-01",
            Title = "Tour Châu Âu",
            Status = "proposed"
        };
        reducer.SaveTravelState(state, travelState);

        // Act
        reducer.ReduceFromUserMessage(state, "Tour này đắt quá tìm cái rẻ hơn giúp anh nhé em ơi");
        var updated = reducer.GetTravelState(state);

        // Assert
        Assert.Null(updated.ActiveTravel);
        Assert.Contains(5L, updated.RejectedIds);
    }
}

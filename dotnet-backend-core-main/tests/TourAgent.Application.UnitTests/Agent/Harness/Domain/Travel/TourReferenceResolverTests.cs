namespace TourAgent.Application.UnitTests.Agent.Harness.Domain.Travel;

using System.Text.Json;
using TourAgent.Application.Agent.Harness.Domain.Travel;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Domain.Conversations;
using Xunit;

/// <summary>
/// Các bài kiểm thử đơn vị cho TourReferenceResolver (Kỹ thuật D2).
/// Đảm bảo thuật toán phân giải 5 tầng hoạt động chính xác và chống đoán mò.
/// </summary>
public sealed class TourReferenceResolverTests
{
    private readonly FakeTourGateway _fakeGateway;
    private readonly TourStateReducer _stateReducer;
    private readonly TourReferenceResolver _resolver;

    /// <summary>
    /// Khởi tạo fake gateway, reducer thực tế và đối tượng cần test.
    /// </summary>
    public TourReferenceResolverTests()
    {
        _fakeGateway = new FakeTourGateway();
        _stateReducer = new TourStateReducer();
        _resolver = new TourReferenceResolver(_fakeGateway, _stateReducer);
    }

    /// <summary>
    /// Kiểm tra Tầng 1: Phân giải tường minh qua tham số travel_id trong JSON.
    /// Độ tin cậy kỳ vọng: 1.00.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier1_ExplicitTravelId_ReturnsConfidence1()
    {
        // Arrange
        var tour = new TourDto(101, "TOUR-DL", "Tour Đà Lạt 3N2Đ", "Việt Nam", "Đà Lạt", "TP.HCM", 3, 2, 2500000m, "Thung lũng tình yêu");
        _fakeGateway.AddTour(tour);

        var argumentsJson = JsonSerializer.Serialize(new { travel_id = 101 });

        // Act
        var result = await _resolver.ResolveAsync(null, argumentsJson, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.TravelId);
        Assert.Equal(1.00m, result.Confidence);
        Assert.Equal("explicit_argument", result.Method);
        Assert.Equal("Tour Đà Lạt 3N2Đ", result.TourTitle);
        Assert.Equal("TOUR-DL", result.TourCode);
    }

    /// <summary>
    /// Kiểm tra Tầng 1: Phân giải tường minh qua tham số tour_code trong JSON.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier1_ExplicitTourCode_ReturnsConfidence1()
    {
        // Arrange
        var tour = new TourDto(102, "TOUR-PQ", "Tour Phú Quốc 4N3Đ", "Việt Nam", "Phú Quốc", "TP.HCM", 4, 3, 4500000m, "Bãi sao");
        _fakeGateway.AddTour(tour);

        var argumentsJson = JsonSerializer.Serialize(new { tour_code = "TOUR-PQ" });

        // Act
        var result = await _resolver.ResolveAsync(null, argumentsJson, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(102, result.TravelId);
        Assert.Equal(1.00m, result.Confidence);
        Assert.Equal("explicit_argument", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 2: Phân giải qua máy trạng thái khi tour đã Confirmed.
    /// Độ tin cậy kỳ vọng: 1.00.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier2_StateConfirmed_ReturnsConfidence1()
    {
        // Arrange
        var state = new AgentState();
        var travelState = _stateReducer.GetTravelState(state);
        travelState.ActiveTravel = new ActiveTravelState
        {
            Id = 201,
            Code = "TOUR-DN",
            Title = "Tour Đà Nẵng",
            Status = "confirmed",
            PriceFrom = 3000000m
        };

        // Act
        var result = await _resolver.ResolveAsync(null, null, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.TravelId);
        Assert.Equal(1.00m, result.Confidence);
        Assert.Equal("state_confirmed", result.Method);
        Assert.Equal("Tour Đà Nẵng", result.TourTitle);
    }

    /// <summary>
    /// Kiểm tra Tầng 2: Phân giải qua máy trạng thái khi tour đang Proposed.
    /// Độ tin cậy kỳ vọng: 0.90.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier2_StateProposed_ReturnsConfidence09()
    {
        // Arrange
        var state = new AgentState();
        var travelState = _stateReducer.GetTravelState(state);
        travelState.ActiveTravel = new ActiveTravelState
        {
            Id = 202,
            Code = "TOUR-NT",
            Title = "Tour Nha Trang",
            Status = "proposed",
            PriceFrom = 3500000m
        };

        // Act
        var result = await _resolver.ResolveAsync(null, null, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(202, result.TravelId);
        Assert.Equal(0.90m, result.Confidence);
        Assert.Equal("state_proposed", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 2: Phân giải khi chỉ có đúng 1 tour ứng viên (Single Candidate).
    /// Độ tin cậy kỳ vọng: 0.85.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier2_SingleCandidate_ReturnsConfidence085()
    {
        // Arrange
        var state = new AgentState();
        var travelState = _stateReducer.GetTravelState(state);
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 203,
            Code = "TOUR-HL",
            Title = "Tour Hạ Long",
            PriceFrom = 2000000m,
            Country = "Việt Nam"
        });

        // Act
        var result = await _resolver.ResolveAsync(null, null, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(203, result.TravelId);
        Assert.Equal(0.85m, result.Confidence);
        Assert.Equal("single_candidate", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 3: Phân giải qua Conversation Metadata khi AgentState và Arguments trống.
    /// Độ tin cậy kỳ vọng: 0.85.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier3_ConversationMetadata_ReturnsConfidence085()
    {
        // Arrange
        var tour = new TourDto(301, "TOUR-SP", "Tour Sa Pa", "Việt Nam", "Sa Pa", "Hà Nội", 3, 2, 3200000m, "Fansipan");
        _fakeGateway.AddTour(tour);

        var metaJson = JsonSerializer.Serialize(new { travel_id = 301 });
        var conversation = Conversation.Create("web", "user-1", metadataJson: metaJson).Value;

        // Act
        var result = await _resolver.ResolveAsync(conversation, null, new AgentState());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(301, result.TravelId);
        Assert.Equal(0.85m, result.Confidence);
        Assert.Equal("conversation_metadata", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 4: Phân giải qua khớp văn bản với tour trong danh sách ứng viên (Candidate Text Match).
    /// Độ tin cậy kỳ vọng: 0.80.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier4_CandidateTextMatch_ReturnsConfidence08()
    {
        // Arrange
        var state = new AgentState();
        var travelState = _stateReducer.GetTravelState(state);
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 401,
            Code = "TOUR-QN",
            Title = "Tour Quy Nhơn Kỳ Co",
            PriceFrom = 2800000m,
            Country = "Việt Nam"
        });
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 402,
            Code = "TOUR-VT",
            Title = "Tour Vũng Tàu",
            PriceFrom = 1500000m,
            Country = "Việt Nam"
        });

        var contextText = "Em muốn chốt tour Quy Nhơn Kỳ Co nhé";

        // Act
        var result = await _resolver.ResolveAsync(null, null, state, contextText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(401, result.TravelId);
        Assert.Equal(0.80m, result.Confidence);
        Assert.Equal("candidate_disambiguated", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 4: Xử lý chống đoán mò (Disambiguation) khi văn bản nhắc đến nhiều tour cùng lúc.
    /// Kết quả kỳ vọng: Confidence = 0.00, TravelId = null, Method = ambiguous.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier4_AmbiguousMatch_ReturnsConfidence0AndNoHallucination()
    {
        // Arrange
        var state = new AgentState();
        var travelState = _stateReducer.GetTravelState(state);
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 401,
            Code = "TOUR-QN",
            Title = "Quy Nhơn",
            PriceFrom = 2800000m,
            Country = "Việt Nam"
        });
        travelState.Candidates.Add(new TravelCandidateState
        {
            Id = 402,
            Code = "TOUR-VT",
            Title = "Vũng Tàu",
            PriceFrom = 1500000m,
            Country = "Việt Nam"
        });

        var contextText = "Tôi phân vân giữa Quy Nhơn và Vũng Tàu quá";

        // Act
        var result = await _resolver.ResolveAsync(null, null, state, contextText);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TravelId);
        Assert.Equal(0.00m, result.Confidence);
        Assert.Equal("ambiguous", result.Method);
    }

    /// <summary>
    /// Kiểm tra Tầng 5: Không có thông tin tour nào khớp ở cả 4 tầng trước.
    /// Kết quả kỳ vọng: Confidence = 0.00, TravelId = null, Method = none.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Tier5_None_ReturnsZeroConfidence()
    {
        // Arrange
        var state = new AgentState();

        // Act
        var result = await _resolver.ResolveAsync(null, null, state, "Xin chào bạn, hôm nay thời tiết thế nào?");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TravelId);
        Assert.Equal(0.00m, result.Confidence);
        Assert.Equal("none", result.Method);
    }

    /// <summary>
    /// Giả lập ITourGateway cho mục đích kiểm thử độc lập.
    /// </summary>
    private sealed class FakeTourGateway : ITourGateway
    {
        private readonly List<TourDto> _tours = new();

        /// <summary>
        /// Thêm tour giả lập vào kho dữ liệu in-memory.
        /// </summary>
        public void AddTour(TourDto tour)
        {
            _tours.Add(tour);
        }

        /// <inheritdoc />
        public Task<TourDto?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var match = _tours.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(match);
        }

        /// <inheritdoc />
        public Task<TourDto?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var match = _tours.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TourDto>> SearchToursAsync(
            string? keyword = null,
            string? country = null,
            string? departureCity = null,
            decimal? maxPrice = null,
            CancellationToken cancellationToken = default)
        {
            var query = _tours.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t => t.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                      || t.Destination.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(t => t.PriceFrom <= maxPrice.Value);
            }
            return Task.FromResult<IReadOnlyList<TourDto>>(query.ToList());
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TourDto>> GetAllPublishedToursAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TourDto>>(_tours.ToList());
        }
    }
}

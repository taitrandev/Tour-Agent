namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using System.Text.Json;
using System.Text.RegularExpressions;
using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Cài đặt ITourStateReducer điều phối máy trạng thái lựa chọn tour du lịch (Kỹ thuật D1).
/// Theo dõi quá trình chuyển trạng thái: Mentioned ➔ Candidate ➔ Proposed ➔ Confirmed (hoặc Rejected).
/// </summary>
public sealed class TourStateReducer : ITourStateReducer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex RejectTourRegex = new(
        @"(không thích|đổi tour|tìm tour khác|hủy tour|không muốn đi tour này|xem tour khác|chê|đắt quá tìm cái rẻ hơn)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public TravelDomainState GetTravelState(AgentState state)
    {
        if (state.DomainState.TryGetValue("travel", out var existingObj) && existingObj is not null)
        {
            if (existingObj is TravelDomainState domainState)
            {
                return domainState;
            }

            try
            {
                var rawJson = existingObj is JsonElement jsonElement
                    ? jsonElement.GetRawText()
                    : JsonSerializer.Serialize(existingObj, JsonOptions);

                var parsed = JsonSerializer.Deserialize<TravelDomainState>(rawJson, JsonOptions);
                if (parsed is not null)
                {
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // Sử dụng trạng thái mới nếu JSON bị hỏng
            }
        }

        var initialState = new TravelDomainState();
        state.DomainState["travel"] = initialState;
        return initialState;
    }

    /// <inheritdoc />
    public void SaveTravelState(AgentState state, TravelDomainState travelState)
    {
        state.DomainState["travel"] = travelState;
    }

    /// <inheritdoc />
    public void ReduceAfterToolExecuted(
        AgentState state,
        string toolName,
        string argumentsJson,
        string resultJson)
    {
        var travelState = GetTravelState(state);

        switch (toolName.ToLowerInvariant())
        {
            case "search_travels":
                ReduceSearchTravels(travelState, resultJson);
                break;

            case "get_travel_itinerary":
            case "get_travel_policies":
            case "check_departures":
                ReduceDetailView(travelState, argumentsJson, resultJson);
                break;

            case "escalate_to_human":
                ReduceEscalateToHuman(travelState, argumentsJson);
                break;
        }

        SaveTravelState(state, travelState);
    }

    /// <inheritdoc />
    public void ReduceFromUserMessage(AgentState state, string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        var travelState = GetTravelState(state);

        // Kiểm tra khách có thể hiện ý định từ chối tour đang xem không
        if (travelState.ActiveTravel is not null &&
            travelState.ActiveTravel.Status != "confirmed" &&
            RejectTourRegex.IsMatch(userMessage))
        {
            if (!travelState.RejectedIds.Contains(travelState.ActiveTravel.Id))
            {
                travelState.RejectedIds.Add(travelState.ActiveTravel.Id);
            }

            travelState.ActiveTravel = null;
            SaveTravelState(state, travelState);
        }
    }

    /// <summary>
    /// Xử lý cập nhật danh sách ứng viên (Candidates) sau khi gọi tool search_travels.
    /// </summary>
    /// <param name="travelState">Trạng thái nghiệp vụ Tour hiện tại.</param>
    /// <param name="resultJson">Kết quả JSON trả về từ tool search_travels.</param>
    private static void ReduceSearchTravels(TravelDomainState travelState, string resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("travels", out var travelsProp) && travelsProp.ValueKind == JsonValueKind.Array)
            {
                var newCandidates = new List<TravelCandidateState>();
                foreach (var item in travelsProp.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt64(out var id))
                    {
                        var candidate = new TravelCandidateState
                        {
                            Id = id,
                            Code = item.TryGetProperty("code", out var cProp) ? cProp.GetString() ?? string.Empty : string.Empty,
                            Title = item.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? string.Empty : string.Empty,
                            PriceFrom = item.TryGetProperty("price_from", out var pProp) && pProp.ValueKind == JsonValueKind.Number && pProp.TryGetDecimal(out var price) ? price : 0m,
                            Country = item.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : null
                        };
                        newCandidates.Add(candidate);
                    }
                }

                travelState.Candidates = newCandidates;

                // Nếu chỉ có đúng 1 tour tìm thấy và chưa có active_travel đã chốt, tự động đặt làm ứng viên chủ lực
                if (newCandidates.Count == 1 &&
                    (travelState.ActiveTravel is null || travelState.ActiveTravel.Status != "confirmed"))
                {
                    var single = newCandidates[0];
                    travelState.ActiveTravel = new ActiveTravelState
                    {
                        Id = single.Id,
                        Code = single.Code,
                        Title = single.Title,
                        PriceFrom = single.PriceFrom,
                        Status = "candidate",
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                }
            }
        }
        catch (JsonException)
        {
        }
    }

    /// <summary>
    /// Xử lý khi khách hàng xem chi tiết, lịch trình hoặc chính sách của 1 tour cụ thể (Proposed status).
    /// </summary>
    /// <param name="travelState">Trạng thái nghiệp vụ Tour hiện tại.</param>
    /// <param name="argumentsJson">Tham số đầu vào của tool.</param>
    /// <param name="resultJson">Kết quả trả về của tool.</param>
    private static void ReduceDetailView(TravelDomainState travelState, string argumentsJson, string resultJson)
    {
        long? targetTravelId = null;
        string? targetTitle = null;

        // Trích xuất ID từ arguments
        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var argsDoc = JsonDocument.Parse(argumentsJson);
                if (argsDoc.RootElement.TryGetProperty("travel_id", out var tidProp) && tidProp.ValueKind == JsonValueKind.Number && tidProp.TryGetInt64(out var tid))
                {
                    targetTravelId = tid;
                }
            }
            catch (JsonException)
            {
            }
        }

        // Trích xuất thông tin bổ sung (ID, Title) từ resultJson nếu có
        if (!string.IsNullOrWhiteSpace(resultJson))
        {
            try
            {
                using var resDoc = JsonDocument.Parse(resultJson);
                var root = resDoc.RootElement;
                if (!targetTravelId.HasValue && root.TryGetProperty("travel_id", out var tidProp) && tidProp.ValueKind == JsonValueKind.Number && tidProp.TryGetInt64(out var tid))
                {
                    targetTravelId = tid;
                }

                if (root.TryGetProperty("travel_title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    targetTitle = titleProp.GetString();
                }
                else if (root.TryGetProperty("title", out var tProp) && tProp.ValueKind == JsonValueKind.String)
                {
                    targetTitle = tProp.GetString();
                }
            }
            catch (JsonException)
            {
            }
        }

        if (targetTravelId.HasValue)
        {
            var matchingCandidate = travelState.Candidates.FirstOrDefault(c => c.Id == targetTravelId.Value);

            travelState.ActiveTravel = new ActiveTravelState
            {
                Id = targetTravelId.Value,
                Code = matchingCandidate?.Code ?? travelState.ActiveTravel?.Code ?? string.Empty,
                Title = targetTitle ?? matchingCandidate?.Title ?? travelState.ActiveTravel?.Title ?? $"Tour #{targetTravelId.Value}",
                PriceFrom = matchingCandidate?.PriceFrom ?? travelState.ActiveTravel?.PriceFrom ?? 0m,
                Status = "proposed",
                UpdatedAtUtc = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Xử lý khi kích hoạt chuyển tiếp nhân viên hoặc chốt đặt chỗ (Confirmed status).
    /// </summary>
    /// <param name="travelState">Trạng thái nghiệp vụ Tour hiện tại.</param>
    /// <param name="argumentsJson">Tham số đầu vào của escalate_to_human.</param>
    private static void ReduceEscalateToHuman(TravelDomainState travelState, string argumentsJson)
    {
        long? explicitTravelId = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                if (doc.RootElement.TryGetProperty("travel_id", out var tidProp) && tidProp.ValueKind == JsonValueKind.Number && tidProp.TryGetInt64(out var tid))
                {
                    explicitTravelId = tid;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (explicitTravelId.HasValue)
        {
            var matchingCandidate = travelState.Candidates.FirstOrDefault(c => c.Id == explicitTravelId.Value);

            travelState.ActiveTravel = new ActiveTravelState
            {
                Id = explicitTravelId.Value,
                Code = matchingCandidate?.Code ?? travelState.ActiveTravel?.Code ?? string.Empty,
                Title = matchingCandidate?.Title ?? travelState.ActiveTravel?.Title ?? $"Tour #{explicitTravelId.Value}",
                PriceFrom = matchingCandidate?.PriceFrom ?? travelState.ActiveTravel?.PriceFrom ?? 0m,
                Status = "confirmed",
                UpdatedAtUtc = DateTime.UtcNow
            };
        }
        else if (travelState.ActiveTravel is not null)
        {
            travelState.ActiveTravel.Status = "confirmed";
            travelState.ActiveTravel.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}

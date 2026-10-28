namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using System.Text.Json;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Domain.Conversations;

/// <summary>
/// Cài đặt ITourReferenceResolver thực hiện thuật toán phân giải Tour 5 tầng nghiêm ngặt (Kỹ thuật D2).
/// Loại bỏ triệt để hiện tượng đoán mò (Hallucinated Reference) và lưu vết kiểm toán chi tiết.
/// </summary>
public sealed class TourReferenceResolver : ITourReferenceResolver
{
    private readonly ITourGateway _tourGateway;
    private readonly ITourStateReducer _stateReducer;

    /// <summary>
    /// Khởi tạo TourReferenceResolver với Gateway và Reducer.
    /// </summary>
    /// <param name="tourGateway">Cổng giao tiếp dữ liệu Tour.</param>
    /// <param name="stateReducer">Bộ quản lý máy trạng thái Tour.</param>
    public TourReferenceResolver(ITourGateway tourGateway, ITourStateReducer stateReducer)
    {
        _tourGateway = tourGateway;
        _stateReducer = stateReducer;
    }

    /// <inheritdoc />
    public async Task<TourResolutionResult> ResolveAsync(
        Conversation? conversation,
        string? argumentsJson,
        AgentState? state,
        string? contextText = null,
        CancellationToken cancellationToken = default)
    {
        var audit = new Dictionary<string, object?>();

        // =========================================================================
        // TẦNG 1: EXPLICIT ARGUMENT (Tham số tường minh được truyền trực tiếp)
        // Độ tin cậy tuyệt đối: 1.00
        // =========================================================================
        var explicitResult = await ResolveTier1ExplicitAsync(argumentsJson, audit, cancellationToken);
        if (explicitResult is not null)
        {
            return explicitResult;
        }

        // =========================================================================
        // TẦNG 2: AGENT STATE MACHINE (Trạng thái trong domain_state['travel'])
        // Độ tin cậy cao: 0.85 - 1.00
        // =========================================================================
        if (state is not null)
        {
            var stateResult = ResolveTier2StateMachine(state, audit);
            if (stateResult is not null)
            {
                return stateResult;
            }
        }

        // =========================================================================
        // TẦNG 3: CONVERSATION METADATA (Metadata của phiên hội thoại hoặc client facts)
        // Độ tin cậy khá: 0.85
        // =========================================================================
        var metaResult = await ResolveTier3MetadataAsync(conversation, state, audit, cancellationToken);
        if (metaResult is not null)
        {
            return metaResult;
        }

        // =========================================================================
        // TẦNG 4: TEXT MATCHING & DISAMBIGUATION (Phân tích văn bản ngữ cảnh)
        // Độ tin cậy: 0.80 hoặc 0.00 (nếu mập mờ / ambiguous)
        // =========================================================================
        var textToAnalyze = BuildTextContext(argumentsJson, contextText);
        if (!string.IsNullOrWhiteSpace(textToAnalyze))
        {
            var textResult = await ResolveTier4TextMatchingAsync(textToAnalyze, state, audit, cancellationToken);
            if (textResult is not null)
            {
                return textResult;
            }
        }

        // =========================================================================
        // TẦNG 5: NONE (Không tìm thấy thông tin tour phù hợp)
        // =========================================================================
        audit["resolved_tier"] = 5;
        audit["resolved_at"] = DateTime.UtcNow;
        return new TourResolutionResult(null, 0.00m, "none", null, null, audit);
    }

    /// <summary>
    /// Xử lý phân giải Tầng 1: Trích xuất travel_id hoặc tour_code từ arguments JSON.
    /// </summary>
    private async Task<TourResolutionResult?> ResolveTier1ExplicitAsync(
        string? argumentsJson,
        Dictionary<string, object?> audit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        long? explicitId = null;
        string? explicitCode = null;

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("travel_id", out var tidProp) && tidProp.ValueKind == JsonValueKind.Number && tidProp.TryGetInt64(out var tid) && tid > 0)
            {
                explicitId = tid;
            }
            else if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt64(out var id) && id > 0)
            {
                explicitId = id;
            }

            if (root.TryGetProperty("tour_code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
            {
                explicitCode = codeProp.GetString();
            }
            else if (root.TryGetProperty("code", out var cProp) && cProp.ValueKind == JsonValueKind.String)
            {
                explicitCode = cProp.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        if (explicitId.HasValue)
        {
            var tour = await _tourGateway.FindByIdAsync(explicitId.Value, cancellationToken);
            if (tour is not null)
            {
                audit["resolved_tier"] = 1;
                audit["matched_id"] = tour.Id;
                audit["matched_code"] = tour.Code;

                return new TourResolutionResult(
                    TravelId: tour.Id,
                    Confidence: 1.00m,
                    Method: "explicit_argument",
                    TourTitle: tour.Title,
                    TourCode: tour.Code,
                    AuditMetadata: audit);
            }
        }

        if (!string.IsNullOrWhiteSpace(explicitCode))
        {
            var tour = await _tourGateway.FindByCodeAsync(explicitCode, cancellationToken);
            if (tour is not null)
            {
                audit["resolved_tier"] = 1;
                audit["matched_code"] = tour.Code;

                return new TourResolutionResult(
                    TravelId: tour.Id,
                    Confidence: 1.00m,
                    Method: "explicit_argument",
                    TourTitle: tour.Title,
                    TourCode: tour.Code,
                    AuditMetadata: audit);
            }
        }

        return null;
    }

    /// <summary>
    /// Xử lý phân giải Tầng 2: Trích xuất từ máy trạng thái domain_state['travel'].
    /// </summary>
    private TourResolutionResult? ResolveTier2StateMachine(
        AgentState state,
        Dictionary<string, object?> audit)
    {
        var travelState = _stateReducer.GetTravelState(state);

        // 2a: Active Tour có trạng thái "confirmed"
        if (travelState.ActiveTravel is not null &&
            string.Equals(travelState.ActiveTravel.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            audit["resolved_tier"] = "2a";
            audit["state_status"] = "confirmed";

            return new TourResolutionResult(
                TravelId: travelState.ActiveTravel.Id,
                Confidence: 1.00m,
                Method: "state_confirmed",
                TourTitle: travelState.ActiveTravel.Title,
                TourCode: travelState.ActiveTravel.Code,
                AuditMetadata: audit);
        }

        // 2b: Active Tour có trạng thái "proposed" (đang xem chi tiết)
        if (travelState.ActiveTravel is not null &&
            string.Equals(travelState.ActiveTravel.Status, "proposed", StringComparison.OrdinalIgnoreCase))
        {
            audit["resolved_tier"] = "2b";
            audit["state_status"] = "proposed";

            return new TourResolutionResult(
                TravelId: travelState.ActiveTravel.Id,
                Confidence: 0.90m,
                Method: "state_proposed",
                TourTitle: travelState.ActiveTravel.Title,
                TourCode: travelState.ActiveTravel.Code,
                AuditMetadata: audit);
        }

        // 2c: Chỉ có duy nhất 1 tour trong danh sách candidates
        if (travelState.Candidates.Count == 1)
        {
            var single = travelState.Candidates[0];
            audit["resolved_tier"] = "2c";
            audit["single_candidate_id"] = single.Id;

            return new TourResolutionResult(
                TravelId: single.Id,
                Confidence: 0.85m,
                Method: "single_candidate",
                TourTitle: single.Title,
                TourCode: single.Code,
                AuditMetadata: audit);
        }

        return null;
    }

    /// <summary>
    /// Xử lý phân giải Tầng 3: Trích xuất từ metadata phiên hội thoại hoặc facts đã chốt.
    /// </summary>
    private async Task<TourResolutionResult?> ResolveTier3MetadataAsync(
        Conversation? conversation,
        AgentState? state,
        Dictionary<string, object?> audit,
        CancellationToken cancellationToken)
    {
        long? metadataTravelId = null;

        // Kiểm tra facts trong AgentState
        if (state is not null && state.ConfirmedFacts.TryGetValue("current_travel_id", out var factVal) &&
            long.TryParse(factVal, out var factId) && factId > 0)
        {
            metadataTravelId = factId;
        }

        // Kiểm tra conversation metadata JSON
        if (!metadataTravelId.HasValue && conversation is not null && !string.IsNullOrWhiteSpace(conversation.MetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(conversation.MetadataJson);
                if (doc.RootElement.TryGetProperty("travel_id", out var tidProp) && tidProp.ValueKind == JsonValueKind.Number && tidProp.TryGetInt64(out var tid))
                {
                    metadataTravelId = tid;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (metadataTravelId.HasValue)
        {
            var tour = await _tourGateway.FindByIdAsync(metadataTravelId.Value, cancellationToken);
            if (tour is not null)
            {
                audit["resolved_tier"] = 3;
                audit["metadata_travel_id"] = tour.Id;

                return new TourResolutionResult(
                    TravelId: tour.Id,
                    Confidence: 0.85m,
                    Method: "conversation_metadata",
                    TourTitle: tour.Title,
                    TourCode: tour.Code,
                    AuditMetadata: audit);
            }
        }

        return null;
    }

    /// <summary>
    /// Xử lý phân giải Tầng 4: Phân tích khớp từ khóa tên/mã tour trong văn bản ngữ cảnh.
    /// </summary>
    private async Task<TourResolutionResult?> ResolveTier4TextMatchingAsync(
        string text,
        AgentState? state,
        Dictionary<string, object?> audit,
        CancellationToken cancellationToken)
    {
        var textNormalized = text.ToLowerInvariant();

        // 4a. Khớp trước với danh sách candidates trong state nếu có
        if (state is not null)
        {
            var travelState = _stateReducer.GetTravelState(state);
            if (travelState.Candidates.Count > 0)
            {
                var matchedCandidates = travelState.Candidates
                    .Where(c => (!string.IsNullOrWhiteSpace(c.Code) && textNormalized.Contains(c.Code.ToLowerInvariant())) ||
                                (!string.IsNullOrWhiteSpace(c.Title) && textNormalized.Contains(c.Title.ToLowerInvariant())))
                    .ToList();

                if (matchedCandidates.Count == 1)
                {
                    var singleMatch = matchedCandidates[0];
                    audit["resolved_tier"] = "4a";
                    audit["matched_from"] = "candidates";

                    return new TourResolutionResult(
                        TravelId: singleMatch.Id,
                        Confidence: 0.80m,
                        Method: "candidate_disambiguated",
                        TourTitle: singleMatch.Title,
                        TourCode: singleMatch.Code,
                        AuditMetadata: audit);
                }

                if (matchedCandidates.Count > 1)
                {
                    audit["resolved_tier"] = "4c";
                    audit["candidate_count"] = matchedCandidates.Count;
                    return TourResolutionResult.Ambiguous(
                        matchedCandidates.Select(c => c.Id).ToList(),
                        "Phát hiện nhiều tour ứng viên trùng khớp trong văn bản.");
                }
            }
        }

        // 4b. Khớp với toàn bộ cơ sở dữ liệu published tours
        var allTours = await _tourGateway.GetAllPublishedToursAsync(cancellationToken);
        var dbMatches = allTours
            .Where(t => (!string.IsNullOrWhiteSpace(t.Code) && textNormalized.Contains(t.Code.ToLowerInvariant())) ||
                        (!string.IsNullOrWhiteSpace(t.Title) && textNormalized.Contains(t.Title.ToLowerInvariant())))
            .ToList();

        if (dbMatches.Count == 1)
        {
            var matched = dbMatches[0];
            audit["resolved_tier"] = "4b";
            audit["matched_from"] = "database";

            return new TourResolutionResult(
                TravelId: matched.Id,
                Confidence: 0.80m,
                Method: "database_match",
                TourTitle: matched.Title,
                TourCode: matched.Code,
                AuditMetadata: audit);
        }

        if (dbMatches.Count > 1)
        {
            audit["resolved_tier"] = "4c";
            audit["db_match_count"] = dbMatches.Count;
            return TourResolutionResult.Ambiguous(
                dbMatches.Select(t => t.Id).ToList(),
                "Phát hiện nhiều tour trong cơ sở dữ liệu cùng thỏa mãn từ khóa trong văn bản.");
        }

        return null;
    }

    /// <summary>
    /// Gom toàn bộ các chuỗi văn bản ngữ cảnh để phân tích từ khóa.
    /// </summary>
    private static string BuildTextContext(string? argumentsJson, string? contextText)
    {
        var textParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(contextText))
        {
            textParts.Add(contextText);
        }

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("reason", out var rProp) && rProp.ValueKind == JsonValueKind.String)
                {
                    textParts.Add(rProp.GetString() ?? string.Empty);
                }
                if (root.TryGetProperty("summary", out var sProp) && sProp.ValueKind == JsonValueKind.String)
                {
                    textParts.Add(sProp.GetString() ?? string.Empty);
                }
            }
            catch (JsonException)
            {
            }
        }

        return string.Join(" ", textParts);
    }
}

namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using TourAgent.Application.Agent.Harness.State;
using TourAgent.Application.Agent.Harness.StatusBar;

/// <summary>
/// Cài đặt IAgentStatusProvider cho nghiệp vụ Tour du lịch (Kỹ thuật D2: Tour Status Provider).
/// Trích xuất dữ liệu từ domain_state['travel'] để nhúng trực tiếp vào thẻ XML &lt;domain_context&gt; của Status Bar (G2).
/// </summary>
public sealed class TourStatusProvider : IAgentStatusProvider
{
    private readonly ITourStateReducer _stateReducer;

    /// <summary>
    /// Khởi tạo TourStatusProvider với ITourStateReducer.
    /// </summary>
    /// <param name="stateReducer">Bộ quản lý máy trạng thái Tour.</param>
    public TourStatusProvider(ITourStateReducer stateReducer)
    {
        _stateReducer = stateReducer;
    }

    /// <inheritdoc />
    public bool Supports(string domain)
    {
        return string.Equals(domain, "travel", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> ProvideStatusBarData(AgentState state)
    {
        var result = new Dictionary<string, object>();
        var travelState = _stateReducer.GetTravelState(state);

        if (travelState.ActiveTravel is not null)
        {
            result["active_tour_id"] = travelState.ActiveTravel.Id;
            result["active_tour_code"] = travelState.ActiveTravel.Code;
            result["active_tour_title"] = travelState.ActiveTravel.Title;
            result["active_tour_price"] = travelState.ActiveTravel.PriceFrom;
            result["selection_status"] = travelState.ActiveTravel.Status;
        }

        result["candidates_count"] = travelState.Candidates.Count;

        if (travelState.Candidates.Count > 0)
        {
            var preview = string.Join("; ", travelState.Candidates.Take(3).Select(c => $"{c.Code}: {c.Title}"));
            result["candidates_summary"] = preview;
        }

        return result;
    }
}

namespace TourAgent.Application.Agent.Harness.StatusBar;

using System.Net;
using System.Text;
using System.Text.Json;
using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Giao diện cho bộ dựng thanh trạng thái phản xạ thực tế của Agent (Kỹ thuật G2: Status Bar Framework).
/// </summary>
public interface IStatusBarBuilder
{
    /// <summary>
    /// Xây dựng chuỗi XML biểu diễn trạng thái thực tế của phiên hội thoại để nhúng vào tin nhắn gửi cho LLM.
    /// </summary>
    /// <param name="state">Trạng thái phiên hội thoại từ Agent State Store.</param>
    /// <param name="domain">Tên định danh nghiệp vụ (mặc định 'travel').</param>
    /// <param name="extraContext">Ngữ cảnh bổ sung nếu có (ví dụ tour đang xem từ frontend).</param>
    /// <returns>Chuỗi XML chuẩn <agent_status>...</agent_status>.</returns>
    string Build(AgentState state, string domain = "travel", IReadOnlyDictionary<string, object?>? extraContext = null);
}

/// <summary>
/// Cài đặt IStatusBarBuilder, gom dữ liệu từ AgentState, Confirmed Facts, Domain State
/// và các IAgentStatusProvider đã đăng ký để đóng gói thành khối XML an toàn.
/// </summary>
public sealed class StatusBarBuilder : IStatusBarBuilder
{
    private readonly IEnumerable<IAgentStatusProvider> _providers;

    /// <summary>
    /// Khởi tạo StatusBarBuilder với danh sách các providers từ DI container.
    /// </summary>
    /// <param name="providers">Tập hợp các status provider được đăng ký.</param>
    public StatusBarBuilder(IEnumerable<IAgentStatusProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public string Build(AgentState state, string domain = "travel", IReadOnlyDictionary<string, object?>? extraContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<agent_status version=\"{state.Version}\">");
        sb.AppendLine($"  <workflow_status>{Escape(state.WorkflowStatus)}</workflow_status>");
        sb.AppendLine($"  <current_goal>{Escape(state.CurrentGoal)}</current_goal>");

        // 1. Render Confirmed Facts (Các sự thật bất biến đã xác nhận)
        if (state.ConfirmedFacts.Count > 0)
        {
            sb.AppendLine("  <confirmed_facts>");
            foreach (var (key, value) in state.ConfirmedFacts)
            {
                sb.AppendLine($"    <fact key=\"{Escape(key)}\">{Escape(value)}</fact>");
            }
            sb.AppendLine("  </confirmed_facts>");
        }
        else
        {
            sb.AppendLine("  <confirmed_facts empty=\"true\" />");
        }

        // 2. Render Domain State (chỉ render các domain CHƯA có IAgentStatusProvider chuyên biệt xử lý)
        var unhandledDomainKeys = state.DomainState.Keys
            .Where(k => !_providers.Any(p => p.Supports(k)))
            .ToList();

        if (unhandledDomainKeys.Count > 0)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            sb.AppendLine("  <domain_state>");
            foreach (var key in unhandledDomainKeys)
            {
                var value = state.DomainState[key];
                var formatted = value is string strVal ? strVal : JsonSerializer.Serialize(value, jsonOptions);
                sb.AppendLine($"    <item key=\"{Escape(key)}\">{Escape(formatted)}</item>");
            }
            sb.AppendLine("  </domain_state>");
        }

        // 3. Render dữ liệu từ các Domain Status Providers đã đăng ký
        foreach (var provider in _providers)
        {
            if (provider.Supports(domain))
            {
                var domainData = provider.ProvideStatusBarData(state);
                if (domainData.Count > 0)
                {
                    sb.AppendLine($"  <domain_context domain=\"{Escape(domain)}\">");
                    foreach (var (key, value) in domainData)
                    {
                        var formatted = value is string strVal ? strVal : JsonSerializer.Serialize(value);
                        sb.AppendLine($"    <property key=\"{Escape(key)}\">{Escape(formatted)}</property>");
                    }
                    sb.AppendLine("  </domain_context>");
                }
            }
        }

        // 4. Render Extra Context (nếu có từ conversation metadata hoặc client context)
        if (extraContext is not null && extraContext.Count > 0)
        {
            sb.AppendLine("  <extra_context>");
            foreach (var (key, value) in extraContext)
            {
                if (value is not null)
                {
                    var formatted = value is string strVal ? strVal : JsonSerializer.Serialize(value);
                    sb.AppendLine($"    <context key=\"{Escape(key)}\">{Escape(formatted)}</context>");
                }
            }
            sb.AppendLine("  </extra_context>");
        }

        sb.Append("</agent_status>");

        return sb.ToString();
    }

    /// <summary>
    /// Mã hóa an toàn 5 ký tự đặc biệt theo chuẩn XML (&, <, >, ", ').
    /// </summary>
    /// <param name="rawText">Văn bản gốc.</param>
    /// <returns>Chuỗi đã được escape an toàn.</returns>
    private static string Escape(string? rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        return rawText
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}

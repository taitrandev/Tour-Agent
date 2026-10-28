namespace TourAgent.Application.Agent.Harness.Guardrails;

using System.Diagnostics;
using System.Text.Json;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Domain.Conversations;

/// <summary>
/// Kết quả điều phối thực thi Tool qua 3 lớp bảo vệ của Guardrail Pipeline.
/// </summary>
/// <param name="Success">Cờ báo Tool đã chạy và vượt qua các kiểm soát thành công.</param>
/// <param name="ResultJson">Chuỗi JSON kết quả cuối cùng để đưa vào ngữ cảnh LLM.</param>
/// <param name="LatencyMs">Thời gian thực thi của riêng Tool (ms).</param>
/// <param name="GuardrailPhase">Giai đoạn Guardrail cuối cùng tác động ('constrained', 'executed', 'verified', 'failed').</param>
public sealed record GuardrailExecutionResult(
    bool Success,
    string ResultJson,
    int LatencyMs,
    string GuardrailPhase
);

/// <summary>
/// Giao diện điều phối chu trình bảo vệ công cụ (Kỹ thuật G3: Tool Guardrail Pipeline).
/// </summary>
public interface IToolGuardrailPipeline
{
    /// <summary>
    /// Thực thi công cụ an toàn qua 3 pha: Constrain ➔ Execute ➔ Verify & Correct.
    /// </summary>
    /// <param name="toolName">Tên công cụ cần thực thi.</param>
    /// <param name="argumentsJson">Chuỗi JSON tham số do LLM yêu cầu.</param>
    /// <param name="state">Trạng thái Agent hiện tại.</param>
    /// <param name="conversation">Phiên hội thoại liên quan nếu có.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng GuardrailExecutionResult.</returns>
    Task<GuardrailExecutionResult> ExecuteAsync(
        string toolName,
        string argumentsJson,
        AgentState state,
        Conversation? conversation = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Cài đặt IToolGuardrailPipeline bọc an toàn 3 lớp quanh toàn bộ các lượt gọi Tool của AI Agent.
/// </summary>
public sealed class ToolGuardrailPipeline : IToolGuardrailPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, IAgentTool> _tools;
    private readonly IToolValidator _validator;
    private readonly IPostConditionVerifier _postVerifier;

    /// <summary>
    /// Khởi tạo pipeline với danh sách công cụ và các bộ kiểm tra.
    /// </summary>
    /// <param name="tools">Tập hợp các công cụ trong hệ thống.</param>
    /// <param name="validator">Bộ tiền kiểm tham số (Pha 1).</param>
    /// <param name="postVerifier">Bộ hậu kiểm kết quả (Pha 3).</param>
    public ToolGuardrailPipeline(
        IEnumerable<IAgentTool> tools,
        IToolValidator validator,
        IPostConditionVerifier postVerifier)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _validator = validator;
        _postVerifier = postVerifier;
    }

    /// <inheritdoc />
    public async Task<GuardrailExecutionResult> ExecuteAsync(
        string toolName,
        string argumentsJson,
        AgentState state,
        Conversation? conversation = null,
        CancellationToken cancellationToken = default)
    {
        // 0. Kiểm tra sự tồn tại của công cụ
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var notFoundJson = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Công cụ '{toolName}' không tồn tại trong hệ thống.",
                recovery_hint = "Hãy kiểm tra lại danh sách các công cụ có sẵn và chỉ gọi các công cụ được định nghĩa trong schema.",
                _guardrail_status = "not_found"
            }, JsonOptions);

            return new GuardrailExecutionResult(false, notFoundJson, 0, "tool_lookup");
        }

        // Tự động làm giàu tham số với conversation_id và chat_id nếu có
        var enrichedArguments = EnrichArgumentsWithContext(argumentsJson, conversation);

        // 1. PHA 1: CONSTRAIN (Tiền kiểm tham số)
        var validation = _validator.Validate(tool, enrichedArguments, state);
        if (!validation.IsValid)
        {
            var errorPayload = JsonSerializer.Serialize(new
            {
                success = false,
                error = "Lỗi tiền kiểm tham số (Pre-condition Guardrail Failed).",
                validation_errors = validation.Errors,
                recovery_hint = validation.RecoveryHint,
                _guardrail_status = "pre_condition_failed"
            }, JsonOptions);

            return new GuardrailExecutionResult(false, errorPayload, 0, "constrain");
        }

        // 2. PHA 2: EXECUTE (Thực thi có bắt ngoại lệ an toàn)
        var stopwatch = Stopwatch.StartNew();
        string rawResult;
        bool executionSuccess = true;

        try
        {
            rawResult = await tool.ExecuteAsync(enrichedArguments, cancellationToken);
        }
        catch (Exception ex)
        {
            executionSuccess = false;
            rawResult = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Sự cố thực thi công cụ '{toolName}': {ex.Message}",
                recovery_hint = "Hệ thống ghi nhận sự cố tạm thời khi xử lý. Hãy giải thích nhẹ nhàng với khách và chuyển hướng hỗ trợ khác.",
                _guardrail_status = "execution_exception"
            }, JsonOptions);
        }
        finally
        {
            stopwatch.Stop();
        }

        var latencyMs = (int)stopwatch.ElapsedMilliseconds;

        // 3. PHA 3: VERIFY & CORRECT (Hậu kiểm kết quả và đóng gói phản hồi)
        var verification = _postVerifier.Verify(tool, rawResult, state);

        return new GuardrailExecutionResult(
            Success: executionSuccess && verification.IsVerified,
            ResultJson: verification.ProcessedResultJson,
            LatencyMs: latencyMs,
            GuardrailPhase: "verify_and_correct");
    }

    /// <summary>
    /// Bổ sung conversation_id và chat_id từ phiên hội thoại vào chuỗi JSON tham số nếu chưa có.
    /// </summary>
    /// <param name="argumentsJson">Chuỗi JSON tham số ban đầu.</param>
    /// <param name="conversation">Phiên hội thoại hiện tại.</param>
    /// <returns>Chuỗi JSON tham số đã được làm giàu thông tin ngữ cảnh.</returns>
    private static string EnrichArgumentsWithContext(string argumentsJson, Conversation? conversation)
    {
        if (conversation is null)
        {
            return argumentsJson;
        }

        try
        {
            var dict = string.IsNullOrWhiteSpace(argumentsJson)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson) ?? new Dictionary<string, object?>();

            if (!dict.ContainsKey("conversation_id"))
            {
                dict["conversation_id"] = conversation.Id;
            }

            if (!dict.ContainsKey("chat_id") && !string.IsNullOrWhiteSpace(conversation.ChannelUserId))
            {
                dict["chat_id"] = conversation.ChannelUserId;
            }

            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return argumentsJson;
        }
    }
}

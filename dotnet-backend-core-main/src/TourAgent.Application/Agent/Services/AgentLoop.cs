using TourAgent.Application.Agent.Harness.Domain.Travel;

namespace TourAgent.Application.Agent.Services;

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.Guardrails;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Application.Agent.Harness.StatusBar;
using TourAgent.Application.Agent.Prompts;
using TourAgent.Domain.Conversations;

/// <summary>
/// Trình điều phối chu trình suy luận đa lượt tự động (Multi-Turn Agent Loop).
/// Được tăng cường bởi bộ khung Agent Harness Architecture:
/// - Kỹ thuật G1: Agent State Store & Versioned State Container (chống đãng trí và OCC lock)
/// - Kỹ thuật G2: General Agent Status Bar Framework (State Reflection Injection vào LLM)
/// - Kỹ thuật G3: Tool Guardrail Pipeline (3 pha Constrain -> Execute -> Verify & Correct)
/// - Kỹ thuật D1: Tour Gateway & State Reducer (Quản lý máy trạng thái Tour du lịch)
/// - Kỹ thuật D2: Tour Status Provider & Tour Reference Resolver (Phân giải Tour 5 tầng)
/// </summary>
public sealed class AgentLoop : IAgentLoop
{
    private const int MaxTurns = 5;
    private const decimal CostPerInputTokenUsd = 0.00000014m;  // ~$0.14 per 1M input tokens (DeepSeek V3)
    private const decimal CostPerOutputTokenUsd = 0.00000028m; // ~$0.28 per 1M output tokens (DeepSeek V3)

    private static readonly Regex PhoneRegex = new(@"(0[1-9][0-9]{8,9})", RegexOptions.Compiled);
    private static readonly Regex GroupSizeRegex = new(@"(?:đi|cho|nhóm|đoàn|nhà mình|gia đình|có)?\s*(\d{1,2})\s*(?:người|khách|bạn|thành viên|chỗ|vé|pax)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AdultsRegex = new(@"(?:^|\D)(\d{1,2})\s*(?:người lớn|lớn)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ChildrenRegex = new(@"(?:^|\D)(\d{1,2})\s*(?:bé|trẻ em|trẻ nhỏ|nhỏ|em bé)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DateRegex = new(@"(?:ngày\s*)?(\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MonthRegex = new(@"tháng\s*(\d{1,2}(?:[/-]\d{2,4})?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EscTicketRegex = new(@"#ESC-(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILlmClient _llmClient;
    private readonly IAppDbContext _dbContext;
    private readonly Dictionary<string, IAgentTool> _tools;
    private readonly IAgentStateStore _stateStore;
    private readonly IStatusBarBuilder _statusBarBuilder;
    private readonly IToolGuardrailPipeline _guardrailPipeline;
    private readonly ITourStateReducer _tourStateReducer;

    /// <summary>
    /// Khởi tạo AgentLoop với đầy đủ các dịch vụ phụ thuộc và các lớp Harness bảo vệ.
    /// </summary>
    /// <param name="llmClient">Client kết nối mô hình ngôn ngữ lớn (DeepSeek).</param>
    /// <param name="dbContext">Giao diện tương tác cơ sở dữ liệu.</param>
    /// <param name="tools">Tập hợp các công cụ hỗ trợ Agent.</param>
    /// <param name="stateStore">Kho lưu trữ trạng thái có cấu trúc của Agent (G1).</param>
    /// <param name="statusBarBuilder">Bộ dựng thanh trạng thái phản xạ thực tế (G2).</param>
    /// <param name="guardrailPipeline">Đường ống kiểm soát an toàn công cụ 3 lớp (G3).</param>
    /// <param name="tourStateReducer">Bộ quản lý máy trạng thái Tour du lịch (D1).</param>
    public AgentLoop(
        ILlmClient llmClient,
        IAppDbContext dbContext,
        IEnumerable<IAgentTool> tools,
        IAgentStateStore stateStore,
        IStatusBarBuilder statusBarBuilder,
        IToolGuardrailPipeline guardrailPipeline,
        ITourStateReducer? tourStateReducer = null)
    {
        _llmClient = llmClient;
        _dbContext = dbContext;
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _stateStore = stateStore;
        _statusBarBuilder = statusBarBuilder;
        _guardrailPipeline = guardrailPipeline;
        _tourStateReducer = tourStateReducer ?? new TourStateReducer();
    }

    /// <inheritdoc />
    public async Task<AgentChatResult> RunAsync(
        AgentChatInput input,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == input.ConversationId, cancellationToken);

        if (conversation is null)
        {
            var createResult = Conversation.Create("web", $"guest_{Guid.NewGuid():N}", id: input.ConversationId);
            conversation = createResult.Value;
            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Lưu tin nhắn của khách hàng vào CSDL
        var userMsgResult = Message.CreateUserMessage(conversation.Id, input.UserMessage);
        if (userMsgResult.IsSuccess)
        {
            conversation.AddMessage(userMsgResult.Value);
            _dbContext.Messages.Add(userMsgResult.Value);
        }

        // Kỹ thuật G1: Tải trạng thái có cấu trúc của Agent (Agent State Store)
        var agentState = await _stateStore.GetAsync(conversation.Id, cancellationToken);
        var initialVersion = agentState.Version;

        // Tự động nhận diện và cập nhật Facts từ tin nhắn khách (SĐT, số lượng người/đoàn, ngày đi, ngân sách)
        ExtractFactsFromUserMessage(agentState, input.UserMessage);

        if (input.CurrentTravelId.HasValue)
        {
            agentState.SetFact("current_travel_id", input.CurrentTravelId.Value.ToString());
        }

        // Kỹ thuật D1: Cập nhật ý định tour từ tin nhắn của khách hàng
        _tourStateReducer.ReduceFromUserMessage(agentState, input.UserMessage);

        // Xây dựng System Prompt với Guardrails chống ảo giác
        var systemPrompt = TravelAgentPrompts.GetCustomerConsultantPrompt(
            input.CurrentTravelId,
            input.CurrentTravelTitle,
            input.Country);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystem(systemPrompt)
        };

        // Nạp tối đa 12 tin nhắn đối thoại gần nhất (User và Assistant) để duy trì ngữ cảnh
        var pastMessages = conversation.Messages
            .Where(m => (m.Role == MessageRole.User || m.Role == MessageRole.Assistant) && !string.IsNullOrWhiteSpace(m.Content))
            .OrderBy(m => m.CreatedAtUtc)
            .TakeLast(12)
            .ToList();

        foreach (var msg in pastMessages)
        {
            if (msg.Role == MessageRole.User)
            {
                messages.Add(ChatMessage.CreateUser(msg.Content));
            }
            else if (msg.Role == MessageRole.Assistant)
            {
                messages.Add(ChatMessage.CreateAssistant(msg.Content));
            }
        }

        // Đảm bảo tin nhắn hiện tại có mặt trong danh sách nếu chưa có
        if (!messages.Any(m => m.Role == ChatMessageRoles.User && m.Content == input.UserMessage))
        {
            messages.Add(ChatMessage.CreateUser(input.UserMessage));
        }

        // Đóng gói danh sách công cụ
        var toolDefinitions = _tools.Values
            .Select(t => new ToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();

        var executedTools = new List<string>();
        int totalTokens = 0;
        decimal totalCostUsd = 0m;
        int totalLatencyMs = 0;
        int turnsCount = 0;
        string finalResponse = string.Empty;

        // Ngữ cảnh bổ sung cho Status Bar
        var extraContext = new Dictionary<string, object?>
        {
            ["travel_id"] = input.CurrentTravelId,
            ["travel_title"] = input.CurrentTravelTitle,
            ["country"] = input.Country
        };

        // Vòng lặp suy luận đa lượt tự động (Autonomous Loop)
        while (turnsCount < MaxTurns)
        {
            turnsCount++;
            var stopwatch = Stopwatch.StartNew();

            // Kỹ thuật G2: Dựng thanh trạng thái XML (State Reflection Injection) và nhúng vào tin nhắn cuối cùng
            var statusBarXml = _statusBarBuilder.Build(agentState, "travel", extraContext);

            var turnMessages = new List<ChatMessage>(messages);
            if (turnMessages.Count > 0 && turnMessages[^1].Role == ChatMessageRoles.User)
            {
                var lastUserContent = turnMessages[^1].Content;
                turnMessages[^1] = ChatMessage.CreateUser($"{lastUserContent}\n\n{statusBarXml}");
            }

            var request = new ChatCompletionRequest(
                Messages: turnMessages,
                Tools: toolDefinitions,
                Temperature: 0.2f);

            var completion = await _llmClient.CompleteChatAsync(request, cancellationToken);
            stopwatch.Stop();

            var turnLatency = (int)stopwatch.ElapsedMilliseconds;
            totalLatencyMs += turnLatency;
            totalTokens += completion.Usage.TotalTokens;

            var turnCost = (completion.Usage.PromptTokens * CostPerInputTokenUsd) +
                           (completion.Usage.CompletionTokens * CostPerOutputTokenUsd);
            totalCostUsd += turnCost;

            // Ghi nhận kiểm toán LlmCall (kèm snapshot thanh trạng thái G2 và agent state G1)
            var llmCall = LlmCall.Create(
                conversationId: conversation.Id,
                model: completion.Model,
                promptTokens: completion.Usage.PromptTokens,
                completionTokens: completion.Usage.CompletionTokens,
                latencyMs: turnLatency,
                costUsd: turnCost,
                toolsCalledJson: completion.ToolCalls.Count > 0
                    ? JsonSerializer.Serialize(completion.ToolCalls.Select(t => t.FunctionName))
                    : null,
                provider: "deepseek",
                finishReason: completion.FinishReason ?? "stop",
                toolPayloadsJson: completion.ToolCalls.Count > 0
                    ? JsonSerializer.Serialize(completion.ToolCalls, JsonOptions)
                    : null,
                statusBarSnapshot: statusBarXml,
                stateSnapshotJson: JsonSerializer.Serialize(agentState, JsonOptions));

            if (llmCall.IsSuccess)
            {
                _dbContext.LlmCalls.Add(llmCall.Value);
            }

            // Trường hợp 1: AI yêu cầu thực thi công cụ
            if (completion.ToolCalls.Count > 0)
            {
                messages.Add(ChatMessage.CreateAssistant(completion.Content, completion.ToolCalls));

                foreach (var toolCall in completion.ToolCalls)
                {
                    executedTools.Add(toolCall.FunctionName);

                    // Kỹ thuật G3: Thực thi công cụ an toàn qua 3 pha Tool Guardrail Pipeline
                    var guardrailResult = await _guardrailPipeline.ExecuteAsync(
                        toolCall.FunctionName,
                        toolCall.FunctionArguments,
                        agentState,
                        conversation,
                        cancellationToken);

                    var toolResult = guardrailResult.ResultJson;

                    // Lưu tin nhắn Tool vào CSDL
                    var toolMessage = Message.CreateToolMessage(
                        conversation.Id,
                        toolCall.FunctionName,
                        toolResult);

                    if (toolMessage.IsSuccess)
                    {
                        conversation.AddMessage(toolMessage.Value);
                        _dbContext.Messages.Add(toolMessage.Value);
                    }

                    // Kỹ thuật D1: Cập nhật máy trạng thái Tour du lịch sau khi gọi công cụ thành công
                    _tourStateReducer.ReduceAfterToolExecuted(
                        agentState,
                        toolCall.FunctionName,
                        toolCall.FunctionArguments,
                        toolResult);

                    // Đồng bộ các dữ kiện xác thực từ tham số và kết quả của công cụ vào ConfirmedFacts (Kỹ thuật G1)
                    SyncFactsAfterToolExecuted(agentState, toolCall.FunctionName, toolCall.FunctionArguments, toolResult);

                    messages.Add(ChatMessage.CreateTool(toolCall.Id, toolCall.FunctionName, toolResult));
                }

                // Tiếp tục vòng lặp để AI phân tích kết quả trả về của công cụ
                continue;
            }

            // Trường hợp 2: AI đã có câu trả lời cuối cùng
            finalResponse = completion.Content ?? string.Empty;
            break;
        }

        if (string.IsNullOrWhiteSpace(finalResponse))
        {
            finalResponse = "Dạ em đã ghi nhận thông tin yêu cầu của anh/chị. Em đang kết nối chuyên viên tư vấn để hỗ trợ chu đáo nhất ngay nhé ạ!";
        }

        // Phòng thủ chống ảo giác tạo ticket giả (Phantom Escalation):
        // Nếu trong câu trả lời AI nhắc đến mã #ESC-XX nhưng chưa từng gọi escalate_to_human trong phiên này
        var escMatch = EscTicketRegex.Match(finalResponse);
        if (escMatch.Success && !executedTools.Contains("escalate_to_human", StringComparer.OrdinalIgnoreCase))
        {
            var customerPhone = agentState.ConfirmedFacts.TryGetValue("customer_phone", out var cp) ? cp : null;
            long? targetTravelId = null;
            if (agentState.ConfirmedFacts.TryGetValue("current_travel_id", out var tidStr) && long.TryParse(tidStr, out var tid))
            {
                targetTravelId = tid;
            }

            int? groupSize = null;
            if (agentState.ConfirmedFacts.TryGetValue("group_size", out var gsStr))
            {
                var digits = Regex.Match(gsStr, @"\d+");
                if (digits.Success && int.TryParse(digits.Value, out var parsedGs)) groupSize = parsedGs;
            }

            var departureDate = agentState.ConfirmedFacts.TryGetValue("departure_date", out var dd) ? dd :
                                agentState.ConfirmedFacts.TryGetValue("target_departure_date", out var tdd) ? tdd : null;

            var autoArgs = new Dictionary<string, object?>
            {
                ["reason"] = "Khách hàng yêu cầu hỗ trợ qua AI (Tự động kích hoạt từ cam kết của Agent)",
                ["customer_phone"] = customerPhone,
                ["travel_id"] = targetTravelId,
                ["group_size"] = groupSize,
                ["departure_date"] = departureDate,
                ["conversation_id"] = conversation.Id,
                ["summary"] = $"Yêu cầu hỗ trợ được ghi nhận từ cuộc trò chuyện: {input.UserMessage}"
            };

            var autoArgsJson = JsonSerializer.Serialize(autoArgs, JsonOptions);
            var guardrailResult = await _guardrailPipeline.ExecuteAsync(
                "escalate_to_human",
                autoArgsJson,
                agentState,
                conversation,
                cancellationToken);

            executedTools.Add("escalate_to_human");

            // Trích xuất mã ticket thật và thay thế mã ảo trong câu trả lời của AI
            try
            {
                using var resDoc = JsonDocument.Parse(guardrailResult.ResultJson);
                string? realCode = null;
                foreach (var prop in resDoc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "ticket_code", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(prop.Name, "ticketCode", StringComparison.OrdinalIgnoreCase))
                    {
                        realCode = prop.Value.GetString();
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(realCode))
                {
                    finalResponse = Regex.Replace(finalResponse, @"#ESC-\d+", realCode, RegexOptions.IgnoreCase);
                }
            }
            catch (Exception)
            {
                // Phòng hộ nếu parse kết quả tool gặp sự cố bất ngờ
            }
        }

        // Lưu câu trả lời cuối cùng của AI vào CSDL
        var assistantMsgResult = Message.CreateAssistantMessage(conversation.Id, finalResponse);
        if (assistantMsgResult.IsSuccess)
        {
            conversation.AddMessage(assistantMsgResult.Value);
            _dbContext.Messages.Add(assistantMsgResult.Value);
        }

        // Kỹ thuật G1: Lưu lại trạng thái AgentState đã được cập nhật vào CSDL
        await _stateStore.SaveAsync(conversation.Id, agentState, initialVersion, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        bool isEscalated = executedTools.Contains("escalate_to_human", StringComparer.OrdinalIgnoreCase) ||
                           conversation.Status == ConversationStatus.HumanActive;

        return new AgentChatResult(
            ResponseText: finalResponse,
            ToolsExecuted: executedTools,
            TotalTokens: totalTokens,
            EstimatedCostUsd: totalCostUsd,
            TotalLatencyMs: totalLatencyMs,
            TurnsCount: turnsCount,
            IsEscalated: isEscalated,
            ConversationId: conversation.Id);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentStreamEvent> StreamRunAsync(
        AgentChatInput input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Thực thi RunAsync và phát ra các sự kiện cho frontend/SSE
        yield return new AgentStreamEvent("status", "Bắt đầu phân tích yêu cầu...");

        AgentChatResult? result = null;
        string? errorMessage = null;
        try
        {
            result = await RunAsync(input, cancellationToken);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        if (errorMessage is not null)
        {
            yield return new AgentStreamEvent("error", errorMessage);
            yield break;
        }

        if (result is not null)
        {
            foreach (var tool in result.ToolsExecuted)
            {
                yield return new AgentStreamEvent("tool_executed", $"Đã tra cứu dữ liệu từ công cụ: {tool}", tool);
            }

            // Truyền phát câu trả lời
            yield return new AgentStreamEvent("completed", result.ResponseText);
        }
    }

    /// <summary>
    /// Tự động trích xuất các dữ kiện quan trọng từ tin nhắn khách hàng (SĐT, số người, người lớn/trẻ em, ngày đi, ngân sách).
    /// </summary>
    /// <param name="agentState">Trạng thái AgentState cần cập nhật.</param>
    /// <param name="message">Tin nhắn người dùng gửi đến.</param>
    private static void ExtractFactsFromUserMessage(AgentState agentState, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // 1. Số điện thoại liên hệ
        var phoneMatch = PhoneRegex.Match(message);
        if (phoneMatch.Success)
        {
            agentState.SetFact("customer_phone", phoneMatch.Groups[1].Value);
        }

        // 2. Số lượng người (Group Size / Pax)
        var adultsMatch = AdultsRegex.Match(message);
        var childrenMatch = ChildrenRegex.Match(message);
        var groupMatch = GroupSizeRegex.Match(message);

        if (adultsMatch.Success && childrenMatch.Success)
        {
            var adults = adultsMatch.Groups[1].Value;
            var children = childrenMatch.Groups[1].Value;
            if (int.TryParse(adults, out var a) && int.TryParse(children, out var c))
            {
                agentState.SetFact("group_size", $"{a + c} ({a} lớn, {c} bé)");
                agentState.SetFact("pax_adults", adults);
                agentState.SetFact("pax_children", children);
            }
        }
        else if (adultsMatch.Success)
        {
            agentState.SetFact("pax_adults", adultsMatch.Groups[1].Value);
            agentState.SetFact("group_size", adultsMatch.Groups[1].Value);
        }
        else if (groupMatch.Success)
        {
            agentState.SetFact("group_size", groupMatch.Groups[1].Value);
        }

        // 3. Ngày / Tháng khởi hành dự kiến
        var dateMatch = DateRegex.Match(message);
        if (dateMatch.Success)
        {
            agentState.SetFact("target_departure_date", dateMatch.Groups[1].Value);
        }
        else
        {
            var monthMatch = MonthRegex.Match(message);
            if (monthMatch.Success)
            {
                agentState.SetFact("target_month", monthMatch.Groups[1].Value);
            }
        }
    }

    /// <summary>
    /// Tự động đồng bộ các tham số và kết quả của công cụ vào danh sách ConfirmedFacts để Agent luôn ghi nhớ.
    /// </summary>
    /// <param name="agentState">Trạng thái AgentState.</param>
    /// <param name="toolName">Tên công cụ vừa thực thi.</param>
    /// <param name="argumentsJson">Tham số đầu vào JSON.</param>
    /// <param name="resultJson">Kết quả trả về JSON.</param>
    private static void SyncFactsAfterToolExecuted(
        AgentState agentState,
        string toolName,
        string argumentsJson,
        string resultJson)
    {
        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("customer_phone", out var pProp) && pProp.ValueKind == JsonValueKind.String)
                {
                    var phone = pProp.GetString();
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        agentState.SetFact("customer_phone", phone);
                    }
                }

                if (root.TryGetProperty("group_size", out var gProp))
                {
                    if (gProp.ValueKind == JsonValueKind.Number && gProp.TryGetInt32(out var g))
                    {
                        agentState.SetFact("group_size", g.ToString());
                    }
                    else if (gProp.ValueKind == JsonValueKind.String)
                    {
                        var gStr = gProp.GetString();
                        if (!string.IsNullOrWhiteSpace(gStr))
                        {
                            agentState.SetFact("group_size", gStr);
                        }
                    }
                }

                if (root.TryGetProperty("departure_date", out var dProp) && dProp.ValueKind == JsonValueKind.String)
                {
                    var dStr = dProp.GetString();
                    if (!string.IsNullOrWhiteSpace(dStr))
                    {
                        agentState.SetFact("departure_date", dStr);
                    }
                }

                if (root.TryGetProperty("travel_id", out var tProp) && tProp.TryGetInt64(out var tid) && tid > 0)
                {
                    agentState.SetFact("current_travel_id", tid.ToString());
                }

                if (root.TryGetProperty("total_price", out var prProp))
                {
                    if (prProp.ValueKind == JsonValueKind.Number && prProp.TryGetDecimal(out var price))
                    {
                        agentState.SetFact("total_price", $"{price:N0} VNĐ");
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        // Đọc mã phiếu nếu công cụ escalate_to_human trả về kết quả
        if (!string.IsNullOrWhiteSpace(resultJson) && toolName.Equals("escalate_to_human", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var resDoc = JsonDocument.Parse(resultJson);
                if (resDoc.RootElement.TryGetProperty("ticket_code", out var tcProp) && tcProp.ValueKind == JsonValueKind.String)
                {
                    var code = tcProp.GetString();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        agentState.SetFact("active_ticket_code", code);
                    }
                }
            }
            catch (JsonException)
            {
            }
        }
    }
}

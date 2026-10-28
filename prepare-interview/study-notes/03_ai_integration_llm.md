# 🤖 NHÓM 3: AI Integration & LLM Patterns
> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

## 🗺️ Bản Đồ Tư Duy

```
AI Integration & LLM Patterns
├── 1. ILlmClient Abstraction (Vendor-neutral)
├── 2. Tool Calling / Function Calling Protocol
│   ├── Tool Definition (JSON Schema)
│   ├── Tool Execution Flow
│   └── 5 Agent Tools của TourAgent
├── 3. Agent Loop Orchestrator
│   ├── Max Turns Guardrail
│   ├── Audit Trail (LlmCall entity)
│   └── Tool Guardrail Pipeline (Harness)
├── 4. Zero Hallucination Strategy
│   ├── Tool-only data retrieval
│   └── Prompt Engineering
├── 5. Streaming (SSE)
├── 6. Agent State & Memory
│   ├── Conversation entity
│   └── Message history injection
└── 7. Omnichannel Queue
    ├── System.Threading.Channels
    └── Telegram Adapter pattern
```

---

## 1️⃣ ILlmClient — Vendor-Neutral Abstraction

### Interface trong Application layer

```csharp
// Application/Abstractions/ILlmClient.cs
public interface ILlmClient
{
    /// <summary>
    /// Gửi danh sách message đến LLM và nhận response (sync).
    /// </summary>
    Task<LlmResponse> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<ToolDefinition>? tools,
        CancellationToken ct);

    /// <summary>
    /// Streaming response từ LLM (token by token).
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken ct);
}
```

### Implementation trong Infrastructure

```csharp
// Infrastructure/LlmClient/DeepSeekLlmClient.cs
public sealed class DeepSeekLlmClient(HttpClient httpClient) : ILlmClient
{
    private const string ApiUrl = "https://api.deepseek.com/v1/chat/completions";

    /// <summary>Gọi DeepSeek V3 API theo OpenAI Function Calling protocol.</summary>
    public async Task<LlmResponse> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<ToolDefinition>? tools,
        CancellationToken ct)
    {
        // DeepSeek dùng OpenAI-compatible protocol
        var requestBody = new { model = "deepseek-chat", messages, tools };
        // ...
    }
}
```

**Q: Tại sao cần ILlmClient abstraction?**
> - **Vendor lock-in risk**: Hôm nay DeepSeek, ngày mai GPT-4o, tháng sau Gemini — chỉ cần đổi 1 file Implementation
> - **Testability**: `FakeLlmClient` trong Unit Test không cần internet, không tốn tiền API
> - **Root cause**: Không có abstraction → refactor vendor = đụng vào toàn bộ code business

---

## 2️⃣ Tool Calling / Function Calling Protocol

### Luồng hoạt động (IPO view)

```
INPUT: User message
  ↓
LLM nhận message + danh sách Tool definitions
  ↓
LLM quyết định: Cần gọi Tool nào? (tool_calls trong response)
  ↓
Code thực thi Tool → query DB → trả về kết quả
  ↓
Inject kết quả Tool vào conversation history
  ↓
LLM nhận lịch sử + kết quả Tool → tạo final response
  ↓
OUTPUT: Response có dữ liệu chính xác từ DB
```

### Tool Definition (JSON Schema)

```csharp
// Application/Agent/Tools/SearchToursTool.cs
public static ToolDefinition GetDefinition() => new(
    Name: "search_tours",
    Description: "Tìm kiếm tour du lịch theo điểm đến và ngày khởi hành",
    Parameters: new
    {
        type = "object",
        properties = new
        {
            destination = new { type = "string", description = "Điểm đến (ví dụ: Paris, Đà Nẵng)" },
            departure_date = new { type = "string", format = "date", description = "Ngày khởi hành (YYYY-MM-DD)" }
        },
        required = new[] { "destination" }
    }
);
```

### 5 Agent Tools của TourAgent

| Tool | Mục đích | Input | Output |
|---|---|---|---|
| `search_tours` | Tìm tour theo điểm đến | destination, date | Danh sách tour |
| `get_tour_departures` | Lấy lịch khởi hành | tourId | Danh sách departures + giá |
| `get_tour_itinerary` | Lịch trình chi tiết | tourId | Lịch trình theo ngày |
| `get_tour_policies` | Chính sách hủy/đặt | tourId | Chính sách |
| `escalate_to_human` | Chuyển cho tư vấn viên | customerPhone | Confirmation |

---

## 3️⃣ Agent Loop Orchestrator

### Sơ đồ Agent Loop

```
User Message
    │
    ▼
┌─────────────────────────────────────────────┐
│              Agent Loop (Max 5 turns)        │
│                                             │
│  Turn 1: LLM → tool_calls: [search_tours]  │
│  Turn 2: Execute tool → inject result       │
│  Turn 3: LLM → tool_calls: [get_departures]│
│  Turn 4: Execute tool → inject result       │
│  Turn 5: LLM → final_answer (no more tools)│
│                                             │
│  [Log LlmCall: tokens_in, tokens_out,      │
│   latency_ms, tools_called]                │
└─────────────────────────────────────────────┘
    │
    ▼
Final Response to User
```

### Code Pattern

```csharp
// Application/Agent/Services/AgentLoop.cs
public sealed class AgentLoop(ILlmClient llmClient, IEnumerable<IAgentTool> tools)
{
    private const int MaxTurns = 5;   // Guardrail: tránh infinite loop

    /// <summary>
    /// Điều phối vòng lặp tool-calling giữa LLM và các Agent Tools.
    /// Dừng khi LLM không còn gọi tool hoặc đạt MaxTurns.
    /// </summary>
    public async Task<AgentResult> RunAsync(
        IReadOnlyList<LlmMessage> initialMessages,
        CancellationToken ct)
    {
        var messages = initialMessages.ToList();

        for (int turn = 0; turn < MaxTurns; turn++)
        {
            var response = await llmClient.CompleteAsync(messages, GetToolDefinitions(), ct);

            // Audit trail
            await _db.LlmCalls.AddAsync(new LlmCall
            {
                TokensIn = response.Usage.InputTokens,
                TokensOut = response.Usage.OutputTokens,
                LatencyMs = response.LatencyMs,
                ToolsCalled = response.ToolCalls?.Select(t => t.Name).ToList()
            }, ct);

            // Nếu LLM không gọi tool nào → done
            if (response.ToolCalls is null || !response.ToolCalls.Any())
                return AgentResult.Success(response.Content);

            // Execute tools và inject kết quả vào conversation
            foreach (var toolCall in response.ToolCalls)
            {
                var tool = tools.First(t => t.Name == toolCall.Name);
                var result = await tool.ExecuteAsync(toolCall.Arguments, ct);
                messages.Add(LlmMessage.ToolResult(toolCall.Id, result));
            }
        }

        return AgentResult.Failure("Exceeded max turns");
    }
}
```

**Q: Tại sao có Max Turns limit?**
> **Inversion Thinking**: "Agent có thể fail theo cách nào?" → Infinite loop (LLM gọi tool mãi mãi) → tốn tiền API, block thread, DOS chính mình.
> Max Turns = **Tool Guardrail** — hàng rào an toàn.

---

## 4️⃣ Zero Hallucination Strategy

### Vấn đề & Root Cause

```
❌ Không có Tool Guardrail:
User: "Tour Đà Nẵng bao nhiêu tiền?"
LLM: "Tour Đà Nẵng 5 ngày 4 đêm chỉ 8.500.000đ" ← Bịa!

✅ Với Tool Calling:
User: "Tour Đà Nẵng bao nhiêu tiền?"
LLM → tool: search_tours(destination="Đà Nẵng")
DB  → result: [{name: "Đà Nẵng Hội An", price: 9.200.000, ...}]
LLM → "Tour Đà Nẵng Hội An hiện có giá 9.200.000đ (từ DB)"
```

### System Prompt Design

```
// Application/Agent/Prompts/SystemPrompt.cs
RULES:
1. Giá tour, ngày bay, lịch trình: CHỈ lấy từ Tool kết quả.
   KHÔNG bao giờ tự đặt ra con số.
2. Nếu Tool không trả về kết quả → nói thật với khách là không có thông tin.
3. Khi khách muốn đặt tour hoặc cần tư vấn sâu → dùng escalate_to_human tool.
```

**Q: Tại sao LLM có thể "hallucinate" giá tiền?**
> LLM là mô hình xác suất — nó predict token tiếp theo dựa trên pattern trong training data. Không có "ý thức" về đúng/sai. Khi không có dữ liệu chính xác → nó vẫn generate ra con số nghe có vẻ hợp lý.
> **Root cause**: LLM không "biết" gì cả — nó chỉ "đoán" pattern tốt.

---

## 5️⃣ Agent State & Memory

### Conversation Entity — lưu toàn bộ lịch sử

```
Conversation
├── Id (Guid)
├── Channel (Web / Telegram / Zalo)
├── CustomerInfo (SĐT, Zalo)
├── Status (Active / Escalated / Closed)
└── Messages[]
    ├── Role (User / Assistant / Tool)
    ├── Content (text)
    └── ToolCallId (nếu là tool result)
```

### State Injection vào LLM prompt

```csharp
// Khi bắt đầu mỗi turn, inject toàn bộ lịch sử vào LLM
var messages = conversation.Messages
    .Select(m => new LlmMessage(m.Role, m.Content))
    .ToList();

// Thêm System Prompt (có Agent State nếu cần)
messages.Insert(0, LlmMessage.System(SystemPrompt.Build(conversation)));
```

**Q: Tại sao phải inject toàn bộ history?**
> LLM là **stateless** — mỗi request là independent. Nó không "nhớ" conversation trước. Phải truyền toàn bộ context vào mỗi request để nó "nhớ" được. Đây là **context window** concept.

---

## 6️⃣ Omnichannel Queue Pattern

### Vấn đề: Telegram Webhook Timeout

```
❌ Xử lý đồng bộ — TIMEOUT:
Telegram → POST /webhook → Handler gọi LLM (5-15s) → Telegram timeout 60s
→ Telegram retry → loop vô tận

✅ Async Queue Pattern — ZERO LATENCY:
Telegram → POST /webhook → Push vào Channel → 200 OK ngay (< 50ms)
                                  ↓
                        BackgroundService dequeue
                                  ↓
                         Gọi LLM Agent Loop
                                  ↓
                         TelegramAdapter gửi response
```

### System.Threading.Channels

```csharp
// Application/Channels/MessageChannel.cs
public class IncomingMessageChannel
{
    // Bounded channel: nếu queue đầy → back-pressure (caller phải chờ)
    private readonly Channel<IncomingMessage> _channel =
        Channel.CreateBounded<IncomingMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public ChannelWriter<IncomingMessage> Writer => _channel.Writer;
    public ChannelReader<IncomingMessage> Reader => _channel.Reader;
}
```

**Q: Channels vs RabbitMQ/Azure Service Bus — trade-off?**
| | System.Threading.Channels | Message Queue |
|---|---|---|
| Latency | Cực thấp (in-memory) | Network overhead |
| Durability | Mất khi app restart | Persist trên disk |
| Scale | Single instance | Multi-instance |
| Setup | Zero infra | Cần broker server |
| Dùng khi | SME, single server | Microservice, HA |

---

## 📝 Checklist Tự Kiểm Tra

- [ ] Vẽ được IPO của Agent Loop (không nhìn tài liệu)
- [ ] Giải thích tại sao cần ILlmClient abstraction (vendor, testability)
- [ ] Giải thích Zero Hallucination bằng ví dụ cụ thể
- [ ] Giải thích tại sao LLM "hallucinate" (bản chất probabilistic)
- [ ] Mô tả Telegram Webhook pattern và tại sao dùng async queue
- [ ] Giải thích trade-off Channels vs Message Queue

---

## 🆕 Cập Nhật Mới

<!-- Thêm kiến thức/câu hỏi mới vào đây -->

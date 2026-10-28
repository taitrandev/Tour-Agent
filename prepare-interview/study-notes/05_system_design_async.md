# ⚙️ NHÓM 5: System Design & Async Patterns
> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

## 🗺️ Bản Đồ Tư Duy

```
System Design & Async Patterns
├── 1. System.Threading.Channels
│   ├── Bounded vs Unbounded Channel
│   ├── Producer / Consumer pattern
│   └── Back-pressure handling
├── 2. BackgroundService
│   ├── IHostedService vs BackgroundService
│   ├── Graceful shutdown
│   └── IServiceScopeFactory (DbContext in BG)
├── 3. Server-Sent Events (SSE)
│   ├── SSE vs WebSocket vs Long Polling
│   └── IAsyncEnumerable streaming
├── 4. Idempotency & Reliability
│   ├── Idempotency Key
│   └── Retry + Fallback
├── 5. Observability
│   ├── Structured Logging (LlmCall audit)
│   ├── Health Checks
│   └── Correlation ID / Trace
└── 6. API Design Principles
    ├── RESTful conventions
    ├── Problem Details (RFC 7807)
    └── Versioning (/api/v1/)
```

---

## 1️⃣ System.Threading.Channels

### Cơ chế hoạt động

```
Producer (Webhook)          Consumer (BackgroundService)
     │                              │
     │──── Write(message) ────►  [bounded queue: 100]
     │     (nếu full → Wait)        │
     │                              │──── ReadAllAsync() ──► Process
     │                              │
```

### Code Pattern trong TourAgent

```csharp
// Application/Channels/IncomingMessageChannel.cs
public sealed class IncomingMessageChannel
{
    // Bounded: giới hạn 100 items → tránh memory overflow
    private readonly Channel<IncomingMessage> _channel =
        Channel.CreateBounded<IncomingMessage>(
            new BoundedChannelOptions(capacity: 100)
            {
                FullMode = BoundedChannelFullMode.Wait,  // Producer phải chờ nếu đầy
                SingleReader = true,   // Chỉ 1 consumer → optimize
                SingleWriter = false   // Nhiều producer OK (webhook có thể đến đồng thời)
            });

    public ChannelWriter<IncomingMessage> Writer => _channel.Writer;
    public ChannelReader<IncomingMessage> Reader => _channel.Reader;
}

// Infrastructure/Telegram/TelegramWebhookController.cs — Producer
[HttpPost("telegram/webhook")]
public async Task<IActionResult> ReceiveUpdate(
    [FromBody] TelegramUpdate update,
    [FromServices] IncomingMessageChannel channel,
    CancellationToken ct)
{
    // Đẩy vào queue — không xử lý ở đây
    await channel.Writer.WriteAsync(
        new IncomingMessage(Channel.Telegram, update.Message.Text, update.Message.Chat.Id),
        ct);

    return Ok();  // ← Response ngay lập tức (< 50ms)
}
```

### Bounded vs Unbounded

| | Bounded | Unbounded |
|---|---|---|
| Capacity | Giới hạn (ví dụ: 100) | Vô hạn (RAM chịu) |
| FullMode | Wait / Drop / Error | N/A |
| Memory safety | ✅ Có giới hạn | ❌ Có thể OOM |
| Back-pressure | ✅ Producer tự động chậm lại | ❌ Không có |
| Dùng khi | Hầu hết cases | Biết chắc consumer nhanh hơn producer |

---

## 2️⃣ BackgroundService

### IHostedService vs BackgroundService

```csharp
// BackgroundService là abstract class implement IHostedService
// → chỉ cần override ExecuteAsync

// Infrastructure/BackgroundServices/TelegramWebhookBackgroundService.cs
public sealed class TelegramWebhookBackgroundService(
    IncomingMessageChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramWebhookBackgroundService> logger)
    : BackgroundService
{
    /// <summary>
    /// Vòng lặp chính: đọc message từ Channel và xử lý qua Agent.
    /// Chạy suốt vòng đời ứng dụng, dừng khi CancellationToken bị cancel.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Telegram Background Service started");

        // ReadAllAsync tự động dừng khi Channel closed hoặc CT cancelled
        await foreach (var message in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                // Log và tiếp tục — không để 1 message lỗi crash toàn bộ service
                logger.LogError(ex, "Error processing message {MessageId}", message.Id);
            }
        }
    }

    private async Task ProcessMessageAsync(IncomingMessage message, CancellationToken ct)
    {
        // ⚠️ DbContext là Scoped — phải tạo scope mới trong BackgroundService
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<HandleIncomingMessageHandler>();

        var result = await handler.HandleAsync(
            new HandleIncomingMessageCommand(message), ct);

        if (!result.IsSuccess)
            logger.LogWarning("Message processing failed: {Error}", result.Error);
    }
}
```

**Q: Tại sao phải dùng IServiceScopeFactory trong BackgroundService?**
> BackgroundService là **Singleton** (sống suốt vòng đời app). DbContext là **Scoped** (sống trong 1 request). Nếu Singleton inject Scoped → **Captive Dependency** → DbContext không được dispose đúng cách → memory leak, stale data.
> Giải pháp: Mỗi message tạo 1 scope mới → DbContext fresh, đúng lifetime.

### Graceful Shutdown

```csharp
// Khi app shutdown, .NET gửi CancellationToken cho ExecuteAsync
// BackgroundService tự động stop khi stoppingToken cancelled
// ReadAllAsync sẽ thoát ra khỏi vòng lặp
```

---

## 3️⃣ Server-Sent Events (SSE)

### SSE vs WebSocket vs Long Polling

| | SSE | WebSocket | Long Polling |
|---|---|---|---|
| Direction | Server → Client (1 chiều) | 2 chiều | Giả lập 2 chiều |
| Protocol | HTTP/1.1, HTTP/2 | WS protocol | HTTP |
| Complexity | Đơn giản | Phức tạp hơn | Cồng kềnh |
| Reconnect | Tự động built-in | Phải tự implement | N/A |
| Dùng cho | AI streaming, notifications | Chat real-time, game | Legacy support |
| TourAgent dùng | ✅ `/chat/stream` | — | — |

### SSE Implementation

```csharp
// Api/Endpoints/AgentEndpoints.cs
app.MapPost("/api/v1/agent/chat/stream", async (
    ChatRequest request,
    AgentLoop agentLoop,
    HttpResponse response,
    CancellationToken ct) =>
{
    // Set SSE headers
    response.Headers.Append("Content-Type", "text/event-stream");
    response.Headers.Append("Cache-Control", "no-cache");
    response.Headers.Append("X-Accel-Buffering", "no");  // Nginx: disable buffering

    // Stream tokens từ LLM
    await foreach (var token in agentLoop.StreamAsync(request, ct))
    {
        // SSE format: "data: <content>\n\n"
        await response.WriteAsync($"data: {token}\n\n", ct);
        await response.Body.FlushAsync(ct);  // Flush ngay để client nhận được
    }

    // Signal done
    await response.WriteAsync("data: [DONE]\n\n", ct);
});
```

### Client-side (JavaScript)

```javascript
// Frontend — kết nối SSE
const eventSource = new EventSource('/api/v1/agent/chat/stream', {
    method: 'POST',
    // Dùng fetch thay vì EventSource cho POST + custom headers
});

// Hoặc dùng fetch với ReadableStream
const response = await fetch('/api/v1/agent/chat/stream', {
    method: 'POST',
    body: JSON.stringify({ message: userInput }),
    headers: { 'Content-Type': 'application/json' }
});

const reader = response.body.getReader();
while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    const token = new TextDecoder().decode(value);
    appendToChat(token);  // Hiển thị từng token
}
```

---

## 4️⃣ Idempotency & Reliability

### Telegram Idempotency

```csharp
// Telegram có thể gửi cùng 1 update nhiều lần (retry)
// → Phải idempotent: xử lý 1 update_id chỉ 1 lần

// Infrastructure/Telegram/TelegramWebhookController.cs
[HttpPost("telegram/webhook")]
public async Task<IActionResult> ReceiveUpdate([FromBody] TelegramUpdate update)
{
    // Check idempotency: update_id đã xử lý chưa?
    if (await _db.ProcessedUpdates.AnyAsync(p => p.UpdateId == update.UpdateId))
        return Ok();  // Đã xử lý → trả OK ngay, bỏ qua

    // Mark as processed ngay (trước khi xử lý — at-least-once semantics)
    await _db.ProcessedUpdates.AddAsync(new ProcessedUpdate(update.UpdateId));
    await _db.SaveChangesAsync();

    // Đẩy vào Channel
    await _channel.Writer.WriteAsync(IncomingMessage.FromTelegram(update));

    return Ok();
}
```

### Retry + Fallback Pattern

```csharp
// Khi gửi Telegram message thất bại → fallback sang plain text
public sealed class TelegramAdapter(HttpClient httpClient)
{
    /// <summary>Gửi message về Telegram, fallback plain text nếu Markdown lỗi.</summary>
    public async Task SendAsync(long chatId, string markdown, CancellationToken ct)
    {
        try
        {
            await SendMarkdownAsync(chatId, markdown, ct);
        }
        catch (TelegramApiException ex) when (ex.ErrorCode == 400)
        {
            // Telegram reject Markdown → fallback plain text
            var plainText = StripMarkdown(markdown);
            await SendPlainTextAsync(chatId, plainText, ct);
        }
    }
}
```

---

## 5️⃣ Observability

### LlmCall Audit Trail

```csharp
// Domain/Conversations/LlmCall.cs — Entity ghi lại mỗi lần gọi LLM
public sealed class LlmCall : Entity<Guid>
{
    public Guid ConversationId { get; private set; }
    public int TokensIn { get; private set; }
    public int TokensOut { get; private set; }
    public long LatencyMs { get; private set; }
    public IReadOnlyList<string> ToolsCalled { get; private set; }
    public DateTimeOffset CalledAt { get; private set; }

    // Dùng để:
    // 1. Debug khi agent behave lạ
    // 2. Tính chi phí API (token × price)
    // 3. Monitor performance (latency trends)
}
```

**Q: Tại sao cần audit LlmCall?**
> - **Cost control**: Token in/out × giá LLM vendor = chi phí thực. Không track → không biết đang đốt bao nhiêu tiền
> - **Debug**: Khi Agent trả lời sai → xem history LlmCall để trace lại tool nào gọi, context gì inject
> - **Performance**: Latency trends → biết khi nào LLM chậm

### Structured Logging

```csharp
// Dùng ILogger với structured logging thay vì string concatenation
logger.LogInformation(
    "Agent loop completed. Turns: {Turns}, TokensTotal: {Tokens}, LatencyMs: {Latency}",
    turns, totalTokens, latencyMs);

// Không làm:
logger.LogInformation($"Agent loop completed. Turns: {turns}");  // Không query được
```

---

## 6️⃣ API Design Principles

### RESTful Conventions trong TourAgent

```
GET    /api/v1/tours                  → Search tours
GET    /api/v1/tours/{id}/itinerary   → Get itinerary
POST   /api/v1/agent/chat             → Chat (sync)
POST   /api/v1/agent/chat/stream      → Chat (SSE stream)
POST   /api/v1/telegram/webhook       → Telegram webhook
GET    /api/v1/health                 → Health check
```

### Problem Details (RFC 7807)

```csharp
// Trả về lỗi theo chuẩn RFC 7807 — không tự đặt ra format
return Results.Problem(
    detail: "Tour not found with the provided ID",
    statusCode: 404,
    title: "Resource Not Found",
    type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
);

// Response JSON:
// {
//   "type": "https://...",
//   "title": "Resource Not Found",
//   "status": 404,
//   "detail": "Tour not found with the provided ID"
// }
```

**Q: Tại sao dùng Problem Details thay vì tự định nghĩa error format?**
> Standard format → client có thể handle error mà không cần đọc doc. Interoperable với các tool, API gateway, monitoring systems.

---

## 📝 Checklist Tự Kiểm Tra

- [ ] Giải thích được Bounded Channel và Back-pressure là gì
- [ ] Vẽ được sơ đồ Webhook → Channel → BackgroundService
- [ ] Giải thích tại sao BackgroundService cần IServiceScopeFactory
- [ ] So sánh SSE vs WebSocket (khi nào dùng cái nào)
- [ ] Viết được SSE endpoint cơ bản
- [ ] Giải thích Idempotency với ví dụ Telegram webhook
- [ ] Giải thích tại sao cần audit LlmCall (3 lý do)

---

## 🆕 Cập Nhật Mới

<!-- Thêm kiến thức/câu hỏi mới vào đây -->

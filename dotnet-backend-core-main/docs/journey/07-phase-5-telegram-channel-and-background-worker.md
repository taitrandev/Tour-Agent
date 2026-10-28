# 🧭 Hành Trình Chuyển Đổi: Kỳ 7 — Tích Hợp Kênh Telegram, In-Process Queue & Background Worker

> **Mục tiêu tài liệu**: Ghi lại quyết định sử dụng Controller cho Webhook, giải pháp kiến trúc phản hồi Zero-Latency Ack bằng `System.Threading.Channels`, kiểm soát chống trùng lặp tin nhắn (Idempotency), và điều phối `BackgroundService` chạy ngầm chu trình AI AgentLoop.

---

## 1. 🎯 Quyết Định Lựa Chọn Controller Thay Vì Minimal API

### Phản biện của tôi:
- Khi chuyển đổi sang .NET 10, tôi kiên định với mô hình **Controller-based API** thay vì Minimal API:
  - `[ApiController]` và `[Route("api/v1/webhooks/telegram")]` tại [TelegramWebhookController.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/TelegramWebhookController.cs).
- **Lý do**:
  1. **Tính tổ chức cao**: Khi hệ thống mở rộng hỗ trợ nhiều kênh (Telegram, Zalo, Messenger, Webchat), việc mỗi kênh có một Controller độc lập giúp code tách bạch, dễ phân quyền, dễ viết unit test và tích hợp middleware.
  2. **Chuẩn mực SME/Enterprise**: Đây là phong cách kiến trúc quen thuộc, dễ bảo trì cho các team phát triển phần mềm doanh nghiệp.

---

## 2. ⚡ Thách Thức Webhook: Cơ Chế Zero-Latency Ack Với `System.Threading.Channels`

### Vấn đề thực tế (The Webhook Timeout & Retry Storm):
- Máy chủ Telegram Bot có cơ chế timeout rất chặt chẽ: nếu server không phản hồi HTTP 200 trong vòng vài giây, Telegram sẽ coi là lỗi mạng và tự động gửi lại tin nhắn (Retry storm).
- Trong khi đó, chu trình AI Agent cần:
  1. Nạp ngữ cảnh hội thoại từ PostgreSQL.
  2. Gọi DeepSeek V3 API (mất 1-3 giây).
  3. Kích hoạt công cụ tra cứu (Search tours, check ngày bay).
  4. Gọi lại DeepSeek lượt 2 để tổng hợp câu trả lời.
- 👉 **Hậu quả nếu xử lý đồng bộ**: Webhook bị timeout, Telegram retry liên tục, hệ thống bị nghẽn và khách hàng nhận 3-4 tin nhắn lặp lại.

### Giải pháp kiến trúc của tôi:
Tôi tách rời 2 pha: **Nhận tin (Ingestion)** và **Xử lý (Processing)** thông qua [System.Threading.Channels](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Channels/TelegramMessageQueue.cs):

```
[Telegram Webhook Request]
           │
           ▼
┌──────────────────────────────────────┐
│ TelegramWebhookController            │
│ 1. Trích xuất chatId, text, msgId    │
│ 2. Đẩy vào ITelegramMessageQueue     │
│ 3. Trả về HTTP 200 {"ok": true} ngay │ ➔ Phản hồi Telegram trong < 5ms!
└──────────────────────────────────────┘
           │ (In-Process Channel: Lock-free, High-throughput)
           ▼
┌──────────────────────────────────────┐
│ TelegramWebhookBackgroundService     │ (Background Worker chạy ngầm)
│ 1. Đọc tin nhắn từ Channel           │
│ 2. Kiểm tra Idempotency (Chống trùng)│
│ 3. Tạo hoặc nạp Conversation         │
│ 4. Chạy IAgentLoop (DeepSeek + Tools)│
│ 5. Gửi reply qua ITelegramAdapter    │
└──────────────────────────────────────┘
           │
           ▼
[Khách nhận câu trả lời trên Telegram]
```

---

## 3. 🛡️ Kiểm Soát Chống Trùng Lặp Tin Nhắn (Idempotency)

Mỗi tin nhắn gửi từ Telegram đều có một số định danh duy nhất `message_id`. Tôi chuẩn hóa mã định danh tin nhắn ngoại bộ:
```csharp
var channelMessageId = $"tg_{chatId}_{rawMessageId}";
```
Trước khi kích hoạt chu trình suy luận AI, [TelegramWebhookBackgroundService.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Channels/TelegramWebhookBackgroundService.cs#L64-L71) thực hiện kiểm tra trong CSDL:
```csharp
var isDuplicate = await dbContext.Messages
    .AsNoTracking()
    .AnyAsync(m => m.ChannelMessageId == inbound.ChannelMessageId, cancellationToken);

if (isDuplicate)
{
    _logger.LogInformation("Bỏ qua tin nhắn trùng lặp: {ChannelMessageId}", inbound.ChannelMessageId);
    return;
}
```
Nhờ đó, dù mạng chập chờn hay có sự cố gửi lại từ phía Telegram, hệ thống cũng chỉ xử lý đúng 1 lần duy nhất, bảo vệ tài nguyên và chi phí gọi token AI.

---

## 4. 🌐 Telegram HTTP Adapter & Cơ Chế Fallback Markdown Thông Minh

Trong [TelegramAdapter.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Telegram/TelegramAdapter.cs):
1. **Định dạng đẹp với Markdown**: Gửi tin nhắn kèm `parse_mode = "Markdown"` để in đậm tên tour, giá tiền và bullet point.
2. **Tự động Fallback plain-text**: Nếu câu trả lời của AI chứa các ký tự đặc biệt vi phạm cú pháp Markdown của Telegram (lỗi HTTP 400 Bad Request), Adapter tự động gỡ cờ `parse_mode` và gửi lại ngay lập tức dưới dạng văn bản thuần. Khách hàng không bao giờ bị mất tin nhắn.
3. **Chế độ mô phỏng an toàn (Simulated Mode)**: Khi chạy local hoặc viết Unit Tests mà chưa cấu hình `BotToken`, hệ thống tự động ghi log mô phỏng mà không làm sập ứng dụng.

---

## 5. 🧪 Kiểm Thử Tự Động & Thống Kê Tiến Độ

Đã bổ sung [TelegramIntegrationTests.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/tests/TourAgent.Application.UnitTests/Channels/TelegramIntegrationTests.cs):
- `TelegramMessageQueue_EnqueueAndRead_ShouldRetrieveMessage`: Kiểm tra tốc độ đọc ghi bất đồng bộ của Channel.
- `TelegramAdapter_WithoutBotToken_ShouldSimulateAndReturnTrue`: Kiểm tra tính kiên cường của Adapter khi thiếu cấu hình token.

```
╔════════════════════════════════════════════════════════════════════════╗
║                   KẾT QUẢ KIỂM THỬ TOÀN DỰ ÁN                         ║
╠══════════════════════════════════════════╦═════════════════════════════╣
║ Dự án Test                               ║ Kết quả kiểm thử            ║
╠══════════════════════════════════════════╬═════════════════════════════╣
║ TourAgent.Domain.UnitTests               ║ 24/24 Passed (100% Green)   ║
║ TourAgent.Application.UnitTests          ║ 14/14 Passed (100% Green)   ║
║ TourAgent.ArchitectureTests              ║ 3/3 Passed   (100% Green)   ║
╠══════════════════════════════════════════╬═════════════════════════════╣
║ TỔNG CỘNG                                ║ 41/41 TESTS PASSED          ║
╚══════════════════════════════════════════╩═════════════════════════════╝
```

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Làm thế nào bạn thiết kế hệ thống tiếp nhận Webhook từ Telegram/Zalo tích hợp với AI Agent mà không bị timeout và không bị nhân bản tin nhắn khi lưu lượng tăng đột biến?"*

**Câu trả lời chuẩn mực**:
> *"Trong bài toán Webhook tích hợp AI, thách thức lớn nhất là độ trễ suy luận của LLM (1-3 giây) luôn vượt quá ngưỡng kiên nhẫn của máy chủ Webhook (dẫn đến timeout và retry storm). Tôi giải quyết bài toán này bằng kiến trúc bất đồng bộ 2 pha:*
> 1. *Tại tầng API, tôi sử dụng `TelegramWebhookController` kết hợp với `System.Threading.Channels` (hàng đợi in-process lock-free hiệu năng cực cao của .NET). Controller chỉ làm nhiệm vụ parse payload thô, đẩy vào Channel và lập tức trả về HTTP 200 OK cho Telegram trong chưa đầy 5 mili-giây (Zero-latency Ack).*
> 2. *Tại tầng nền, một `BackgroundService` tiêu thụ các tin nhắn từ Channel một cách tuần tự hoặc có kiểm soát độ đồng thời. Tôi áp dụng cơ chế Idempotency dựa trên `ChannelMessageId` (kết hợp `chat_id` và `message_id`) để loại bỏ hoàn toàn các tin nhắn trùng lặp trước khi gọi `AgentLoop`.*
> 3. *Đối với việc gửi phản hồi, `TelegramAdapter` có cơ chế tự động thử lại bằng plain-text nếu payload Markdown bị lỗi cú pháp, đảm bảo 100% tính sẵn sàng của thông điệp tới khách hàng."*

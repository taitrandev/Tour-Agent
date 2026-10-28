# 🗺️ Kỳ 11: Bước 3 — Giám Sát Hội Thoại & Kiểm Toán Đo Lường AI Telemetry (Observability Live Inspector)

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**  
> **Chủ đề**: Xây dựng hệ thống quan sát AI toàn diện (AI Observability & Telemetry), quản trị chi phí FinOps (Token & USD), chẩn đoán độ trễ mạng (Latency ms), giải quyết triệt để sự cố vi phạm ràng buộc CSDL (NOT NULL Constraint), hoàn thiện cổng tích hợp Telegram Webhook và LLM Gateway Settings.

---

## 💡 1. Bối Cảnh & Vấn Đề Kỹ Thuật (The Challenge)

Khi triển khai các ứng dụng thực tế tích hợp Mô hình Ngôn ngữ Lớn (LLM), hai thách thức lớn nhất mà doanh nghiệp phải đối mặt là:
1. **Hộp đen suy luận (Blackbox AI)**: Không biết AI đã "suy nghĩ" những gì, gọi các công cụ nào, trả về kết quả gì, và vì sao lại đưa ra câu trả lời đó cho khách hàng.
2. **Chi phí ngoài tầm kiểm soát (FinOps Spikes) & Độ trễ (Latency)**: Mỗi lượt trao đổi tiêu tốn bao nhiêu token đầu vào (Prompt Tokens), bao nhiêu token sinh ra (Completion Tokens), chi phí thực tế tính bằng USD là bao nhiêu, và mất bao nhiêu mili-giây để phản hồi.

Trong hệ thống PHP cũ, module `AiAgent` kết hợp cùng giao diện `admin.conversations` và `admin.llm-settings` để giám sát các chỉ số này. Khi chuyển sang .NET 10, nhiệm vụ là phải xây dựng lại toàn bộ hạ tầng đo lường vi mô (Micro-telemetry) theo chuẩn Clean Architecture.

> 🎯 **Mục tiêu kỹ thuật của Bước 3**:
> 1. Thiết kế bảng ghi kiểm toán `llm_calls` ghi lại đầy đủ: `prompt_tokens`, `completion_tokens`, `cost_usd`, `latency_ms`, `tools_called`, `provider`, `finish_reason`, `state_snapshot`.
> 2. Xây dựng API và giao diện Quản Trị Giám Sát Hội Thoại (`/admin/conversations`) gồm 2 cột: Danh sách hội thoại bên trái và Khung chat thời gian thực kèm Thanh phân tích Telemetry bên phải.
> 3. Tích hợp trọn vẹn Kênh Telegram Bot qua Webhook ngầm và Trung tâm Cấu hình LLM DeepSeek Gateway (`/admin/settings/llm`).

---

## 🛠️ 2. Sự Cố Thực Tế & Cách Xử Lý Triệt Để (Real-World Incident Post-Mortem)

Trong quá trình đưa hệ thống vào thử nghiệm nhận tin nhắn từ Telegram Webhook thực tế, một sự cố nghiêm trọng đã phát sinh:
> **Triệu chứng**: Khi khách hàng gửi tin nhắn vào Bot Telegram, máy chủ ngrok báo `POST /api/v1/agent/webhook/telegram 200 OK`, nhưng trên Telegram người dùng mãi không nhận được câu trả lời từ AI.

### Phân tích nguyên nhân gốc rễ (Root Cause Analysis):
1. **Kiến trúc Zero-latency Ack**: Endpoint [`TelegramWebhookController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/TelegramWebhookController.cs) nhận webhook, đẩy ngay vào `System.Threading.Channels` và trả về `200 OK` tức thì để tránh Telegram gửi lại (retry). Do đó ngrok luôn thấy mã 200.
2. **Lỗi ngầm trong Worker**: Khi [`TelegramWebhookBackgroundService.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Channels/TelegramWebhookBackgroundService.cs) lấy tin nhắn ra và kích hoạt [`AgentLoop.RunAsync`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Services/AgentLoop.cs), câu lệnh `_dbContext.SaveChangesAsync()` bị văng ngoại lệ:
   ```
   Npgsql.PostgresException (0x80004005): 23502: null value in column "provider" of relation "llm_calls" violates not-null constraint
   ```
3. Trong CSDL PostgreSQL, bảng `llm_calls` có ràng buộc `provider VARCHAR(30) NOT NULL`. Trong khi đó, Entity [`LlmCall.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Conversations/LlmCall.cs) ở tầng Domain lúc khởi tạo phương thức `Create()` chưa gán giá trị mặc định cho `Provider` và `FinishReason`. Hậu quả là toàn bộ transaction bị rollback và luồng gửi tin nhắn [`telegramAdapter.SendTextMessageAsync`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Telegram/TelegramAdapter.cs) không được thực thi.

### Giải pháp khắc phục:
- Cập nhật [`LlmCall.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Conversations/LlmCall.cs):
  - Thuộc tính `Provider` luôn mặc định `"deepseek"`.
  - Thuộc tính `FinishReason` luôn mặc định `"stop"` (hoặc giá trị trả về từ LLM API).
  - Constructor và phương thức `LlmCall.Create()` nhận đầy đủ các trường đo lường mở rộng.
- Cập nhật [`AgentLoop.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Services/AgentLoop.cs): Truyền tường minh `provider: "deepseek"`, `finishReason: completion.FinishReason ?? "stop"`.
- Bổ sung Store cấu hình động [`LlmSettingsStore.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Llm/LlmSettingsStore.cs) và cập nhật [`DeepSeekLlmClient.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Llm/DeepSeekLlmClient.cs) hỗ trợ cả `deepseek-chat` (chuyên trách Function Calling) và `deepseek-reasoner` (chuyên trách suy luận chuỗi tư duy CoT).

---

## 🏗️ 3. Kiến Trúc Bộ Điều Khiển & Phân Hệ Quản Trị

### 3.1. Phân Hệ Giám Sát Hội Thoại (`ConversationsController.cs`)
Triển khai bộ endpoint tại [`ConversationsController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/ConversationsController.cs):
- `GET /api/v1/conversations`: Liệt kê danh sách các phiên trò chuyện từ Web và Telegram, sắp xếp theo tin nhắn mới nhất, kèm bộ đếm số tin nhắn (`MessagesCount`) và số lượt gọi AI (`LlmCallsCount`).
- `GET /api/v1/conversations/{id}`: Trả về toàn bộ dòng thời gian tin nhắn của phiên chat (User, Assistant, Tool Call, System).
- `GET /api/v1/conversations/{id}/audit`: Trả về chi tiết kiểm toán từng cuộc gọi LLM (Token tiêu thụ, Chi phí USD, Độ trễ mili-giây, Danh sách các công cụ đã được AI triệu hồi).

### 3.2. Phân Hệ Cấu Hình LLM Gateway (`LlmSettingController.cs`)
Triển khai bộ endpoint tại [`LlmSettingController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/LlmSettingController.cs):
- `GET /api/v1/llm-setting`: Lấy thông tin cấu hình runtime kèm Masked API Key bảo mật (ví dụ: `sk-9••••••••e06a`).
- `POST /api/v1/llm-setting`: Lưu API Key mới, chuyển đổi Model (`deepseek-reasoner` / `deepseek-chat`), địa chỉ Base URL và Nhiệt độ (`Temperature`).
- `POST /api/v1/llm-setting/test`: Gửi gói tin ping trực tiếp đến máy chủ DeepSeek API và đo đạc độ trễ mạng thực tế (kết quả đạt **374 ms 100% OK**).

---

## 🎨 4. Hiện Thực Hóa Frontend SPA (3 Màn Hình Quản Trị Độc Lập)

1. **Giao Diện Giám Sát Hội Thoại ([`AdminConversationsPage.tsx`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminConversationsPage.tsx))**:
   - Truy cập tại `/admin/conversations`.
   - Cột bên trái: Danh sách các phiên hội thoại kèm badge kênh (`Web` / `Telegram`), trạng thái (`Active`, `Human Active`, `Resolved`) và thời gian cập nhật.
   - Cột bên phải:
     - **Live Message Stream**: Hiển thị bong bóng chat sinh động giữa khách hàng và AI Agent; hiển thị thẻ khối màu tím riêng biệt cho các bước AI gọi Tool (kèm tham số đầu vào và kết quả trả về của hệ thống).
     - **Telemetry Inspector Panel**: Bảng thống kê tổng Token, Chi phí ước tính (USD), Độ trễ trung bình (ms) và nhật ký các lần LLM Call.

2. **Giao Diện Kênh Telegram ([`AdminTelegramPage.tsx`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminTelegramPage.tsx))**:
   - Truy cập tại `/admin/channels/telegram`.
   - Quản trị Bot Token (kiểm tra trạng thái `@TaiGoClawBot`), kích hoạt/hủy Webhook URL công khai, cấu hình `Admin Chat ID` (5809306571) và nút bấm phát tín hiệu kiểm thử duyệt deal đoàn với Inline Keyboard `[DUYỆT DEAL] / [TỪ CHỐI]`.

3. **Giao Diện Cấu Hình LLM Gateway ([`AdminLlmSettingsPage.tsx`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminLlmSettingsPage.tsx))**:
   - Truy cập tại `/admin/settings/llm`.
   - Cung cấp form nhập liệu an toàn (có nút mắt toggle che/hiện key), thanh trượt nhiệt độ và Hộp đen Console Chẩn đoán đo kiểm tra độ trễ mạng trực tiếp tới máy chủ DeepSeek.

---

## 🌟 5. Giá Trị Trình Bày Khi Phỏng Vấn (Talking Points for Interviewers)

1. *"Khi tích hợp LLM vào ứng dụng doanh nghiệp, làm thế nào bạn kiểm soát chi phí (FinOps) và ngăn chặn việc token bị tiêu thụ vô tội vạ?"*  
   ➔ **Trả lời**: *"Tôi thiết kế cơ chế kiểm toán chi tiết đến từng turn suy luận. Mỗi khi AI phản hồi, tầng Infrastructure bóc tách trường `usage` từ response (Prompt Tokens, Completion Tokens), tính toán chi phí quy đổi ra USD theo đơn giá của nhà cung cấp và đo độ trễ mạng (Latency ms). Tất cả được lưu tự động vào bảng `llm_calls` liên kết với phiên hội thoại, giúp đội ngũ vận hành theo dõi trực tiếp trên dashboard và phát hiện ngay những phiên chat bất thường."*

2. *"Bạn xử lý như thế nào khi nhận Webhook từ bên thứ ba (Telegram) cần thời gian xử lý AI kéo dài 3-5 giây mà không bị timeout?"*  
   ➔ **Trả lời**: *"Tôi áp dụng mô hình 'Zero-latency Acknowledgment'. Endpoint Webhook trong Controller chỉ thực hiện đúng 2 nhiệm vụ: Parse dữ liệu cơ bản và đẩy Message vào In-Process Channel (`System.Threading.Channels`), sau đó trả về `HTTP 200 OK` ngay lập tức cho Telegram trong vòng < 20ms. Một `BackgroundService` chạy ngầm độc lập sẽ lắng nghe Channel đó để gọi chu trình AI `AgentLoop` và gửi tin nhắn phản hồi về chat room thông qua Telegram Bot API."*

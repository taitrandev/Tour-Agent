# 💬 PROMPTS.md — Bộ Câu Lệnh Chuẩn Cho Session Mới (.NET 10 & UI Frontend)

> **Mục đích:** File này chứa các mẫu Prompt chuẩn đã được tinh chỉnh tối ưu.  
> Mỗi khi **mở chat session mới trên IDE**, bạn chỉ cần **copy & paste** một trong các prompt dưới đây để AI tiếp quản dự án ngay lập tức trong < 30 giây mà không bị mất ngữ cảnh hay hallucination.

---

## 🚀 1. PROMPT KHỞI ĐỘNG SESSION MỚI — GIAI ĐOẠN UI FRONTEND (Copy Paste Vào Chat Mới)

> 📋 **Khi nào dùng:** Dùng ngay khi bạn tạo **New Chat Session** để bắt đầu lập kế hoạch và triển khai giao diện Frontend (Client & Admin) kết nối với Backend .NET 10.

```markdown
Chào bạn, tôi đang tiếp tục phát triển dự án "AI Agent Travel" — Chuyển sang Giai đoạn UI Frontend (Phase 6).

## 📌 Bối cảnh dự án
Dự án đã hoàn thành trọn vẹn 5 giai đoạn Backend trên nền tảng .NET 10 (Clean Architecture):
- **Phase 1-3**: Scaffolding 4 layers, Domain Modeling (Tour, Departure, Conversation, Message, LlmCall), EF Core 10 PostgreSQL (tour_agent_db), Vertical Slice SearchTours. (41/41 unit & architecture tests 100% green).
- **Phase 4**: Core AI Agent Engine (ILlmClient DeepSeek V3, 5 Tools tra cứu thực tế, AgentLoop tự hành đa lượt tối đa 5 turns, Controller sync & SSE Streaming).
- **Phase 5**: Telegram Webhook Omnichannel (System.Threading.Channels in-process queue, BackgroundService phi tập trung, TelegramAdapter).

Backend được thiết kế chuẩn REST API thuần (Controllers, không dùng Razor/MVC Views).  
Frontend sẽ được xây dựng tách biệt hoàn toàn dưới dạng Single Page Application (SPA).

## 🎯 Yêu cầu cho AI
Hãy thực hiện các bước sau để nắm toàn bộ ngữ cảnh trước khi tiến hành code:

### Bước 1: Đọc nhanh các tài liệu theo thứ tự ưu tiên
1. `dotnet-backend-core-main/docs/PROGRESS.md` — Trạng thái tiến độ & các quyết định kiến trúc đã chốt.
2. `dotnet-backend-core-main/docs/UI_PLAN.md` — Blueprint quy hoạch giao diện Frontend (Layout, Components, Tech Stack).
3. `dotnet-backend-core-main/docs/API_ENDPOINTS.md` — Danh mục đầy đủ các REST & SSE endpoints backend cung cấp.
4. `dotnet-backend-core-main/docs/ARCHITECTURE.md` — Kiến trúc Clean Architecture 4 layers của backend.
5. `dotnet-project-knowledge/DATABASE_SCHEMA.md` — Cấu trúc 28 bảng dữ liệu PostgreSQL để hiểu model dữ liệu.
6. `dotnet-project-knowledge/AI_CONTEXT.md` — Quy tắc dự án (Project Rules) & Mục tiêu kinh doanh.

### Bước 2: Phân tích UI Layout
- Tham khảo layout mẫu Dark Glassmorphism từ giao diện quản trị cũ (Sidebar + Dashboard + Data Table + AI Drawer).
- Giữ vững tư duy phân tách: Client Tour Tra cứu & AI Consultant Chat Widget (dành cho du khách) vs Admin Dashboard & AI Audit Log (dành cho quản trị viên).

### Bước 3: Đề xuất kế hoạch triển khai Frontend chi tiết
1. Thống nhất Tech Stack cho Frontend (Vite + React / TypeScript hoặc Vanilla CSS / Modern UI).
2. Xây dựng Component Hierarchy & Mock Data State.
3. Kế hoạch kết nối SSE Streaming (`/api/v1/agent/chat/stream`) với hiệu ứng gõ phím và hiển thị tool badges.
4. Đề xuất các API bổ sung trên backend nếu cần (CRUD tour, lấy danh sách hội thoại).

## ⚠️ Lưu ý quan trọng
- Tạm thời SKIP toàn bộ phần Zalo (chỉ tập trung Web Client và Telegram đã có).
- Mục tiêu dự án: Demo phỏng vấn Senior/Lead — Code clean, giao diện Dark Glassmorphism sang trọng, ấn tượng.
- Giữ đúng quy tắc: Không dùng MediatR, không dùng AutoMapper.
```

---

## 🛠️ 2. PROMPT TIẾP TỤC PHÁT TRIỂN / MỞ RỘNG BACKEND .NET 10

> 📋 **Khi nào dùng:** Khi bạn muốn AI tập trung bổ sung thêm một Slice/Feature mới cho Backend C#.

```markdown
Chào bạn, tôi muốn bổ sung thêm Feature mới cho Backend .NET 10: "[TÊN FEATURE, ví dụ: CRUD Tour Management / Lấy Lịch Sử Hội Thoại]".

Yêu cầu thực hiện:
1. Đọc `dotnet-backend-core-main/docs/ARCHITECTURE.md` và `dotnet-backend-core-main/docs/API_ENDPOINTS.md`.
2. Tuân thủ tuyệt đối cấu trúc Feature Folder trong `TourAgent.Application/Features/[FeatureName]/`:
   - Tạo Command / Query Record (triển khai `ICommand<Result<T>>` hoặc `IQuery<Result<T>>`).
   - Tạo Response DTO kèm hàm ánh xạ tĩnh `FromEntity` (Nói KHÔNG với AutoMapper).
   - Tạo Handler (triển khai `ICommandHandler` hoặc `IQueryHandler`) inject trực tiếp vào Controller (Nói KHÔNG với MediatR).
   - Viết FluentValidation Validator kế thừa `AbstractValidator<T>`.
3. Tạo Endpoint tương ứng trong Controller kế thừa `ControllerBase` với XML docblock đầy đủ.
4. Viết Unit Tests kiểm thử các kịch bản Success và Failure.
5. Chạy `dotnet test` đảm bảo 100% tests PASSED và kiến trúc layer boundary không bị vi phạm.
```

---

## 🏁 3. PROMPT KẾT THÚC SESSION (Wrap-Up & Cập Nhật Context)

> 📋 **Khi nào dùng:** Khi bạn chuẩn bị kết thúc phiên làm việc để bảo toàn ngữ cảnh cho lần sau.

```markdown
Phiên làm việc này sắp kết thúc. Hãy giúp tôi tổng kết và đóng gói dự án:

1. Cập nhật file `dotnet-backend-core-main/docs/PROGRESS.md`:
   - Đánh dấu các bước đã hoàn thành sang `✅ DONE`.
   - Cập nhật mục `Bước tiếp theo`.
   - Ghi lại nhật ký công việc vào phần `Work Log`.
2. Cập nhật `dotnet-backend-core-main/docs/API_ENDPOINTS.md` nếu phiên này có thêm hoặc sửa endpoint nào.
3. Nếu phát hiện bài học kinh nghiệm hoặc kỹ thuật đáng nhớ, hãy ghi vào `dotnet-project-knowledge/LESSONS_LEARNED.md`.
4. Báo cáo ngắn gọn 3 điểm:
   - Những việc ĐÃ HOÀN THÀNH.
   - Trạng thái kiểm thử / mã nguồn hiện tại.
   - Việc CẦN LÀM ĐẦU TIÊN trong session kế tiếp.
```

---

## 🧑‍💻 4. PROMPT HỎI ĐÁP PHỎNG VẤN & GIẢI TRÌNH KIẾN TRÚC (.NET Migration Journey)

> 📋 **Khi nào dùng:** Khi bạn muốn chuẩn bị trả lời các câu hỏi phỏng vấn hóc búa về dự án di trú từ PHP sang .NET 10.

```markdown
Hãy đóng vai trò là Senior .NET Solution Architect đóng vai người phỏng vấn tôi về dự án "AI Agent Travel":
1. Dựa trên tài liệu `dotnet-backend-core-main/docs/journey/` (8 chapters), hãy chất vấn tôi về các trade-off kỹ thuật:
   - Tại sao lại chọn .NET 10 thay vì tiếp tục với PHP 8.4 Laravel?
   - Tại sao từ chối MediatR và AutoMapper dù cộng đồng .NET dùng rất phổ biến?
   - Làm thế nào kiểm soát Hallucination số liệu tour khi AI tự động tư vấn giá?
   - Cơ chế Zero-latency Ack qua System.Threading.Channels giải quyết bài toán gì của Telegram Webhook?
   - Tầng Domain bảo vệ Invariants và Encapsulation như thế nào?
2. Giúp tôi đưa ra câu trả lời thuyết phục, gãy gọn, thể hiện tư duy Senior Engineer.
```

# PROGRESS — Trạng Thái Tiến Độ Dự Án TourAgent

> **File này lưu trữ trạng thái hiện tại và tiến độ công việc giữa các session làm việc.**
> 📖 **Hồ sơ hành trình phỏng vấn & phản biện kỹ thuật**: Xem chi tiết tại [MIGRATION JOURNEY](journey/README.md).

---

## 📌 Trạng Thái Hiện Tại
- **Giai đoạn hiện tại**: **Hoàn thành Giai đoạn 5 (Telegram Webhook Controller, Channels Queue, Background Worker & Telegram Adapter)**.
- **Trạng thái Test**: **41/41 tests PASSED** (24 Domain Tests + 14 Application Tests + 3 Architecture Tests).
- **Cơ sở dữ liệu**: Đã clone thành công database `tour_agent_db` trên PostgreSQL Docker (Port 5432) với đầy đủ 28 tables và dữ liệu tour thực tế.
- **Bước tiếp theo**: **Giai đoạn 6: UI Planning & Frontend Implementation (Tách biệt SPA, Dark Glassmorphism, SSE Chat & Admin)**.

---

## 🎯 Quyết Định Đã Chốt (Architecture Decisions)
- **Framework**: .NET 10 (LTS/Current SDK: 10.0.401).
- **Kiến trúc**: Clean Architecture 4 Layers (`Domain`, `Application`, `Infrastructure`, `Api`) + Feature Folders trong Application.
- **Database**: PostgreSQL 16 (Docker: `tour_agent_db`) qua EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Mapping**: Manual Extension Methods (Nói KHÔNG với AutoMapper vì rủi ro license và hiệu năng).
- **Use Cases**: Explicit Command/Query Handlers inject trực tiếp vào Controllers (Nói KHÔNG với MediatR).
- **Persistence**: EF Core 10 qua `IAppDbContext` (Không dùng Generic Repository riêng cho từng entity).
- **AI Integration**: `ILlmClient` + DeepSeek V3 (OpenAI Function Calling protocol).
- **Omnichannel Queue**: `System.Threading.Channels` + `BackgroundService` xử lý bất đồng bộ Zero-latency.
- **Architecture Enforcement**: `NetArchTest.Rules` trong `tests/TourAgent.ArchitectureTests`.
- **Frontend Strategy**: SPA độc lập hoàn toàn với backend C# (REST API + SSE Streaming).

---

## 📋 Bảng Theo Dõi Các Bước (Roadmap)

| Bước | Mô tả nội dung | Trạng thái | Ghi chú |
| :---: | :--- | :---: | :--- |
| **1.1** | Scaffold Solution `TourAgent.sln` + 4 src projects + 3 test projects bằng `dotnet` CLI | ✅ DONE | .NET 10 |
| **1.2** | Cấu hình `Directory.Build.props` (Nullable, ImplicitUsings, TreatWarningsAsErrors) | ✅ DONE | |
| **1.3** | Cài đặt `NetArchTest.Rules` & viết bộ 3 tests bảo vệ layer boundaries | ✅ DONE | 3/3 passed |
| **1.4** | Thiết lập file luật `AGENTS.md` và `docs/PROGRESS.md` | ✅ DONE | |
| **2.1** | `Domain/Common`: `Entity<T>`, `Result<T>`, `Error` | ✅ DONE | Đã hoàn thành & test |
| **2.2** | `Domain/Tours`: Entity `Tour`, `TourDeparture` với Invariant checks + Unit Tests | ✅ DONE | 24 tests passed |
| **2.3** | `Domain/Conversations`: Entity `Conversation`, `Message`, `LlmCall` | ✅ DONE | Đã hoàn thành & test |
| **3.1** | `Infrastructure/Persistence`: `AppDbContext`, Entity Configurations, PostgreSQL `tour_agent_db` | ✅ DONE | Npgsql 10.0.3 |
| **3.2** | Dựng Slice mẫu `Features/Tours/SearchTours` (Khuôn mẫu chuẩn cho các slice sau) + Tests | ✅ DONE | 4/4 tests passed |
| **4.1** | `ILlmClient` + Implementation qua DeepSeek V3 + FakeLlmClient cho Unit Tests | ✅ DONE | Trừu tượng hóa vendor-neutral |
| **4.2** | Triển khai 5 Agent Tools tra cứu tour & chính sách | ✅ DONE | Search, Departures, Itinerary, Policies, Escalation |
| **4.3** | `AgentLoop` Orchestrator điều phối tool calling (Max 5 turns, Audit LlmCall) | ✅ DONE | 3/3 agent loop tests passed |
| **4.4** | Controllers `POST /api/v1/agent/chat` & `POST /api/v1/agent/chat/stream` (SSE) | ✅ DONE | Swagger UI & Streaming |
| **5.1** | `TelegramWebhookController` + `System.Threading.Channels` In-Process Queue | ✅ DONE | Zero-latency Ack |
| **5.2** | `TelegramWebhookBackgroundService` + `TelegramAdapter` gửi phản hồi tự động | ✅ DONE | Idempotency + Fallback Markdown |
| **6.1** | Lập Blueprint `UI_PLAN.md`, cấu hình CORS Policy trên Backend .NET 10 | ✅ DONE | `SpaCorsPolicy` kích hoạt |
| **6.2** | Khởi tạo dự án Frontend SPA (`tour-agent-frontend`) Vite + React 19 + TypeScript | ✅ DONE | Tailwind CSS v4, Lucide |
| **6.3** | Tích hợp Client Portal & AI Consultant Widget với SSE Streaming + Tool Badges | ✅ DONE | `/api/v1/agent/chat/stream` |
| **6.4** | Tách Route 1-1 theo dự án PHP: `/` (Landing), `/admin`, `/admin/conversations`, `/admin/escalations`, `/admin/channels/telegram` | ✅ DONE | Replicate 100% PHP UI |

---

## 📝 Nhật Ký Hoạt Động (Work Log)
- **2026-09-27**:
  - Dựng khung `TourAgent.sln` (.NET 10) với 4 source projects và 3 test projects.
  - Thiết lập `Directory.Build.props` bật `TreatWarningsAsErrors`.
  - Tạo `AssemblyMarker.cs` cho cả 3 tầng `Domain`, `Application`, `Infrastructure`.
  - Viết `LayerTests.cs` kiểm tra tính toàn vẹn phụ thuộc giữa các tầng, chạy `dotnet test` đạt 3/3 tests PASSED.
  - Hoàn thành Phase 2: Domain Entities (`Tour`, `TourDeparture`, `Conversation`, `Message`, `LlmCall`) + 24 unit tests.
  - Hoàn thành Phase 3: EF Core 10 PostgreSQL (`tour_agent_db`) + Vertical Slice `SearchTours`.
  - Hoàn thành Phase 4: `ILlmClient` DeepSeek V3, 5 Tools, `AgentLoop`, Controllers HTTP sync & SSE streaming.
  - Hoàn thành Phase 5: `TelegramWebhookController` + `System.Threading.Channels` In-Process Queue + `TelegramAdapter`.
  - Đạt tổng **41/41 tests PASSED**.
  - Hoàn thành Phase 6 (UI Frontend SPA):
    - Cấu hình CORS Policy trên Backend [Program.cs](src/TourAgent.Api/Program.cs).
    - Khởi tạo Single Page Application độc lập `tour-agent-frontend/` (Vite, React 19, TypeScript, Tailwind CSS v4).
    - Thiết kế hệ thống Design Tokens Dark Glassmorphism Luxury (`#070a12`, Frosted Glass, Electric Cyan, Emerald Gold, Cormorant Garamond / Plus Jakarta Sans).
    - Xây dựng Client Travel Portal: Hero Search Bar đa tiêu chí, Grid Thẻ Tour, Modal Lịch Trình Chi Tiết & 3 Lớp Cam Kết Godfather.
    - Xây dựng Floating AI Consultant Widget với kết nối Server-Sent Events (SSE) `/api/v1/agent/chat/stream`, hiển thị Tool Badges thời gian thực, hiệu ứng gõ phím typewriter và Markdown rendering.
    - Xây dựng Admin Operations Desk: Thống kê tổng quan Metrics (Tours, LLM Tokens, Chi phí USD), Quản lý Tour, Giám sát hội thoại khách và Trạng thái Telegram Webhook Bot.
    - Build thành công bản Production Bundle (`tsc -b && vite build`) đạt 100% không lỗi.

# 🤖 AI_CONTEXT.md — AI Agent Travel System (.NET 10 Backend Core)

> **Đọc file này trước khi làm bất kỳ điều gì trong dự án này.**
> File này là tài liệu ổn định (ít biến động) được thiết kế để AI session mới có thể nắm được ngữ cảnh, kiến trúc hệ thống, tech stack, các rules, danh mục modules hiện tại của dự án trong < 2 phút.

---

## 📌 Tổng Quan Dự Án

| Trường | Nội dung |
| :--- | :--- |
| **Tên dự án** | **AI Agent Travel** (Hệ Thống Du Lịch 4 Chạm Thảnh Thơi — AI-Human Hybrid Travel Protocol) |
| **Mục tiêu kinh doanh** | Biến "Lời đề nghị không thể từ chối" trong `05-offer.md` thành **hệ sinh thái sản phẩm & hệ thống bán hàng thực tế**: Bán gói Combo 3N2Đ / 6N5Đ chuẩn gu cho Dân văn phòng 24–35 tuổi, an tâm 100% với Hợp Đồng Doanh Nghiệp & 3 Lớp Cam Kết Godfather. |
| **Mô hình kiến trúc** | **Hệ thống 5 Trụ Cột Kinh Doanh**: `1. Offer (Đóng gói)` ➔ `2. Attraction (Thu hút)` ➔ `3. Conversion (Chuyển đổi)` ➔ `4. Delivery (Bàn giao)` ➔ `5. Operation (Vận hành)`. |
| **Tư duy cốt lõi** | **Phân tách Sản Phẩm (Product) & Lời Đề Nghị (Offer)**: `1 Product ──< N Offers` (Sản phẩm chứa lịch trình, kỹ thuật, FAQ; Offer chứa chiến lược giá, quà tặng Value Stack, tính khan hiếm và cam kết). |
| **GitHub Repo** | `https://github.com/taitrandev/Agent-Travel.git` (Branch: `main`) |
| **Backend Core** | **.NET 10 / C# 13 Clean Architecture** (trong thư mục `dotnet-backend-core-main/`) với 4 lớp: `Domain`, `Application`, `Infrastructure`, `Api` |
| **LLM Driver** | **DeepSeek API** (`deepseek-chat` / DeepSeek-V3) với OpenAI-compatible Function Calling + Multi-Turn Autonomous Agent Loop |
| **Môi trường & DB** | .NET 10 SDK (Windows / Linux) • Database: **PostgreSQL 16** (`tour_agent_db` trên Docker port 5432) qua EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| **Tiêu chuẩn Code** | C# 13 Strict, Nullable enabled, TreatWarningsAsErrors, NetArchTest (Layer Boundary Enforcement), 41/41 Unit/Architecture tests passed (100% green) |
| **Frontend Strategy** | Kiến trúc tách rời hoàn toàn (Decoupled SPA / Vite + React), tiêu thụ REST API & SSE Streaming từ .NET Backend, giao diện Dark Glassmorphism chuẩn demo phỏng vấn |
| **Tài liệu cốt lõi** | `DATABASE_SCHEMA.md` (Từ điển CSDL & Mermaid ERD) • `dotnet-backend-core-main/docs/ARCHITECTURE.md` (Chi tiết 4 layers) • `dotnet-backend-core-main/docs/PROGRESS.md` (Roadmap & Status) • `dotnet-backend-core-main/docs/API_ENDPOINTS.md` (Đặc tả REST & SSE) • `LESSONS_LEARNED.md` & `LESSONS_LEARNED_AGENT.md` |
| **Quản lý Tiến độ** | `dotnet-backend-core-main/docs/PROGRESS.md` (Roadmap các giai đoạn từ Phase 1 đến Phase 6+) |
| **Bộ Prompt Mẫu** | `PROMPTS.md` (Bộ câu lệnh chuẩn để khởi động session mới hiệu quả cao) |

---

## 🗺️ Bản Đồ 5 Trụ Cột Triển Khai (The 5 Business Pillars)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   HỆ THỐNG 5 TRỤ CỘT AI AGENT TRAVEL                   │
├───────────────────┬───────────────────┬────────────────────────────────┤
│ Trụ Cột Kinh Doanh │ Vai Trò Thực Tế   │ Phân Hệ Tương Ứng Trong .NET   │
├───────────────────┼───────────────────┼────────────────────────────────┤
│ 1. OFFER          │ Lời đề nghị đỉnh  │ `Domain/Tours` & `Offers`      │
│    (Đóng gói)     │ cao không từ chối │ (Tour Base, Departures, Stack) │
├───────────────────┼───────────────────┼────────────────────────────────┤
│ 2. ATTRACTION     │ Thu hút traffic,  │ Marketing & Content Channels   │
│    (Thu hút)      │ khách hàng tiềm   │ (Ad Copy, Landing Page Hooks)  │
├───────────────────┼───────────────────┼────────────────────────────────┤
│ 3. CONVERSION     │ Biến khách quan   │ `TourAgent.Api/Controllers`    │
│    (Chuyển đổi)   │ tâm thành tiền cọc│ + `AgentLoop` (Chat 24/7 & SSE)│
├───────────────────┼───────────────────┼────────────────────────────────┤
│ 4. DELIVERY       │ Giao hàng & Trải  │ Channels Integration           │
│    (Bàn Giao)     │ nghiệm khách VIP  │ (Telegram Webhook & Adapter)   │
├───────────────────┼───────────────────┼────────────────────────────────┤
│ 5. OPERATION      │ Vận hành tự động, │ Admin Management Dashboard     │
│    (Vận Hành)     │ kiểm soát hệ thống│ + Audit Logs (`llm_calls`)     │
└───────────────────┴───────────────────┴────────────────────────────────┘
```

---

## 📜 Project Rules (Quy Tắc Bất Di Bất Dịch)

1. **Code Style Guide**:
   - Luôn viết docblock/comment tiếng Việt giải thích mục đích cho mỗi method/hàm/class/record.
   - Sử dụng strict C# features: `record`, `readonly`, `sealed`, pattern matching, primary constructors khi phù hợp.
   - Đặt tên biến rõ ràng, dễ hiểu, theo chuẩn convention C# / .NET (PascalCase cho class/method/property, camelCase cho parameter/local variable).
2. **Kiến Trúc Clean Architecture (.NET 10)**:
   - **Domain**: Chứa Entities, Value Objects, Domain Errors, Invariant Checks. KHÔNG phụ thuộc vào bất kỳ thư viện ngoài hay framework nào.
   - **Application**: Feature-based slices (`Features/Tours/...`), Command/Query Handlers rõ ràng (`IQueryHandler`, `ICommandHandler`), Interfaces abstraction (`IAgentLoop`, `ILlmClient`, `IAppDbContext`).
   - **Infrastructure**: Triển khai cụ thể: EF Core 10 (`AppDbContext`), `DeepSeekLlmClient`, `TelegramAdapter`.
   - **Api**: ASP.NET Core Web API Controllers, Dependency Injection Registration, Middleware, SSE Streaming endpoints.
   - **Kiểm soát phụ thuộc**: `NetArchTest.Rules` bảo vệ layer boundaries nghiêm ngặt.
   - **Nói KHÔNG với MediatR & AutoMapper**: Do vấn đề bản quyền thương mại và overhead; dùng explicit DI handlers và manual mapping methods (`TourSummaryDto.FromEntity`).
3. **Quy Tắc Sửa Code (Minimal Surgical Edits)**:
   - Khi chỉnh sửa file hiện có, LUÔN dùng `replace_file_content` hoặc `multi_replace_file_content` để sửa đúng những dòng cần thiết.
   - TUYỆT ĐỐI KHÔNG ghi đè toàn bộ file (`write_to_file`) khi file đã tồn tại, nhằm giữ Git Diff sạch và an toàn cho review.
4. **Implementation Intentions Rules (Tự Động Kích Hoạt Khi Thay Đổi)**:
   - 🗄️ **Trigger DB**: Bất cứ khi nào tạo/sửa EF Core migration, thay đổi bảng, thêm cột ➔ BẮT BUỘC cập nhật ngay `dotnet-project-knowledge/DATABASE_SCHEMA.md`.
   - 📋 **Trigger Roadmap**: Khi hoàn thành hoặc thay đổi phạm vi giai đoạn ➔ BẮT BUỘC cập nhật `dotnet-backend-core-main/docs/PROGRESS.md`.
   - 🤖 **Trigger Agent**: Khi thay đổi logic `AgentLoop`, `ILlmClient`, Agent Tools hoặc System Prompts ➔ BẮT BUỘC cập nhật `dotnet-project-knowledge/LESSONS_LEARNED_AGENT.md` và `API_ENDPOINTS.md`.

---

## 🗂️ Cấu Trúc File & Dự Án Hiện Tại

```
AI-Agent-Travel/
├── dotnet-project-knowledge/                 # 📚 Knowledge Hub chính xác cho .NET Core
│   ├── AI_CONTEXT.md                         # File này (Context sống cho AI)
│   ├── DATABASE_SCHEMA.md                    # Từ điển CSDL 28 bảng PostgreSQL & Mermaid ERD
│   ├── LESSONS_LEARNED.md                    # Đúc kết kinh nghiệm kỹ thuật
│   ├── LESSONS_LEARNED_AGENT.md              # Đúc kết chuyên sâu về Autonomous AI Agent
│   ├── TECHNICAL_NOTES.md                    # Ghi chú kỹ thuật runtime & tokens
│   └── PROMPTS.md                            # Bộ câu lệnh mẫu chuẩn (cho session mới)
│
├── dotnet-backend-core-main/                 # 🏗️ Backend Core C# / .NET 10
│   ├── TourAgent.sln                         # Solution file
│   ├── Directory.Build.props                 # Cấu hình TreatWarningsAsErrors, Nullable
│   ├── docs/
│   │   ├── PROGRESS.md                       # Tiến độ chi tiết các giai đoạn
│   │   ├── ARCHITECTURE.md                   # Đặc tả Clean Architecture 4 Layers
│   │   ├── API_ENDPOINTS.md                  # Danh mục & chi tiết các REST / SSE endpoints
│   │   ├── UI_PLAN.md                        # Blueprint kế hoạch xây dựng Frontend
│   │   └── journey/                          # Hồ sơ hành trình di trú kỹ thuật
│   │       ├── README.md                     # Mục lục 8 chương
│   │       ├── 01..07-*.md                   # Nhật ký chi tiết các giai đoạn 1 -> 5
│   │       └── 08-phase-6-ui-planning.md     # Kế hoạch UI Frontend
│   ├── src/
│   │   ├── TourAgent.Domain/                 # Entities: Tour, TourDeparture, Conversation, Message, LlmCall
│   │   ├── TourAgent.Application/            # Slices: Features/Tours/SearchTours, Agent Loop & Tools
│   │   ├── TourAgent.Infrastructure/         # EF Core AppDbContext, DeepSeekLlmClient, TelegramAdapter
│   │   └── TourAgent.Api/                    # Controllers: AgentController, ToursController, TelegramWebhookController
│   └── tests/
│       ├── TourAgent.Domain.UnitTests/       # 24 Unit tests cho Invariants & Business Logic
│       ├── TourAgent.Application.UnitTests/  # 14 Unit tests cho Queries & Agent Loop
│       └── TourAgent.ArchitectureTests/      # 3 Architecture tests (NetArchTest.Rules)
│
└── kp3-backend-core-main/                    # 🗄️ PHP Legacy (CHỈ dùng tham khảo UI Layout, không sửa)
```

---

## 🔄 Trạng Thái Đã Đạt Được (Current Milestones)

- **Backend .NET 10 & Frontend SPA hoàn thành 100% các Phase cốt lõi (1 - 6)**:
  1. **Phase 1: Setup & Scaffolding**: Khởi tạo Clean Architecture 4 layers, .NET 10 SDK, cấu hình CI/CD rule, `NetArchTest` bảo vệ boundaries.
  2. **Phase 2: Domain Modeling**: Xây dựng Entities vững chắc với encapsulation, invariants check, domain errors, 24 unit tests đạt chuẩn.
  3. **Phase 3: Persistence & Slice**: EF Core 10 kết nối PostgreSQL `tour_agent_db` trên Docker (5432). Hoàn thành vertical slice `SearchTours` hoàn chỉnh không dùng MediatR / AutoMapper.
  4. **Phase 4: AI Agent Engine**: Thiết kế `ILlmClient` (DeepSeek V3 OpenAI-compatible), 5 Agent Tools tra cứu dữ liệu thực, `AgentLoop` tự hành đa lượt (max 5 turns, audit token/chi phí), Controllers hỗ trợ cả HTTP Sync và SSE Streaming.
  5. **Phase 5: Telegram Omnichannel**: Triển khai `TelegramWebhookController` bất đồng bộ với `System.Threading.Channels` In-Process Queue, `BackgroundService` tiêu thụ thông điệp không độ trễ, `TelegramAdapter` gửi markdown thông minh.
  6. **Phase 6: UI Frontend SPA**: Hoàn thiện Single Page Application độc lập `tour-agent-frontend` (Vite, React 19, TypeScript, Tailwind CSS v4) chuẩn Dark Glassmorphism Luxury, tích hợp Client Catalog, AI Consultant SSE Streaming Widget với Tool Execution Badges, và Admin Operations Desk.
  7. **Chất lượng kiểm thử**: **41/41 tests PASSED** tuyệt đối trên `dotnet test`, frontend build 100% sạch không lỗi type hay lint.

---

## 🎯 Giai Đoạn Tiếp Theo (Next Focus: Phase 7 — Live End-to-End Demo & Video Walkthrough)

- Khởi chạy đồng thời Backend .NET 10 (`http://localhost:5000`) và Frontend Vite (`http://localhost:5173`).
- Thực hiện phiên test tương tác trực tiếp: Khách hàng hỏi AI về tour qua widget, quan sát SSE streaming và tool calling badges hoạt động trực tiếp.
- Kiểm tra bảng Admin Operations Desk giám sát chi phí token và trạng thái kênh Telegram Bot.

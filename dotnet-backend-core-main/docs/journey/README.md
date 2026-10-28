# 🗺️ Nhật Ký Chuyển Đổi Dự Án: AI Agent Travel (PHP ➔ .NET 10)
## *From PHP 8.4 / Laravel 12 to Modern .NET 10 Clean Architecture*

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**
> Được thiết kế đặc biệt nhằm trình bày tư duy kiến trúc, năng lực phân tích phản biện và khả năng làm chủ công nghệ C# .NET 10 sau 15 tháng gián đoạn để phục vụ phỏng vấn tuyển dụng vị trí Senior / Lead .NET Developer.

---

## 📖 Mục Lục Các Kỳ Nhật Ký

Hành trình được chia thành 11 chuyên đề chi tiết có liên kết chéo (bấm vào từng kỳ để xem phân tích chuyên sâu):

| Kỳ | Chủ Đề Chi Tiết | Trọng Tâm Nhận Thức & Tư Duy Kỹ Thuật |
| :---: | :--- | :--- |
| **[Kỳ 1](01-evaluation-php-vs-dotnet.md)** | **[Đánh Giá Công Nghệ: PHP 8.4 vs .NET 10](01-evaluation-php-vs-dotnet.md)** | Động lực chuyển đổi, câu hỏi phản biện ban đầu, bảng ánh xạ tính năng cốt lõi, ưu/nhược điểm và trade-off hiệu năng/concurrency. |
| **[Kỳ 2](02-architectural-decisions-and-licensing.md)** | **[Bản Quyền Thư Viện & Định Vị Kiến Trúc](02-architectural-decisions-and-licensing.md)** | Nhận thức rủi ro bản quyền của MediatR & AutoMapper; thay thế bằng Direct Handlers và Manual Mapping; Clean Architecture cho phân khúc SME; tách rời BE và FE. |
| **[Kỳ 3](03-phase-1-scaffolding-and-architecture-tests.md)** | **[Giai Đoạn 1: Dựng Khung & Architecture Tests](03-phase-1-scaffolding-and-architecture-tests.md)** | Dựng Solution bằng CLI (không dùng template ma thuật), cấu hình `Directory.Build.props` (Zero Warning), khóa ranh giới các tầng bằng `NetArchTest.Rules`. |
| **[Kỳ 4](04-phase-2-domain-modeling-and-result-pattern.md)** | **[Giai Đoạn 2: Rich Domain Model & Result Pattern](04-phase-2-domain-modeling-and-result-pattern.md)** | Xử lý sự cố Git & .gitignore cục bộ; thiết kế Rich Domain Model với Invariants; Result Pattern thay thế Exception; chuyển đổi khóa chính `long` tương thích PostgreSQL. |
| **[Kỳ 5](05-phase-3-persistence-postgresql-and-slice-pattern.md)** | **[Giai Đoạn 3: PostgreSQL Docker, Slice Mẫu & Controller](05-phase-3-persistence-postgresql-and-slice-pattern.md)** | Nhân bản database thực tế `tour_agent_db` trong Docker; triển khai Vertical Slice `SearchTours`; chuyển từ Minimal API sang Controller đầy đủ; tích hợp Swagger UI và test live data. |
| **[Kỳ 6](06-phase-4-core-ai-agent-engine-and-tool-calling.md)** | **[Giai Đoạn 4: Trái Tim AI Agent & 5 Travel Tools](06-phase-4-core-ai-agent-engine-and-tool-calling.md)** | Thiết kế `ILlmClient` độc lập vendor; tích hợp DeepSeek V3 (Function Calling & SSE); hàng rào phòng thủ chống ảo giác (Anti-hallucination); triển khai 5 Agent Tools nghiệp vụ; bộ điều phối suy luận đa lượt tự động `AgentLoop`; 39/39 tests xanh. |
| **[Kỳ 7](07-phase-5-telegram-channel-and-background-worker.md)** | **[Giai Đoạn 5: Webhook Telegram & Background Worker](07-phase-5-telegram-channel-and-background-worker.md)** | Kiên định Controller-based Webhook (`TelegramWebhookController`); giải quyết thách thức timeout bằng In-Process Channel (`System.Threading.Channels`); kiểm soát chống trùng lặp tin nhắn (Idempotency); Background Worker điều phối chu trình AI ngầm; 41/41 tests xanh. |
| **[Kỳ 8](08-phase-6-ui-planning-and-frontend-strategy.md)** | **[Giai Đoạn 6: Chiến Lược UI & Tách Biệt SPA](08-phase-6-ui-planning-and-frontend-strategy.md)** | Bài học view PHP 208KB quá tải; tách rời độc lập SPA Frontend; trải nghiệm Server-Sent Events (SSE) thời gian thực; chuẩn hóa quy trình chuyển giao context giữa các phiên chat. |
| **[Kỳ 9](09-step-1-products-and-tours-catalog.md)** | **[Bước 1: Danh Mục Sản Phẩm (Tours) & CRUD](09-step-1-products-and-tours-catalog.md)** | Phân tách rành mạch Routing Client vs Admin; loại bỏ 100% Mock Data; kết nối PostgreSQL thật bảng `travels`; phục hồi đầy đủ catalog và bộ lọc tìm kiếm. |
| **[Kỳ 10](10-step-2-grand-slam-offers-and-valuestack.md)** | **[Bước 2: Grand Slam Offers & Value Stack (Hormozi)](10-step-2-grand-slam-offers-and-valuestack.md)** | Mô hình hóa kinh doanh $100M Offers vào Domain Entity; lưu trữ JSONB PostgreSQL (cam kết, điều khoản); bộ soạn thảo Value Stack động và tính cọc tức thì. |
| **[Kỳ 11](11-step-3-conversations-and-llm-telemetry.md)** | **[Bước 3: Giám Sát Hội Thoại & AI Telemetry](11-step-3-conversations-and-llm-telemetry.md)** | Xây dựng hệ thống AI Observability (FinOps + Latency + Token); Post-mortem sự cố vi phạm ràng buộc NOT NULL CSDL; tích hợp Telegram Webhook & LLM Gateway DeepSeek. |
| **[Kỳ 12](12-step-4-contacts-and-crm.md)** | **[Bước 4: Quản Trị Khách Hàng CRM & Trụ Cột Conversion](12-step-4-contacts-and-crm.md)** | Tái thiết kế Trụ Cột Conversion; Hồ sơ khách hàng 360° độc lập booking; tự động phát hiện số điện thoại và thăng hạng Hot Lead; liên kết lịch sử chat liên kênh. |
| **[Kỳ 13](13-step-5-tour-orders-and-visa-pipeline.md)** | **[Bước 5: Quản Lý Đơn Tour & Đường Ống Visa](13-step-5-tour-orders-and-visa-pipeline.md)** | Mô hình Agency B2B2C; Cọc 50% ➔ Làm Visa qua Cty Du Lịch ➔ Thu 50% ➔ Điều phối HDV trước 3 ngày (T-3); AI Briefing Call Summary (30s Review); Tự động soạn kịch bản Zalo; 56/56 tests xanh. |
| **[Kỳ 14](14-step-6-escalation-and-order-sync.md)** | **[Bước 6: Bàn Quyết Định (Escalations) & Đồng Bộ Đơn Hàng](14-step-6-escalation-and-order-sync.md)** | Quy trình Human-in-the-Loop; giải quyết triệt để lỗi mã ticket ảo; tự động đồng bộ ba chiều Contact ➔ Ticket Escalation ➔ Tour Order; API thời gian thực và kết nối giao diện thực chiến. |
| **[Kỳ 15](15-step-7-agent-harness-general-architecture.md)** | **[Bước 7: Khung Giám Sát Tác Nhân (Agent Harness Phase A)](15-step-7-agent-harness-general-architecture.md)** | Nâng cấp AI Agent thành Production-Grade Autonomous System; State Store với OCC (G1); Status Bar Framework với State Reflection Injection (G2); Tool Guardrail Pipeline 3 lớp Constrain-Execute-Verify (G3); 68/68 tests xanh. |
| **[Kỳ 16](16-step-8-agent-harness-domain-techniques.md)** | **[Bước 8: Khung Giám Sát Nghiệp Vụ Tour (Agent Harness Phase B)](16-step-8-agent-harness-domain-techniques.md)** | Hoàn thiện 2 kỹ thuật đặc thù Domain: Tour Gateway & Tour State Reducer (D1) tách biệt ORM; Tour Status Provider & Thuật toán Phân giải Tour 5 Tầng Chống Đoán Mò (D2); Lưu vết kiểm toán vào Escalations; 84/84 tests xanh. |

---

## 🎯 Bảng Tóm Tắt Các Quyết Định Kỹ Thuật (Executive Architecture Decisions)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   KIẾN TRÚC TỔNG THỂ TOURAGENT (.NET 10)               │
├──────────────────────┬─────────────────────────────────────────────────┤
│ Tiêu chí kỹ thuật    │ Quyết định áp dụng trong dự án                 │
├──────────────────────┼─────────────────────────────────────────────────┤
│ Runtime & SDK        │ .NET 10 (LTS/Current, SDK 10.0.401)             │
│ Pattern kiến trúc    │ Clean Architecture 4 Tầng + Vertical Slices     │
│ Enforce kiến trúc    │ NetArchTest.Rules (Kiểm tra tự động tầng)       │
│ Compiler Policy      │ Nullable=enable, TreatWarningsAsErrors=true     │
│ CQRS / Mediator      │ Direct Handlers (Không MediatR, tránh bản quyền)│
│ Data Mapping         │ Manual Extension Methods (Không AutoMapper)    │
│ Cơ sở dữ liệu        │ PostgreSQL 16 (Docker: tour_agent_db)          │
│ ORM                  │ EF Core 10 (Npgsql 10.0.3) + JSON Columns      │
│ AI Client & Model    │ ILlmClient + DeepSeek V3 (OpenAI Protocol)     │
│ Agent Orchestration  │ Autonomous Multi-Turn AgentLoop (Max 5 turns)   │
│ Agent Harness        │ 5 Kỹ Thuật Chuẩn SOT: G1, G2, G3 (Phase A)     │
│                      │                    + D1, D2 (Phase B)          │
│ Omnichannel & Queue  │ System.Threading.Channels + BackgroundService   │
│ API Style            │ ASP.NET Core Controllers + Swagger UI + SSE     │
│ Error Handling       │ Result Pattern (Result<T> & Error)              │
│ Trạng thái test      │ 84/84 Tests Passed (Domain + Application + Architecture)│
└──────────────────────┴─────────────────────────────────────────────────┘
```

---

## 🌟 Giá Trị Đọng Lại Dành Cho Nhà Tuyển Dụng (Why Hire Me?)

1. **Tư duy kiến trúc vững vàng**: Không bị phụ thuộc vào các template sẵn có; hiểu bản chất của Dependency Inversion và bảo vệ nó bằng Unit Test kiến trúc.
2. **Quản trị rủi ro & chi phí công nghệ**: Nhạy bén với vấn đề thay đổi bản quyền thư viện nguồn mở (MediatR, AutoMapper, Duende), biết cách thay thế bằng code sạch, tường minh và đạt hiệu năng cao hơn.
3. **Kỹ năng điều phối AI Assistant (Vibe Coding Master)**:
   - Không để AI tự tung tự tác sinh code rác.
   - Luôn chia nhỏ thành các bước kiểm thử được (Incremental Delivery).
   - Đặt câu hỏi phản biện, điều chỉnh hướng đi kịp thời (ví dụ: chuyển từ Minimal API về Controller, yêu cầu Swagger, chuyển ID sang `long`).
4. **Tập trung vào giá trị thực chiến**: Kết nối trực tiếp với cơ sở dữ liệu thật trong Docker, kiểm thử với dữ liệu thực tế, code có XML comments đầy đủ theo chuẩn doanh nghiệp.

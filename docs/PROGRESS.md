# PROGRESS — Trạng Thái Tiến Độ Dự Án TourAgent

> **File này lưu trữ trạng thái hiện tại và tiến độ công việc giữa các session làm việc.**
> 📖 **Hồ sơ hành trình phỏng vấn & phản biện kỹ thuật**: Xem chi tiết tại [MIGRATION JOURNEY](journey/README.md).

---

## 📌 Trạng Thái Hiện Tại
- **Giai đoạn hiện tại**: **Hoàn thiện File-per-Type cho `IAgentLoop.cs` và chuẩn hóa 100% `API_ROUTES` tập trung cho toàn bộ các trang và API client trên Frontend**.
- **Trạng thái Test**: **132/132 tests PASSED** (54 Domain Tests + 74 Application Tests + 4 Architecture Tests).
- **Cơ sở dữ liệu**: Đã clone thành công database `tour_agent_db` trên PostgreSQL Docker (Port 5432) với đầy đủ 28 tables và dữ liệu tour thực tế.
- **Bước tiếp theo**: Tiếp tục hoàn thiện các màn hình chuyên sâu và tính năng AI tư vấn đóng gói tour.

---

## 🎯 Quyết Định Đã Chốt (Architecture Decisions)
- **Nguồn chân lý kiến trúc**: Toàn bộ quyết định kiến trúc được chuẩn hóa tại [`docs/adr/`](adr/README.md) gồm 11 ADRs đã chấp thuận (ADR-0001 đến ADR-0011).
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
| **6.4** | Tách Route chuẩn hóa theo phân hệ: `/` (Landing), `/admin`, `/admin/conversations`, `/admin/escalations`, `/admin/channels/telegram` | ✅ DONE | Giao diện React SPA độc lập |
| **7.1** | Phase 0 Step 1: Dọn dẹp Secret & Hardcoded API Key khỏi code và config | ✅ DONE | Trừu tượng hóa qua Env Vars |
| **7.2** | Phase 0 Step 2: Cấu hình CORS động (`CORS_ALLOWED_ORIGINS`) & Telegram WebhookUrl | ✅ DONE | Hỗ trợ Cloud Deploy |
| **7.3** | Phase 0 Step 3: Tích hợp ASP.NET Core Health Checks (`/health`) cho DB & DeepSeek AI | ✅ DONE | Endpoint `/health` kiểm tra song song DB và DeepSeek API |
| **7.4** | Phase 0 Step 4: Dockerfile Multi-stage .NET 10 & .dockerignore | ✅ DONE | Image size chỉ 102MB, non-root user, layer cache tối ưu |
| **7.5** | CI/CD Pipeline GitHub Actions deploy Azure App Service | ⏳ PENDING | GitHub Actions workflow |

---

## 📝 Nhật Ký Hoạt Động (Work Log)
- **2026-11-24**:
  - **Tối Ưu DX & Tiết Kiệm Token Cho AI Agent: Thiết Lập Bộ Đôi Script Kiểm Thử Tập Trung 1-Click (`scripts/test-backend.sh` & `scripts/test-backend.ps1`)**:
    - **Lý do**: Trước đây, mỗi lần kiểm tra sau khi code xong, AI Agent thường tốn nhiều turn và hàng chục ngàn tokens ngữ cảnh để lần lượt chạy `dotnet build` và `dotnet test` rời rạc cho từng dự án, dễ xảy ra nhầm lẫn working directory và rủi ro bỏ sót kiểm thử kiến trúc (`ArchitectureTests`).
    - **Giải pháp**:
      - Xây dựng bộ đôi script kiểm thử cross-platform tại thư mục `scripts/`:
        - [scripts/test-backend.sh](file:///d:/github/Tour-Agent/scripts/test-backend.sh): Tối ưu chạy trực tiếp trên **Git Bash**, Linux, macOS (dành cho Developer).
        - [scripts/test-backend.ps1](file:///d:/github/Tour-Agent/scripts/test-backend.ps1): Tối ưu chạy trên **Windows PowerShell** (dành cho AI Agent chạy ngầm trên Windows OS).
      - Cơ chế: Tự động định vị `TourAgent.slnx`, build toàn bộ 4 dự án src (`Domain`, `Application`, `Infrastructure`, `Api`), tự động sinh tài liệu API (Swagger + api-index) và thực thi trọn vẹn 132 tests của 3 dự án test trong đúng 1 lệnh duy nhất (~3.8s - 6.7s).
      - Hỗ trợ đầy đủ tham số lọc test `-Filter <pattern>` và bỏ qua build `-NoBuild`.
      - Cập nhật quy tắc bắt buộc vào [AGENTS.md](file:///d:/github/Tour-Agent/AGENTS.md): Nghiêm cấm chạy build/test lẻ tẻ; bắt buộc dùng 1 lệnh tập trung phù hợp theo môi trường (`./scripts/test-backend.sh`, `pwsh scripts/test-backend.ps1` hoặc `dotnet test dotnet-backend-core-main/TourAgent.slnx`).
    - **Kiểm thử**: Đã xác minh thực tế 100% test passed trên cả Git Bash lẫn PowerShell.
  - **Rà Soát & Dọn Dẹp Triệt Để Toàn Bộ Tàn Dư PHP Khỏi Dự Án TourAgent**:
    - **Lý do**: Dự án sau khi convert từ PHP sang .NET 10 còn tồn dư nhiều từ khóa, docblock XML, rule gitignore và tài liệu cũ liên quan đến PHP/Laravel (`kp3-backend-core-main`, `php artisan test`, `Pest`, `PHPStan`, `Pint`, `Blade`), gây nhầm lẫn và nhiễu ngữ cảnh cho các AI Agent khi làm việc trên codebase.
    - **Thay đổi chi tiết**:
      - **Backend C# (.NET 10)**: Chuẩn hóa toàn bộ docblock XML trong 4 Controllers (`LlmSettingController`, `OffersController`, `TelegramSetupController`, `TravelsController`), Handler `GetAllOffersHandler`, DTO `OfferDtos.cs` và Query `GetAllToursQuery.cs`, loại bỏ hoàn toàn các nhắc đến "tương thích PHP core", "PHP OfferResource".
      - **Frontend SPA (React 19 / TypeScript)**: Dọn sạch các chú thích `.blade.php` và `from PHP` trong `App.tsx`, `index.css` và 7 trang quản trị / landing page (`AdminContactsPage`, `AdminConversationsPage`, `AdminOffersPage`, `AdminOrdersPage`, `AdminProductsPage`, `AdminTelegramPage`, `LandingPage`).
      - **Cấu hình Git (.gitignore)**: Loại bỏ các mục đặc thù của Laravel & PHPUnit (`**/vendor/`, `**/.phpunit.cache/`, `**/storage/*`, `**/public/hot`...).
      - **Knowledge Base (dotnet-project-knowledge/)**: Dọn sạch `AI_CONTEXT.md` (xóa `kp3-backend-core-main/`), viết lại `TECHNICAL_NOTES.md` theo chuẩn `dotnet test` và `FakeLlmClient`, cập nhật `LESSONS_LEARNED.md` sang interface C# (`IAgentTool`, `ILlmClient`), chuẩn hóa `LESSONS_LEARNED_AGENT.md` và `GLOSSARY.md`. Xóa thư mục rỗng `dotnet-project-knowledge/adr/`.
      - **Phỏng vấn (prepare-interview/)**: Làm mới `prepare-interview/PROMPTS.md`, loại bỏ các prompt cũ về Laravel/Pest/PHPStan, cập nhật `STAIRCASE_LEARNING_METHOD.md`.
      - **Tài liệu (docs/)**: Cập nhật `UI_PLAN.md`, `API_ENDPOINTS.md` và gắn nhãn ghi chú lịch sử rõ ràng tại [docs/journey/README.md](journey/README.md).
    - **Kiểm thử**: **132/132 tests backend PASSED 100%**. Frontend `npm run build` thành công không lỗi type hay lint. `git grep` xác nhận 0 tàn dư PHP trong toàn bộ code vận hành và knowledge hub.
  - **Ban Hành Bộ 3 Quyết Định Kiến Trúc Mới (ADR-0009, ADR-0010, ADR-0011)**:
    - Kích hoạt skill `adr-manager` theo quy chuẩn Michael Nygard (2011), rà soát và chính thức ban hành 3 ADRs:
      - [ADR-0009](adr/0009-dual-persona-agent-defense-in-depth.md): Kiến trúc Dual-Persona AI Agent và phòng thủ 2 lớp (Defense-in-Depth) chống Prompt Injection bảo vệ dữ liệu nghiệp vụ.
      - [ADR-0010](adr/0010-centralized-frontend-api-routes.md): Chuẩn hóa tập trung hóa 100% URL API Routes trên Frontend (`API_ROUTES`), cấm hardcode magic strings.
      - [ADR-0011](adr/0011-facade-pattern-volatile-ui-libraries.md): Áp dụng Facade Pattern bọc thư viện UI dễ thay đổi (`notify` over `sonner`), chống Leaky Abstraction.
    - Cập nhật liên kết trực tiếp từ [AGENTS.md](../AGENTS.md) sang các ADRs và đồng bộ mục lục [docs/adr/README.md](adr/README.md).
  - **Tập Trung Hóa 100% Frontend API Routes & Cập Nhật Luật Bắt Buộc Vào AGENTS.md**:
    - **Lý do**: Khắc phục triệt để tình trạng hardcode magic string URL (`/api/v1/...`) rải rác trong các component và services trên Frontend, đồng thời ngăn chặn các AI Agent hoặc lập trình viên tái phạm trong tương lai.
    - **Thay đổi chi tiết**:
      - Ban hành hằng số tập trung `API_ROUTES` tại [apiRoutes.ts](file:///d:/github/Tour-Agent/tour-agent-frontend/src/constants/apiRoutes.ts) phân theo domain: `AGENT`, `TRAVELS`, `OFFERS`, `ADMIN`, `WEBHOOKS`.
      - Refactor toàn bộ 35+ lời gọi URL trong toàn bộ source code Frontend sang `API_ROUTES`.
      - Tách System Prompt giữa Khách hàng (`GetCustomerConsultantPrompt`) và Admin (`GetAdminCommanderPrompt`), bổ sung phân quyền `IsAdminOnly` trên `IAgentTool` và runtime guard check trong `AgentLoop`.
      - Áp dụng triệt để nguyên tắc File-per-Type: Tách `ChatRequest.cs`, `AdminChatRequest.cs`, `AgentChatInput.cs`, `AgentChatResult.cs`, `AgentStreamEvent.cs`, `AgentRole.cs` thành các file độc lập.
      - **Bổ sung Quy Tắc 12 & Điều Cấm vào [AGENTS.md](file:///d:/github/Tour-Agent/AGENTS.md)**: Yêu cầu bắt buộc 100% endpoint trên Frontend phải định nghĩa tại `API_ROUTES`, nghiêm cấm hardcode chuỗi URL.
    - **Kiểm thử**: **132/132 tests backend PASSED 100%**. Frontend `npm run build` thành công không lỗi.
  - **Chuẩn Hóa Mô Hình Quản Trị Kiến Trúc 2 Tầng (Two-Tier Pointer Model) AGENTS.md & ADR**:
    - **Vấn đề & Trăn trở của PO**: Có nguy cơ trùng lặp quy tắc hoặc phình to token nếu vừa ghi quy tắc vào `AGENTS.md` vừa tạo file ADR riêng cho các quy tắc viết code (như 0005 camelCase, 0007 Query Objects, 0008 Shimmer Zero-FOUC).
    - **Giải pháp chuẩn hóa (Phương án A được phê duyệt)**:
      - **Tầng 1 - `AGENTS.md` (Operational Memory)**: Giữ các quy tắc thép súc tích (1-2 câu) dạng checklist + link trỏ trực tiếp đến ADR (`docs/adr/000X-...`). AI Agent nạp quy tắc vào bộ nhớ làm việc ngay từ prompt đầu tiên mà không làm phình context window.
      - **Tầng 2 - `docs/adr/` (Long-term Archive)**: Lưu trữ đầy đủ bối cảnh phát sinh vấn đề, các lựa chọn bị bác bỏ (rejected alternatives), phân tích đánh đổi (trade-offs) và mã nguồn mẫu chi tiết để tra cứu lý do ("The WHY").
      - Cập nhật đồng bộ các quy tắc số 7, 8, 9, 10 và danh sách điều cấm trong `AGENTS.md` trỏ trực tiếp sang ADR-0005, 0006, 0007, 0008.
    - **Kiểm thử**: **126/126 tests backend PASSED 100%**. Frontend build 100% không lỗi.
  - **Tạo Skill `adr-manager` & Chuẩn Hóa Hệ Thống ADRs Dự Án (0001 - 0008)**:
    - **Lý do & Bối cảnh**: Để ngăn ngừa các AI Agent tương lai tái diễn lỗi (dùng sai quy ước camelCase/snake_case, tự ý viết lại quan hệ DB, tạo controller thừa...), dự án cần một Nguồn Chân Lý Duy Nhất (Single Source of Truth) tuân thủ nguyên bản Michael Nygard (2011).
    - **Thành quả**:
      - Tạo mới skill `adr-manager` tại [.agents/skills/adr-manager/SKILL.md](.agents/skills/adr-manager/SKILL.md) với quy trình 2 bước nghiêm ngặt: (1) Đánh giá tiêu chí bất khả nghịch & hỏi xác nhận từ User; (2) Tạo file ADR chuẩn và cập nhật mục lục.
      - Thiết lập kho lưu trữ trung tâm tại `docs/adr/` và tạo mục lục [docs/adr/README.md](adr/README.md).
      - Di chuyển và đồng bộ 4 ADRs từ CI/CD (`0001` đến `0004`).
      - Soạn thảo và ban hành 4 ADRs mới:
        - [ADR-0005](adr/0005-strict-camelcase-api-contracts.md): Quy ước 100% camelCase cho API contracts & cấm tuyệt đối snake_case.
        - [ADR-0006](adr/0006-hybrid-80-20-tour-offer-adaptive-ui.md): Mô hình Hybrid 80/20 Adaptive UI cho quan hệ Tour (1) — Offer (N).
        - [ADR-0007](adr/0007-query-objects-and-unified-travels-api.md): Đóng gói query params vào Query Objects & hợp nhất API Tour vào TravelsController.
        - [ADR-0008](adr/0008-separate-tour-page-zero-fouc.md): Tách route `/tour/:slug` độc lập, triệt tiêu FOUC bằng Shimmer skeleton và conditional rendering.
      - Cập nhật quy tắc bắt buộc trong [AGENTS.md](file:///d:/github/Tour-Agent/AGENTS.md).
  - **Chuẩn Hóa [FromQuery] Sang Query Objects & Hợp Nhất ToursController Vào TravelsController**:
    - **Lý do**:
      - Loại bỏ code smell "Long Parameter List" trong các action GET ở tầng API, tuân thủ nguyên tắc CQRS và ASP.NET Core Model Binding.
      - Xóa bỏ `ToursController.cs` dư thừa chỉ chứa duy nhất `SearchTours`, hợp nhất vào `TravelsController.cs` theo route `GET /api/v1/travels/search` (không cần giữ route alias `tours` do FE không sử dụng).
    - **Thay đổi chi tiết**:
      - [ContactsController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/ContactsController.cs): Chuẩn hóa `[FromQuery] GetContactsQuery query` thay cho 5 tham số rời rạc.
      - [ConversationsController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/ConversationsController.cs): Chuẩn hóa `[FromQuery] GetConversationsQuery query` thay cho 2 tham số rời rạc.
      - [EscalationsController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/EscalationsController.cs): Chuẩn hóa `[FromQuery] GetEscalationsQuery query` thay cho 2 tham số rời rạc.
      - [OffersController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/OffersController.cs): Chuẩn hóa `[FromQuery] GetAllOffersQuery query` thay cho 4 tham số rời rạc.
      - [TourOrdersController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/TourOrdersController.cs): Chuẩn hóa `[FromQuery] GetOrdersQuery query` thay cho 4 tham số rời rạc.
      - [TravelsController.cs](dotnet-backend-core-main/src/TourAgent.Api/Controllers/TravelsController.cs): Chuẩn hóa `[FromQuery] GetAllToursQuery query`, tiếp nhận `SearchTours([FromQuery] SearchToursQuery query)` tại route `GET /api/v1/travels/search`.
      - **Xóa bỏ**: `dotnet-backend-core-main/src/TourAgent.Api/Controllers/ToursController.cs`.
    - **Kiểm thử**: **126/126 tests backend PASSED 100%** (54 Domain + 68 Application + 4 Architecture). Frontend `tsc -b && vite build` 100% không lỗi.
  - **Triển Khai Mô Hình Hybrid 80/20 Adaptive UI Cho Quan Hệ Tour — Offer ([TourProductPage.tsx](tour-agent-frontend/src/pages/TourProductPage.tsx))**:
    - **Bối cảnh & Quyết định**: Người dùng phê duyệt Phương án 4 (Hybrid 80/20 Adaptive UI) trong tài liệu phân tích kiến trúc `tour_offer_relationship_and_ui_strategy_plan.md`.
    - **Backend .NET 10**:
      - Mở rộng `TourDetailDto` bổ sung `IReadOnlyList<OfferDto>? Offers = null`.
      - Cập nhật `GetTourBySlugHandler.cs` và `GetTourByIdHandler.cs` nạp danh sách các gói Offer đang hoạt động (`Status == OfferStatus.Active`) kèm `OfferItems` sắp xếp theo cờ `IsFeatured`.
      - Mở rộng `GetAllOffersQuery` và `OffersController.cs` hỗ trợ tham số lọc `travel_id`.
    - **Frontend React 19 ([TourProductPage.tsx](tour-agent-frontend/src/pages/TourProductPage.tsx))**:
      - Thêm state `selectedOfferIndex` quản lý gói offer đang chọn.
      - **Giao diện thích ứng (Adaptive UI)**:
        - *Nếu Tour có từ 2 Offer trở lên*: Tự động hiển thị thanh chuyển đổi Pill Tabs `[🌸 Gói 1 (28tr9)]` `[💎 Gói 2 (34tr9)]` ở đầu section Offer. Khi khách chuyển tab, toàn bộ giá, % cọc, quyền lợi Value Stack và thẻ giá ở Hero sẽ cập nhật đồng bộ mượt mà.
        - *Nếu Tour có đúng 1 Offer*: Hiển thị trực tiếp 1 lời đề nghị rõ ràng, tinh gọn, không có tab thừa, không gây phân tâm cho khách.
        - *Nếu Tour chưa có Offer*: Tự động fallback sang danh sách dịch vụ bao gồm (`tour.inclusions`) hoặc ẩn hoàn toàn nếu không có dữ liệu.
    - **Kiểm thử**: **126/126 tests backend PASSED 100%** (54 Domain + 68 Application + 4 Architecture). Frontend `tsc -b && vite build` 100% không lỗi.
  - **Loại Bỏ Hoàn Toàn Dữ Liệu Mẫu (Zero Mock Data), Ẩn Section Rỗng & Menu Điều Hướng Động ([TourProductPage.tsx](tour-agent-frontend/src/pages/TourProductPage.tsx))**:
    - **Vấn đề**: `TourProductPage.tsx` trước đó còn chứa các hằng số mock giả định (`DEFAULT_REVIEWS`, `DEFAULT_FAQS`, `DEFAULT_VALUE_STACK`, `DEFAULT_ITINERARY_IMAGES`, ảnh Hero Unsplash hardcode). Điều này gây khó chịu khi xem các tour chưa có dữ liệu thật, đồng thời menu điều hướng hiển thị các link không có nội dung.
    - **Giải pháp**:
      - Xóa bỏ 100% các hằng số dữ liệu mẫu: `DEFAULT_REVIEWS`, `DEFAULT_FAQS`, `DEFAULT_VALUE_STACK`, `DEFAULT_ITINERARY_IMAGES`.
      - **Conditional Rendering nghiêm ngặt**:
        - Đánh giá khách hàng (Reviews): Khởi tạo `[]`, chỉ hiển thị section nếu API trả về `reviews.length > 0`.
        - Câu hỏi thường gặp (FAQs): Khởi tạo `[]`, chỉ hiển thị section nếu API trả về `faqs.length > 0`.
        - Giá trị bao gồm (Value Stack): Khởi tạo `[]`, phân tích từ `inclusions` thực tế; nếu không có thì ẩn hoàn toàn section Offer.
        - Chính sách & cam kết: Chỉ hiển thị section nếu tour có ít nhất 1 chính sách trong DB (`cancellationPolicy`, `visaPolicy`, `paymentPolicy`, `childPolicy`).
        - Lịch trình & ảnh: Ngày nào có ảnh thì hiển thị ảnh thật (`day.images[0]`), ngày nào không có ảnh thì render card dạng text thanh lịch, không dùng ảnh Unsplash giả.
        - Ảnh bìa Hero: Sử dụng background Dark Luxury Gradient sang trọng nếu tour không có ảnh thật.
      - **Menu Điều Hướng Động (Dynamic Navbar & Footer)**: Mảng `navItems` tự động tính toán dựa trên các section thực tế tồn tại. Section nào bị ẩn thì mục tương ứng trên Header và Footer sẽ không xuất hiện.
    - **Kiểm thử**: **126/126 tests backend PASSED 100%** (54 Domain + 68 Application + 4 Architecture). Frontend `tsc -b && vite build` 100% không lỗi.
  - **Tách Riêng Trang Chi Tiết Sản Phẩm ([TourProductPage.tsx](tour-agent-frontend/src/pages/TourProductPage.tsx)) & Khắc Phục Triệt Để Hiện Tượng Flash Dữ Liệu Mẫu**:
    - **Nguyên nhân**: `LandingPage.tsx` gộp chung trang chủ và sản phẩm, đồng thời nạp fallback data mặc định ("Cung Đường Vàng Nhật Bản") trong lúc chờ API trả về, gây ra hiện tượng FOUC (chớp nháy giao diện từ tour mẫu sang tour thực tế).
    - **Giải pháp**:
      - Tạo mới [TourProductPage.tsx](tour-agent-frontend/src/pages/TourProductPage.tsx) phục vụ riêng cho route `/tour/:slug`.
      - Tích hợp Skeleton Shimmer Loading chuẩn Dark Glassmorphism ngay từ đầu (`loading = true`), loại bỏ 100% dữ liệu fallback mặc định của tour khác 👉 triệt tiêu hoàn toàn hiện tượng chớp nháy.
      - Bổ sung màn hình 404 thân thiện và sang trọng khi đường dẫn slug không tồn tại.
      - Tinh gọn [LandingPage.tsx](tour-agent-frontend/src/pages/LandingPage.tsx), trả lại đúng vai trò Corporate/Brand Landing Page cho trang chủ `/`.
      - Cập nhật định tuyến rõ ràng trong [App.tsx](tour-agent-frontend/src/App.tsx).
    - **Kiểm thử**: **126/126 tests backend PASSED 100%** (54 Domain + 68 Application + 4 Architecture). Frontend `tsc -b && vite build` 100% không lỗi.
  - **URL Slug Sản Phẩm Tour & Nút Xem Landing Page từ Admin**:
    - **Frontend Admin Products ([AdminProductsPage.tsx](tour-agent-frontend/src/pages/AdminProductsPage.tsx))**:
      - Bổ sung nút hành động `🌐 Xem Page ↗` (mở tab mới `target="_blank"`) trong cột Chi Tiết Kỹ Thuật cho từng dòng tour.
      - Bổ sung icon link `↗` ngay cạnh tên tour tại cột Mã & Tên Tour Sản Phẩm.
    - **Backend .NET 10**:
      - Tạo mới use case `GetTourBySlugQuery` & `GetTourBySlugHandler` trong `TourAgent.Application/Features/Tours/GetTourBySlug/`.
      - Hỗ trợ tra cứu linh hoạt theo `slug` hoặc fallback theo `code` / `id`.
      - Bổ sung endpoint `GET /api/v1/travels/slug/{slug}` vào `TravelsController.cs`.
      - Bổ sung `TourErrors.NotFoundBySlug` vào Domain.
      - Đăng ký DI trong `DependencyInjection.cs`.
      - Bổ sung 4 unit tests mới trong `GetTourBySlugHandlerTests.cs` (tổng test backend nâng lên 126 tests).
    - **Landing Page Frontend ([LandingPage.tsx](tour-agent-frontend/src/pages/LandingPage.tsx))**:
      - Kết nối hook `useParams<{ slug?: string }>()`.
      - Tải động dữ liệu chi tiết tour, lịch trình các ngày (`itinerary`), bảng ngày khởi hành (`departures`), chính sách cam kết (`cancellationPolicy`), câu hỏi thường gặp (`faqs`) và đánh giá khách hàng (`reviews`) từ Backend API.
      - Giữ nguyên trải nghiệm trang chủ công ty `http://localhost:5173/` với fallback mượt mà.
    - **Kiểm thử**: **126/126 tests backend PASSED 100%** (54 Domain + 68 Application + 4 Architecture). Frontend `tsc -b && vite build` 100% không lỗi.
- **2026-11-23**:
  - **Tái Cấu Trúc Toàn Diện Module Tour Reviews & Tour FAQs Chuẩn Clean Architecture (Fix Root Cause Triệt Để)**:
    - Loại bỏ hoàn toàn phương pháp "chữa cháy" tạm thời (`EnsureTablesExistAsync`, kết nối raw `NpgsqlConnection` và SQL strings trong `TravelsController`).
    - **Tầng Domain (`TourAgent.Domain/Tours/`)**:
      - Tạo 2 Entity độc lập: `TourReview.cs` và `TourFaq.cs` kế thừa `Entity`, quản lý private setter, static factory method `Create()` kiểm soát chặt chẽ các bất biến nghiệp vụ (Rating 1-5 sao, nội dung không rỗng, câu hỏi/câu trả lời không rỗng).
      - Mở rộng `TourErrors.cs` với đầy đủ mã lỗi nghiệp vụ cho Review và FAQ (`ReviewerNameEmpty`, `ReviewContentEmpty`, `InvalidRating`, `FaqQuestionEmpty`, `FaqAnswerEmpty`,...).
      - Bổ sung bộ 19 unit tests mới trong `TourReviewTests.cs` và `TourFaqTests.cs`, nâng tổng số domain tests lên 54 tests.
    - **Tầng Infrastructure (`TourAgent.Infrastructure/`)**:
      - Viết `TourReviewConfiguration.cs` và `TourFaqConfiguration.cs` ánh xạ Fluent API vào bảng `travel_reviews` và `travel_faqs`.
      - Đăng ký `DbSet<TourReview>` và `DbSet<TourFaq>` vào `IAppDbContext` và `AppDbContext`.
    - **Tầng Application (`TourAgent.Application/Features/Tours/`)**:
      - Tạo 8 Use Case Handlers chuẩn mực:
        - Reviews: `GetTourReviewsQuery/Handler`, `CreateTourReviewCommand/Handler`, `UpdateTourReviewVisibilityCommand/Handler`, `DeleteTourReviewCommand/Handler`.
        - FAQs: `GetTourFaqsQuery/Handler`, `CreateTourFaqCommand/Handler`, `UpdateTourFaqVisibilityCommand/Handler`, `DeleteTourFaqCommand/Handler`.
      - Đăng ký 8 Handlers vào DI container tại `DependencyInjection.cs`.
    - **Tầng API ([TravelsController.cs](src/TourAgent.Api/Controllers/TravelsController.cs))**:
      - Loại bỏ toàn bộ `using Npgsql;`, `_connectionString`, và code ADO.NET thô.
      - Inject trực tiếp 8 Handlers qua constructor theo đúng kiến trúc của toàn bộ dự án.
    - **Kiểm thử**: **122/122 tests PASSED 100%** (54 Domain + 64 Application + 4 Architecture). Frontend Vite build 100% sạch sẽ.
  - **Refactor toàn diện chuẩn camelCase 100% giữa .NET 10 Backend và React Frontend (Phương án B)**:
    - Bổ sung quy chuẩn bắt buộc vào `AGENTS.md` (Mục 3.7 & Mục 5): Chuẩn hóa 100% `camelCase` cho API Request/Response payload, DTOs và Frontend state; cấm hoàn toàn `snake_case`.
    - Loại bỏ triệt để toàn bộ `[property: JsonPropertyName("...")]` snake_case trên tất cả DTOs trong tầng `Application` (`TourOrderDtos.cs`, `ConversationDtos.cs`, `CrmContactDtos.cs`). Tận dụng `JsonNamingPolicy.CamelCase` tự động của ASP.NET Core để code C# giữ PascalCase sạch sẽ, không boilerplate thừa.
    - Cập nhật toàn bộ các Controllers (`LlmSettingController`, `TelegramSetupController`, `ContactsController`, `TourOrdersController`, `ConversationsController`, `TravelsController`) sang camelCase thuần túy (`perPage`, `zaloMessage`, `botInfo`, `latencyMs`, `testOutput`, `reviewerName`, `sortOrder`, `isVisible`,...).
    - Đồng bộ hóa 100% TypeScript interfaces, JSX state bindings và payload gửi đi trong Frontend React (`AdminOrdersPage.tsx`, `AdminConversationsPage.tsx`, `AdminContactsPage.tsx`, `AdminLlmSettingsPage.tsx`, `AdminTelegramPage.tsx`, `AdminProductsPage.tsx`).
    - Khắc phục triệt để lỗi payload mismatch (ví dụ: submit API key báo thành công nhưng không lưu do khác biệt giữa `api_key` và `apiKey`).
    - Kiểm thử: **103/103 tests backend PASSED** (35 Domain + 64 Application + 4 Architecture). Frontend `tsc -b && vite build` hoàn thành 100% không lỗi.
  - Chuẩn hóa và dọn dẹp triệt để các multiple route attributes trên các Controller (`LlmSettingController`, `ContactsController`, `ConversationsController`, `EscalationsController`, `TourOrdersController`, `TravelsController`).
  - Đưa route gốc lên cấp Class Controller (`[Route("...")]`) theo chuẩn ASP.NET Core RESTful, loại bỏ các alias trùng lặp và các method thừa (`HttpPatch`, `/status`, `/save`, `/catalog`).
  - Tinh gọn tài liệu Swagger và `docs/api/api-index.txt` từ **91 endpoints xuống còn 57 endpoints** sạch sẽ, không trùng lặp.
- **2026-11-12**:
  - Hoàn thành Step 4: Viết Multi-stage `Dockerfile` (.NET 10) gồm 4 stages (base, build, publish, final) và `.dockerignore`.
  - Tối ưu Docker Layer Caching (copy .csproj và restore trước khi copy source).
  - Sử dụng non-root user (`USER $APP_UID`) chuẩn bảo mật ASP.NET 10.
  - Test build Docker image `touragent-api:local` thành công với dung lượng tối ưu chỉ 102MB.
  - Chạy thử nghiệm container, xác minh tính năng Health Check trả về kết quả chuẩn xác trên Docker.
- **2026-11-11**:
  - Khởi tạo GitHub Issue #1 theo dõi quá trình chuẩn bị Cloud-Ready cho CI/CD.
  - Hoàn thành Step 1: Loại bỏ API key hardcode trong `LlmSettingsStore.cs`, dọn sạch `appsettings.json`, chuyển cấu hình local vào `appsettings.Development.json`.
  - Hoàn thành Step 2: Cấu hình CORS động từ `Cors:AllowedOrigins` và `CORS_ALLOWED_ORIGINS`, cập nhật ưu tiên biến môi trường cho `TelegramSettingsStore` và `LlmSettingsStore`.
  - Tạo tài liệu checklist hướng dẫn cấu hình toàn diện `docs/AZURE_DEPLOYMENT_CHECKLIST.md`.
  - Toàn bộ 87 tests PASSED (35 Domain + 48 Application + 4 Architecture).
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

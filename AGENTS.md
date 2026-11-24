# AGENTS.md — TourAgent (.NET 10 Clean Architecture)

> **Tài liệu quy tắc bắt buộc dành cho mọi AI Assistant và Developer khi làm việc trên dự án TourAgent.**
> Đọc file này và `docs/PROGRESS.md` trước khi thực hiện bất kỳ hành động nào trong mỗi session mới.

---

## 1. 📌 Bối Cảnh Sản Phẩm & Mục Tiêu Dự Án

- **Tên dự án**: **TourAgent** — AI Agent tư vấn tour và đóng gói du lịch cho doanh nghiệp du lịch SME.
- **Mục tiêu kỹ thuật**:
  - Xây dựng trên nền tảng **C# / .NET 10** mới nhất.
  - Sử dụng **Clean Architecture** tinh giản (SME-friendly), không over-engineering.
  - Chuẩn bị sản phẩm chuẩn mực, có kiểm thử kiến trúc (Architecture Tests), dùng làm dự án mẫu demo khi phỏng vấn Senior/Lead .NET.
- **Mục tiêu kinh doanh**:
  - Trả lời tư vấn tour 24/7 với thời gian phản hồi nhanh.
  - Tra cứu chính xác ngày bay, giá vé, chính sách từ DB (Zero Hallucination).
  - Thu thập lead (SĐT/Zalo) và chuyển giao cho nhân viên tư vấn con người (Escalation).

---

## 2. 🏛️ Kiến Trúc Hệ Thống: Clean Architecture (4 Layers)

Hướng phụ thuộc **chỉ đi vào trong**: `Api` ➔ `Infrastructure` ➔ `Application` ➔ `Domain`.

1. **`TourAgent.Domain`**:
   - Chứa Entities, Value Objects, Domain Rules, `Result<T>`, `Error`.
   - **Tuyệt đối KHÔNG phụ thuộc** vào `Application`, `Infrastructure`, `Api` hay bất kỳ thư viện bên ngoài nào (kể cả EF Core).
2. **`TourAgent.Application`**:
   - Chứa Use Cases chia theo **Feature Folders** (`Features/{Area}/{UseCase}/`), DTOs, Abstractions (`IAppDbContext`, `ILlmClient`), Agent Tools, Prompts.
   - **Tuyệt đối KHÔNG phụ thuộc** vào `Infrastructure` hoặc `Api`.
   - Lệnh & Vị trí file
     - Backend nằm trong `dotnet-backend-core-main/`.
     - **Lệnh kiểm tra toàn diện Backend duy nhất (1-Command Verification)**:
       - Chạy từ thư mục gốc repository: `dotnet test dotnet-backend-core-main/TourAgent.slnx`
       - Khi đang đứng trong thư mục `dotnet-backend-core-main/`: `dotnet test TourAgent.slnx`
       - *Lưu ý*: Lệnh này chuẩn cross-platform (hoạt động giống hệt nhau trên mọi terminal: Git Bash, PowerShell, Linux, macOS, GitHub Actions CI), tự động build toàn bộ 4 projects src (`Domain`, `Application`, `Infrastructure`, `Api`), tự kích hoạt sinh `docs/api/swagger.json` & `docs/api/api-index.txt`, và chạy trọn vẹn 132 tests của 3 test projects chỉ trong ~3.5s - 6s. Không cần tạo thêm bất kỳ script wrapper nào.
     - Schema DB: `dotnet-backend-core-main/db/supabase_migration.sql` (nguồn duy nhất).
     - `docs/api/`: tự sinh khi build Debug. KHÔNG sửa tay.
   - Tra cứu API
     1. Đọc `docs/api/api-index.txt` trước (format: `METHOD  path  [tag]`).
     2. Cần request/response chi tiết thì chỉ trích endpoint đó trong
        `docs/api/swagger.json`. KHÔNG đọc toàn bộ file.
3. **`TourAgent.Infrastructure`**:
   - Cài đặt các interface từ `Application`: EF Core DbContext, LlmClient, Background Services, Channels.
   - **Tuyệt đối KHÔNG phụ thuộc** vào `Api`.
4. **`TourAgent.Api`**:
   - ASP.NET Core Minimal APIs, Dependency Injection setup, Middleware.
   - Không chứa business logic.
5. **Kiểm tra tự động**:
   - Ranh giới các tầng được kiểm tra tự động bởi `tests/TourAgent.ArchitectureTests` sử dụng `NetArchTest.Rules`. Mọi vi phạm tầng sẽ khiến `dotnet test` báo lỗi ngay lập tức.
6. **Kho Quyết Định Kiến Trúc (Architecture Decisions)**:
   - Toàn bộ bối cảnh, phân tích đánh đổi và quyết định kiến trúc được lưu trữ tập trung tại [`docs/adr/`](docs/adr/README.md) (ADR-0001 đến 0008). Mọi quy tắc code bên dưới đều có ADR tương ứng để tra cứu lý do ("The WHY").

---

## 3. 📜 Quy Ước Code Bắt Buộc (Strict Guidelines)

1. **User Global Rules**:

   - **Luôn viết comment XML doc/giải thích cho MỌI hàm/method**.
   - **Đặt tên biến rõ ràng, dễ hiểu, tránh viết tắt tối nghĩa**.
2. **Use Case / Feature Folder Pattern**:

   - Mỗi use case là một folder: `Features/{Area}/{UseCase}/` gồm:
     - `{Name}Command.cs` hoặc `{Name}Query.cs`
     - `{Name}Handler.cs` (sealed class, 1 method public `HandleAsync(request, CancellationToken ct)`)
     - `{Name}Validator.cs`
     - `{Name}Response.cs`
3. **Không dùng MediatR & Không dùng AutoMapper**:

   - Handlers được inject trực tiếp vào Minimal API endpoint.
   - Data Mapping hoàn toàn viết thủ công bằng Extension Methods (ví dụ: `tour.ToResponse()`).
4. **Data Access**:

   - Truy cập dữ liệu qua `IAppDbContext`.
   - **Không tạo Generic Repository riêng cho từng entity** (tránh boilerplate thừa).
5. **Error Handling & Invariants**:

   - Lỗi nghiệp vụ trả về `Result<T>.Failure(Error)`. Không dùng exception cho luồng xử lý nghiệp vụ thông thường.
   - Entities có private setter, khởi tạo qua static factory method `Create()` để kiểm soát invariants.
6. **Async & Type Safety**:

   - Mọi phương thức async PHẢI nhận `CancellationToken ct`.
   - Bật `<Nullable>enable</Nullable>` và `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
7. **JSON & API Naming Convention (Chuẩn camelCase 100%)** — Xem [ADR-0005](docs/adr/0005-strict-camelcase-api-contracts.md):

   - Dự án kết hợp giữa **.NET 10** và **React (TypeScript)**: Mọi Request Payload và Response Data qua HTTP API **bắt buộc dùng `camelCase`**, **tuyệt đối KHÔNG dùng `snake_case`**.
   - **Backend (.NET)**: DTOs khai báo bằng C# record/class PascalCase thông thường (ví dụ: `ApiKey`, `BaseUrl`, `BotToken`). ASP.NET Core với `JsonNamingPolicy.CamelCase` sẽ tự động chuyển thành `apiKey`, `baseUrl`, `botToken`. **KHÔNG đặt `[JsonPropertyName("ten_snake_case")]`**.
   - **Frontend (React)**: Mọi interface, state và payload gửi đi (`apiFetch`) đều dùng `camelCase` thuần túy (`apiKey`, `baseUrl`, `botToken`, `isConfigured`).
8. **Query Objects Cho Controller & Hợp Nhất TravelsController** — Xem [ADR-0007](docs/adr/0007-query-objects-and-unified-travels-api.md):

   - Mọi action HTTP GET có nhiều hơn 1 tham số lọc/phân trang phải gom vào Query Object: `[FromQuery] TQuery query`.
   - Toàn bộ API nghiệp vụ tour tập trung tại `TravelsController.cs` (endpoint tìm kiếm nâng cao: `GET /api/v1/travels/search`). Không tạo thêm `ToursController.cs`.
9. **Mô Hình Tour (1) — Offer (N) & Adaptive UI** — Xem [ADR-0006](docs/adr/0006-hybrid-80-20-tour-offer-adaptive-ui.md):

   - Database giữ quan hệ chuẩn 1-N (1 Tour có thể có nhiều Offer tiers).
   - Frontend hiển thị thích ứng: Nếu tour chỉ có 1 offer thì hiển thị trực diện phẳng; nếu có từ 2 offer trở lên thì hiển thị thanh chuyển đổi Pill Tabs.
10. **Tách Biệt Route Sản Phẩm & Zero-FOUC Shimmer UX** — Xem [ADR-0008](docs/adr/0008-separate-tour-page-zero-fouc.md):

- Trang chủ công ty (`/`) dùng riêng `LandingPage.tsx`; trang chi tiết sản phẩm (`/tour/:slug`) dùng riêng `TourProductPage.tsx`.
- Bắt buộc Shimmer Loading Skeleton khi fetch API. Cấm tuyệt đối khởi tạo mock data gây chớp nháy (FOUC). Section không có data thì ẩn hoàn toàn cùng mục menu tương ứng.

11. **Nguyên Tắc File-Per-Type & Tách Biệt Request DTOs Khỏi Controller**:

- Mọi DTO, Request/Response record, Query Object, Enum, hoặc Command/Query **BẮT BUỘC nằm trong file `.cs` riêng biệt**, tên file trùng khớp 100% với tên Type.
- **Tuyệt đối KHÔNG khai báo `record SomeRequest(...)` hay DTOs ở đầu/cuối file Controller** (vi phạm Single Responsibility Principle & File-per-Type).
- Các API Request/Response DTOs thuộc tầng Api phải đặt tại thư mục `TourAgent.Api/Contracts/{Feature}/` (ví dụ: `TourAgent.Api/Contracts/Agent/ChatRequest.cs`).

12. **Tập Trung Hóa API Routes Trên Frontend (Zero Hardcoded URLs)** — Xem [ADR-0010](docs/adr/0010-centralized-frontend-api-routes.md):

- Mọi URL endpoint HTTP được gọi từ Frontend (`apiFetch`, `fetch`, SSE `EventSource`, v.v.) **BẮT BUỘC định nghĩa tập trung** trong `tour-agent-frontend/src/constants/apiRoutes.ts` thông qua hằng số `API_ROUTES`.
- Với các route có dynamic parameter (như `id`, `slug`), bắt buộc định nghĩa hàm helper nhận tham số (ví dụ: `API_ROUTES.ADMIN.TRAVEL_BY_ID(id)`, `API_ROUTES.TRAVELS.BY_SLUG(slug)`).
- **Tuyệt đối KHÔNG gõ chuỗi URL trực tiếp** như `'/api/v1/...'` rải rác trong React components, hooks, hay API services. Mọi endpoint mới phải được khai báo trong `API_ROUTES` trước khi gọi.

13. **Cô Lập Thư Viện UI Thứ Ba Dễ Biến Động (Facade Pattern)** — Xem [ADR-0011](docs/adr/0011-facade-pattern-volatile-ui-libraries.md):

- Mọi thao tác hiển thị Toast/Alert trong ứng dụng **BẮT BUỘC** gọi thông qua module `notify` tại `src/lib/notify.tsx` (`notify.success`, `notify.error`, `notify.warning`, `notify.info`).
- Sử dụng kiểu dữ liệu riêng của dự án (`NotifyOptions`), tuyệt đối không để lộ hoặc tái sử dụng type của thư viện bên ngoài ra ngoài wrapper (ngăn chặn Leaky Abstraction).
- Tuyệt đối **KHÔNG import trực tiếp `'sonner'`** trong bất kỳ component, hook hay trang nào khác.

---

## 4. 🤖 Quy Tắc Riêng Cho AI Agent & Tool Calling

1. **Zero Hallucination về số liệu**:
   - Giá tour, ngày bay, lịch trình, chính sách: **CHỈ được lấy từ database qua Tool**.
   - LLM tuyệt đối không tự bịa đặt giá hoặc lịch bay nếu Tool không trả về.
   - Mọi tool trả về dữ liệu có cấu trúc; tool lỗi → agent xin lỗi và escalate, không đoán.
2. **Abstractions**:
   - LLM chỉ được tương tác qua `ILlmClient` (sử dụng `Microsoft.Extensions.AI` hoặc OpenAI client chuẩn).
3. **Observability**:
   - Mỗi lượt hội thoại phải lưu log: `conversationId`, `tokens_in`, `tokens_out`, `latency_ms`, danh sách tool đã gọi.
   - Dữ liệu cá nhân (SĐT, email) không ghi vào log dạng plain text
4. **Phân Quyền Dual-Persona & Phòng Thủ 2 Lớp (Defense-in-Depth)** — Xem [ADR-0009](docs/adr/0009-dual-persona-agent-defense-in-depth.md):
   - Tách biệt 100% System Prompt giữa `Customer Consultant` (khách hàng tư vấn tour) và `Admin Commander` (quản trị viên điều hành).
   - Mọi Write Tool (như `update_travel_title`, `update_offer_price`) bắt buộc khai báo `IsAdminOnly => true`.
   - Bảo vệ 2 lớp: Lớp 1 (Context Filter) giấu tool definition khỏi prompt khách hàng; Lớp 2 (Runtime Guard) kiểm tra quyền hạn trong `AgentLoop` chặn đứng thực thi nếu phát hiện vai trò khách hàng gọi Write Tools.
   - Tách biệt endpoint API: `POST /api/v1/agent/chat` (Consultant) vs `POST /api/v1/agent/admin/chat` (Commander).

---

## 5. 🚫 Danh Sách Điều CẤM (Forbidden List)

- ❌ CẤM cài thêm NuGet package mà chưa được người dùng đồng ý.
- ❌ CẤM dùng MediatR, AutoMapper.
- ❌ CẤM tạo Generic Repository pattern cho từng entity.
- ❌ CẤM sửa hoặc xóa test để "cho qua" (fix code chứ không sửa test sai lệch).
- ❌ CẤM tạo abstraction trừu tượng trước khi có nhu cầu sử dụng thực tế (YAGNI).
- ❌ CẤM đoán API/package/version, hỏi lại thay vì đoán, nói rõ "tôi không chắc"
- ❌ CẤM dùng `snake_case` cho API Request/Response payload, DTOs, hay frontend state ([ADR-0005](docs/adr/0005-strict-camelcase-api-contracts.md)).
- ❌ CẤM viết danh sách tham số rời rạc dài trong GET action controller hoặc tạo lại `ToursController.cs` ([ADR-0007](docs/adr/0007-query-objects-and-unified-travels-api.md)).
- ❌ CẤM hardcode mock data giả lập trong frontend component gây chớp nháy FOUC ([ADR-0008](docs/adr/0008-separate-tour-page-zero-fouc.md)).
- ❌ CẤM khai báo DTOs, Request/Response records, Enums chung file với Controller (vi phạm File-per-Type & Single Responsibility Principle). Mọi contract phải tách thành file riêng biệt tại `TourAgent.Api/Contracts/{Feature}/` hoặc Application Feature Folder.
- ❌ CẤM hardcode chuỗi URL endpoint trực tiếp trong React components, hooks hoặc API services (ví dụ: `'/api/v1/...'`). Bắt buộc sử dụng hằng số tập trung `API_ROUTES` từ `tour-agent-frontend/src/constants/apiRoutes.ts` ([ADR-0010](docs/adr/0010-centralized-frontend-api-routes.md)).
- ❌ CẤM gộp chung System Prompt giữa khách hàng và admin; CẤM cấp quyền hoặc để lộ Write Tools cho khách hàng ([ADR-0009](docs/adr/0009-dual-persona-agent-defense-in-depth.md)).
- ❌ CẤM import trực tiếp thư viện `'sonner'` rải rác trong các React components; bắt buộc dùng `notify` từ `@/lib/notify` ([ADR-0011](docs/adr/0011-facade-pattern-volatile-ui-libraries.md)).
- ❌ CẤM chạy build hoặc unit test lẻ tẻ từng dự án qua nhiều turn (gây lãng phí token context và dễ sót kiểm thử). Bắt buộc dùng 1 lệnh duy nhất: `dotnet test dotnet-backend-core-main/TourAgent.slnx` (hoặc `dotnet test TourAgent.slnx` khi đứng trong thư mục backend).

---

## 6. 🔄 Quy Trình Mỗi Task (Session Workflow)

1. Đọc `AGENTS.md` và `docs/PROGRESS.md`.
2. Nêu rõ kế hoạch: các file sẽ tạo/sửa + lý do.
3. Code theo từng bước nhỏ, có comment đầy đủ cho từng hàm.
4. Chạy kiểm tra toàn diện Backend bằng 1 lệnh duy nhất: `dotnet test dotnet-backend-core-main/TourAgent.slnx` (hoặc `dotnet test TourAgent.slnx`), đảm bảo 100% test pass.
5. Cập nhật `docs/PROGRESS.md` trước khi kết thúc session.
6. Nếu có quyết định kiến trúc mới → kích hoạt skill `adr-manager` để đánh giá và tạo ADR trong `docs/adr/`, không tự quyết.

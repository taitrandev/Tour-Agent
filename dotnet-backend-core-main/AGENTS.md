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
3. **`TourAgent.Infrastructure`**:
   - Cài đặt các interface từ `Application`: EF Core DbContext, LlmClient, Background Services, Channels.
   - **Tuyệt đối KHÔNG phụ thuộc** vào `Api`.
4. **`TourAgent.Api`**:
   - ASP.NET Core Minimal APIs, Dependency Injection setup, Middleware.
   - Không chứa business logic.
5. **Kiểm tra tự động**:
   - Ranh giới các tầng được kiểm tra tự động bởi `tests/TourAgent.ArchitectureTests` sử dụng `NetArchTest.Rules`. Mọi vi phạm tầng sẽ khiến `dotnet test` báo lỗi ngay lập tức.

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

---

## 4. 🤖 Quy Tắc Riêng Cho AI Agent & Tool Calling

1. **Zero Hallucination về số liệu**:
   - Giá tour, ngày bay, lịch trình, chính sách: **CHỈ được lấy từ database qua Tool**.
   - LLM tuyệt đối không tự bịa đặt giá hoặc lịch bay nếu Tool không trả về.
2. **Abstractions**:
   - LLM chỉ được tương tác qua `ILlmClient` (sử dụng `Microsoft.Extensions.AI` hoặc OpenAI client chuẩn).
3. **Observability**:
   - Mỗi lượt hội thoại phải lưu log: `conversationId`, `tokens_in`, `tokens_out`, `latency_ms`, danh sách tool đã gọi.

---

## 5. 🚫 Danh Sách Điều CẤM (Forbidden List)
- ❌ CẤM cài thêm NuGet package mà chưa được người dùng đồng ý.
- ❌ CẤM dùng MediatR, AutoMapper.
- ❌ CẤM tạo Generic Repository pattern cho từng entity.
- ❌ CẤM sửa hoặc xóa test để "cho qua" (fix code chứ không sửa test sai lệch).
- ❌ CẤM tạo abstraction trừu tượng trước khi có nhu cầu sử dụng thực tế (YAGNI).

---

## 6. 🔄 Quy Trình Mỗi Task (Session Workflow)
1. Đọc `AGENTS.md` và `docs/PROGRESS.md`.
2. Nêu rõ kế hoạch: các file sẽ tạo/sửa + lý do.
3. Code theo từng bước nhỏ, có comment đầy đủ cho từng hàm.
4. Chạy `dotnet build` và `dotnet test` đảm bảo 100% test pass.
5. Cập nhật `docs/PROGRESS.md` trước khi kết thúc session.

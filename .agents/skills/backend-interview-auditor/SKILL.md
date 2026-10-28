---
name: backend-interview-auditor
description: Soi mã nguồn backend TourAgent (.NET 10 Clean Architecture, CQRS, EF Core, AI Agent) để trích xuất câu hỏi phỏng vấn thực chiến và phản biện kỹ thuật bám sát 100% dự án.
---

# 🕵️ Backend Interview Auditor — Skill Soi Dự Án & Tạo Bộ Câu Hỏi Phỏng Vấn Thực Chiến

## 🎯 Mục Tiêu
Skill này dùng để biến AI thành một **Technical Architect / Senior .NET Interviewer khó tính**, trực tiếp đọc và soi từng file code trong dự án `dotnet-backend-core-main/`, đối chiếu với tài liệu `dotnet-project-knowledge/` và `prepare-interview/study-notes/`. Mục tiêu cuối cùng là giúp ứng viên **hiểu sâu sắc 100% mọi dòng code và quyết định kiến trúc trong dự án**, không học lý thuyết lan man.

---

## 🧭 Nguyên Tắc Phỏng Vấn (Zero Lan Man)
1. **Chỉ hỏi những gì CÓ trong dự án**: Mọi câu hỏi và kịch bản bắt bẻ phải chỉ đích danh file, folder hoặc pattern đang dùng (`Features/...`, `IAppDbContext`, `ILlmClient`, `NetArchTest`, `Result<T>`).
2. **Quy tắc 3 tầng đào sâu (3-Level Deep Dive)**:
   - **Level 1 (Bản chất & Lý do)**: "Tại sao em lại chọn cách này mà không dùng cách phổ biến?" (vd: Tại sao không dùng MediatR, không dùng AutoMapper, không dùng Generic Repository?).
   - **Level 2 (Trade-off & Rủi ro)**: "Tự viết thì em phải đánh đổi gì? Khi tải cao hoặc hệ thống mở rộng thì điểm gãy (bottleneck) nằm ở đâu?"
   - **Level 3 (Bắt bẻ trực tiếp trên code)**: "Nhìn vào đoạn code X trong file Y, nếu có 100 request đồng thời gọi tới thì có bị rò rỉ bộ nhớ / xung đột DbContext / lỗi concurrency không? Xử lý exception thế nào?"

---

## 📂 Phạm Vi Soi Code Theo 4 Trụ Cột

### 1. API Layer & Cross-Cutting Concerns
- **Vị trí code**: `src/TourAgent.Api/`
- **Nội dung soi**:
  - Minimal APIs vs Controller: Cách mapping endpoint và inject thẳng Handler.
  - FluentValidation tích hợp ở đâu? Có validate trước khi vào handler không?
  - Xử lý lỗi tập trung: Dùng `Result<T>` trả về HTTP Status Code (400, 404, 500) như thế nào? Có dùng Global Exception Handler Middleware không?
  - Cơ chế truyền `CancellationToken` xuyên suốt từ HTTP request xuống DB và LLM.

### 2. CQRS & Vertical Feature Folders (No MediatR, No AutoMapper)
- **Vị trí code**: `src/TourAgent.Application/Features/`
- **Nội dung soi**:
  - Cấu trúc Feature Folder: `{Name}Command`, `{Name}Query`, `{Name}Handler`, `{Name}Validator`, `{Name}Response`.
  - Không dùng MediatR: Tự viết Pipeline behavior (Logging, Validation, Transaction) bằng cách nào? Trade-off khi cần publish domain events cho nhiều handler?
  - Không dùng AutoMapper: Extension method mapping (`ToResponse()`) có ưu thế gì về compile-time, zero-reflection, benchmark RAM so với AutoMapper/Mapster?
  - Architecture Tests: Thư viện `NetArchTest.Rules` trong `tests/TourAgent.ArchitectureTests` chặn xói mòn kiến trúc thế nào?

### 3. EF Core & Database Persistence (No Generic Repository)
- **Vị trí code**: `src/TourAgent.Domain/`, `src/TourAgent.Infrastructure/Persistence/`
- **Nội dung soi**:
  - Tại sao dùng `IAppDbContext` trực tiếp thay vì bọc thêm lớp Generic Repository?
  - `AsNoTracking()` trong Query: Tại sao bắt buộc dùng? Nếu quên thì hậu quả gì ở môi trường tải cao?
  - Rich Domain Model vs Anemic Domain Model: Private setters, Static factory method `Create()`, bảo vệ Invariants trước khi lưu DB.
  - Concurrency & Transactions: Xử lý conflict khi 2 agent cùng cập nhật trạng thái đơn tour/khách hàng.

### 4. AI Agent, LLM Client & Tool Calling
- **Vị trí code**: `src/TourAgent.Application/Agent/`, `src/TourAgent.Infrastructure/Llm/`
- **Nội dung soi**:
  - Abstraction `ILlmClient` & DIP: Tách biệt thế nào với OpenAI / DeepSeek / Ollama?
  - Zero Hallucination: Ràng buộc Tool Calling chỉ lấy giá và lịch trình từ Database. Nếu LLM hallucinate ra thông tin không có trong DB thì chặn ở đâu?
  - Context & Token Management: Quản lý lịch sử hội thoại, cắt tỉa context, log tokens_in, tokens_out, latency.
  - Human Escalation: Khi nào Agent quyết định chuyển giao cho tư vấn viên con người?

---

## 🛠️ Quy Trình Thực Thi Của Skill
Khi được kích hoạt để tạo câu hỏi hoặc phỏng vấn thử:
1. **Bước 1**: Đọc các file code thực tế trong `src/` liên quan đến chủ đề được yêu cầu.
2. **Bước 2**: Liệt kê 3-5 câu hỏi phỏng vấn hóc búa nhất, chia theo các góc độ:
   - Câu hỏi khởi động (Concept & Architectural rationale)
   - Câu hỏi tình huống (Failure scenario & Concurrency)
   - Câu hỏi bắt bẻ code (Code review & Trade-off)
3. **Bước 3**: Cung cấp gợi ý trả lời chuẩn Senior (bám sát cấu trúc: *Bản chất -> Lý do chọn trong TourAgent -> Đánh đổi (Trade-off) -> Bằng chứng kiểm chứng/Unit test*).

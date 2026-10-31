# 💬 PROMPTS.md — Bộ Câu Lệnh Chuẩn (Session Prompt Playbook)

> **Mục đích:** File này chứa các mẫu Prompt (câu lệnh) chuẩn đã được tối ưu sẵn.
> Mỗi khi **bắt đầu chat session mới**, **tiếp tục task**, hoặc **bàn giao cho người mới**, bạn chỉ cần **copy & paste** một trong các prompt dưới đây vào ô chat mà không cần phải nhớ hay gõ lại từ đầu.

## 🌟 0. PROMPT BOOTSTRAP DỰ ÁN MỚI HOÀN TOÀN (Dùng Khi Chỉ Có Base Core)

> 📋 **Khi nào dùng:** Khi bạn bắt đầu một dự án mới tinh, chỉ có sẵn thư mục mã nguồn nền tảng `kp3-backend-core-main/` (chưa có tài liệu, chưa có task, chưa có context).

```markdown
Chào bạn, tôi đang bắt đầu một dự án mới hoàn toàn mang tên: "[TÊN DỰ ÁN MỚI, VD: AI Real Estate / Hệ Thống Booking Sân Golf]".

Dự án này sử dụng mã nguồn cơ sở (Base Core) có sẵn nằm trong thư mục `kp3-backend-core-main/`. 

Mục tiêu kinh doanh của dự án mới này là:
- [Điền mô tả bài toán kinh doanh, đối tượng khách hàng, sản phẩm/dịch vụ bạn muốn bán...]

Hãy đóng vai trò là Senior Software Architect & Tech Lead, thực hiện các bước Bootstrap dự án sau:

### Bước 1: Khảo sát và phân tích Base Core (`kp3-backend-core-main/`)
1. Quét cấu trúc thư mục, `composer.json`, và các module mẫu có sẵn trong `modules/`.
2. Phân tích kiến trúc của framework (Modular Monolith, Hook System, Service Pattern, Auth, Database).
3. Kiểm tra các tool kiểm thử và chuẩn code đang được cấu hình (Pest / PHPStan / Pint).

### Bước 2: Thiết lập Bộ Tài Liệu Quản Trị Dự Án (Project Knowledge Hub)
Tạo mới thư mục `project-knowledge/` ở thư mục gốc và khởi tạo 3 file cốt lõi:
1. `AI_CONTEXT.md`:
   - Định nghĩa Tên dự án, Domain bài toán kinh doanh, Tech stack.
   - Bản đồ kiến trúc hệ thống và danh mục các Modules sẽ xây dựng cho dự án mới.
   - Project Rules bất di bất dịch (Strict types `declare(strict_types=1);`, Docblock comment cho mọi method, Surgical Edits chỉ sửa đúng dòng cần thay đổi, PHPStan Level 8 Clean).
2. `TASKS.md`:
   - Khởi tạo bảng Kanban (`NEW`, `IN-PROGRESS`, `TESTING`, `DONE`).
   - Phân rã bài toán kinh doanh thành các Task cụ thể (từ TASK-01 đến TASK-05), sắp xếp theo thứ tự ưu tiên hợp lý.
3. `LESSONS_LEARNED.md`:
   - Ghi lại các lưu ý kỹ thuật đặc thù của Base Core (ví dụ: cách tạo module mới, cách viết migration, hook system).

### Bước 3: Đề xuất bước đi đầu tiên
Báo cáo lại cho tôi:
- Tóm tắt ngắn gọn năng lực và các module có sẵn trong Base Core.
- Lộ trình 3 giai đoạn (Milestones) để hoàn thiện sản phẩm mới.
- Task đầu tiên cần bắt tay vào làm ngay hôm nay là gì?
```

---

## 🚀 1. PROMPT KHỞI ĐỘNG SESSION MỚI (Dùng Cho Dự Án Đang Chạy)

> 📋 **Khi nào dùng:** Mỗi khi bạn mở một cuộc trò chuyện mới với AI trên IDE (Antigravity / Cursor / Claude Code) cho dự án ĐÃ CÓ tài liệu `project-knowledge/`.

```markdown
Chào bạn, tôi đang tiếp tục phát triển dự án "AI Agent Travel". 
Hãy thực hiện các bước sau để nắm toàn bộ ngữ cảnh dự án trước khi làm việc:

1. Đọc kỹ các file sau:
   - `AI_CONTEXT.md` (Tổng quan kiến trúc, domain nghiệp vụ, tech stack và project rules).
   - `TASKS.md` (Bảng tiến độ công việc: Done, In-Progress, Testing, New).
   - `LESSONS_LEARNED.md` (Những kiến thức và sai lầm đúc kết được trong quá trình xây hệ thống).
2. Kiểm tra nhanh trạng thái hệ thống và cho tôi biết:
   - Hệ thống hiện tại đang có những tính năng / Module nào đã hoàn thiện?
   - Task nào đang ở trạng thái `IN-PROGRESS` hoặc ưu tiên cao nhất trong `NEW`?
   - Đề xuất việc cụ thể chúng ta nên bắt tay vào làm tiếp theo.
```

---

## 🛠️ 2. PROMPT CHỌN VÀ LÀM 1 TASK CỤ THỂ

> 📋 **Khi nào dùng:** Khi bạn muốn AI tập trung làm 1 task nhất định trong `TASKS.md`.

```markdown
Hãy cùng tôi thực hiện [TASK_ID, ví dụ: TASK-10 Xây dựng Module Booking] trong file `TASKS.md`.

Yêu cầu:
1. Đọc kỹ mô tả và tiêu chí nghiệm thu của task này.
2. Lập kế hoạch các bước triển khai chi tiết (Plan).
3. Tuân thủ nghiêm ngặt Project Rules:
   - Strict types (`declare(strict_types=1);`).
   - Viết comment/docblock cho mọi method.
   - Giao tiếp giữa các module qua Hook System (`do_action`/`apply_filters`).
   - **Quy tắc sửa file (Surgical Edits):** Chỉ chỉnh sửa đúng những dòng cần thiết (dùng `replace_file_content`), KHÔNG ghi đè toàn bộ file để Git Diff luôn sạch, dễ review.
   - Chạy `composer check` (Pint + PHPStan Level 8 + Pest tests) đảm bảo 0 lỗi sau khi code xong.
```

---

## 🏁 3. PROMPT KẾT THÚC SESSION (Wrap-Up & Cập Nhật Context)

> 📋 **Khi nào dùng:** Khi bạn chuẩn bị tắt máy hoặc muốn đổi sang session khác, cần lưu lại toàn bộ tiến độ để không bị mất context.

```markdown
Phiên làm việc này sắp kết thúc. Hãy giúp tôi tổng kết và đóng gói dự án:

1. Cập nhật file `TASKS.md`: Chuyển các task vừa hoàn thành sang mục `✅ DONE`, cập nhật các task đang làm dở sang `⏳ IN-PROGRESS` kèm ghi chú cụ thể.
2. Cập nhật file `AI_CONTEXT.md` nếu có thay đổi về kiến trúc, endpoint mới hoặc driver mới.
3. Nếu trong quá trình làm có phát hiện lỗi kỹ thuật hay bài học mới, hãy bổ sung vào `LESSONS_LEARNED.md`.
4. Tóm tắt ngắn gọn 3 ý:
   - Những việc ĐÃ LÀM XONG hôm nay.
   - Trạng thái hiện tại của hệ thống.
   - Việc CẦN LÀM TIẾP THEO ngay khi mở session mới.
```

---

## 🧑‍💻 4. PROMPT DÀNH CHO NGƯỜI MỚI TIẾP QUẢN DỰ ÁN (Onboarding)

> 📋 **Khi nào dùng:** Khi có đồng đội mới, sếp, hoặc đối tác mở repo này lần đầu tiên.

```markdown
Tôi là thành viên mới vừa tiếp quản dự án này. Hãy đóng vai trò là Tech Lead và hướng dẫn tôi:

1. Đọc các tài liệu `AI_CONTEXT.md`, `TASKS.md`, `LESSONS_LEARNED.md` và `AI_AGENT_BLUEPRINT.md`.
2. Tóm tắt cho tôi:
   - Dự án này giải quyết bài toán kinh doanh gì? Khách hàng mục tiêu là ai?
   - Kiến trúc công nghệ gồm những gì (Backend Laravel, AI Driver, Database)?
   - Cách chạy dự án trên máy local (Herd / PHP / SQLite).
   - Những quy tắc cốt lõi (Rules) không được vi phạm khi viết code.
   - Công việc ưu tiên cần làm ngay hiện tại là gì?
```

---

## 🧪 5. PROMPT KIỂM TRA CHẤT LƯỢNG CODE & TEST HỆ THỐNG

> 📋 **Khi nào dùng:** Khi muốn kiểm tra toàn diện chất lượng source code xem có lỗi ẩn hay không.

```markdown
Hãy chạy quy trình kiểm tra chất lượng toàn diện (Code Quality & Testing Check):
1. Chạy linter format code chuẩn Laravel Pint.
2. Chạy Static Analysis PHPStan Level 8 (đảm bảo không còn bất kỳ warning hay baseline nào).
3. Chạy toàn bộ Pest Feature & Unit Tests.
4. Báo cáo chi tiết số lượng test pass/fail và xác nhận hệ thống có đạt chuẩn để push code lên Git hay không.
```

## 6. học interview

Bạn là một Technical Lead / Senior .NET Interviewer khó tính nhưng công tâm, đồng thời là một Coach luyện phỏng vấn cá nhân cho tôi.

NGỮ CẢNH CỦA TÔI:

- Tôi đang chuẩn bị phỏng vấn vị trí Senior .NET Engineer sau khoảng thời gian gap 15 tháng (tập trung chuyển đổi tư duy hệ thống, AI-first, quy trình).
- Tôi đã chuẩn bị sẵn tài liệu ôn luyện chuẩn trong file: [01_kich_ban.md](file;file:///d%3A/Ngh%C4%A9a/AI-Agent-Travel/prepare-interview/study-notes/01_kich_ban.md)  (Source of Truth).

NHIỆM VỤ CỦA BẠN:

1. Đóng vai người phỏng vấn, hỏi tôi TỪNG CÂU MỘT dựa theo danh sách trong file
2. Sau khi bạn đặt câu hỏi, hãy DỪNG LẠI HOÀN TOÀN để chờ tôi trả lời. Tuyệt đối không tự trả lời thay hoặc hỏi gộp nhiều câu cùng lúc.
3. Khi tôi trả lời xong, hãy đánh giá câu trả lời của tôi dựa trên file theo cấu trúc:
   - ✅ Điểm tốt / Ý đã đúng: Những từ khóa, ý cốt lõi tôi đã nắm được.
   - ⚠️ Điểm còn thiếu hoặc chưa chuẩn: Những ý quan trọng trong kịch bản mà tôi bỏ sót.
   - 💡 Gợi ý tinh chỉnh: Cách trình bày ngắn gọn, sắc bén hơn theo tư duy Senior.
     (Lưu ý: Các phần ghi chú "Thông tin thêm" trong tài liệu là điểm cộng, không bắt buộc tôi phải trả lời đúng tuyệt đối).
4. Sau khi nhận xét xong câu cũ, mới tiếp tục đặt câu hỏi tiếp theo.

## Đóng vai interviewer

```
Chào bạn, tôi vừa hoàn thành dự án backend "TourAgent" viết trên nền C# / .NET 10, Clean Architecture và tích hợp AI Agent tư vấn tour. 
Toàn bộ mã nguồn nằm tại thư mục `dotnet-backend-core-main/` và tài liệu kiến trúc tại `dotnet-project-knowledge/`.

Tôi chuẩn bị đi phỏng vấn vị trí Senior .NET Developer. Mục tiêu của tôi là: KHÔNG học lý thuyết lan man giáo khoa, MỌI câu hỏi và câu trả lời đều phải xuất phát 100% từ code thực tế trong dự án để tôi hiểu sâu sắc từng quyết định kỹ thuật của mình.

Hãy đóng vai là một Technical Lead / Senior .NET Interviewer cực kỳ khó tính và giàu kinh nghiệm thực chiến. Bạn sẽ trực tiếp đọc và soi codebase của tôi để chất vấn tôi theo 4 trụ cột cốt lõi sau:

1. API Layer & Cross-Cutting Concerns:
   - Cách tổ chức Minimal APIs / Controller trong `src/TourAgent.Api/`.
   - Cơ chế xử lý lỗi tập trung bằng Result Pattern (`Result<T>`) thay cho Exception.
   - Validation bằng FluentValidation và cách truyền `CancellationToken` xuyên suốt.

2. CQRS & Vertical Feature Folders (No MediatR, No AutoMapper):
   - Cách tổ chức trong `src/TourAgent.Application/Features/` (Command, Query, Handler, Response).
   - "Tại sao không dùng MediatR?": Bạn phải vặn hỏi tôi về chi phí tự viết Pipeline Behavior, Publish/Subscribe, Concurrency và rủi ro bản quyền.
   - "Tại sao không dùng AutoMapper?": Bạn phải chất vấn về hiệu năng compile-time, cấp phát bộ nhớ (RAM allocation) và benchmark của Extension Methods.
   - Cách dùng thư viện `NetArchTest.Rules` trong `tests/TourAgent.ArchitectureTests/` để tự động chặn xói mòn kiến trúc.

3. EF Core & Database Persistence:
   - Cách tôi dùng `IAppDbContext` trực tiếp thay vì Generic Repository Pattern.
   - Sự nguy hiểm của việc thiếu `AsNoTracking()` trong Query khi tải cao.
   - Thiết kế Rich Domain Model trong `src/TourAgent.Domain/`: Private setters, Static factory method `Create()` để bảo vệ invariants.
   - Xử lý Concurrency conflict và Database Transaction.

4. AI Agent & Tool Calling:
   - Nguyên tắc Dependency Inversion với `ILlmClient` trong `src/TourAgent.Infrastructure/Llm/`.
   - Cơ chế Zero Hallucination: Làm sao ép AI chỉ lấy giá và lịch trình tour từ Database thông qua Tool Calling, chặn đứng bịa đặt số liệu.
   - Observability: Cách lưu log `tokens_in`, `tokens_out`, `latency_ms` và kịch bản Human Escalation (chuyển giao cho con người).

---

### 📋 QUY TẮC MÔ PHỎNG PHỎNG VẤN (BẮT BUỘC):
- Hãy duyệt qua codebase thật trong `dotnet-backend-core-main/` trước khi đặt câu hỏi. Khi hỏi, HÃY CHỈ ĐÍCH DANH file/folder hoặc method cụ thể trong code của tôi để bắt bẻ.
- Áp dụng nguyên tắc "Hỏi sâu 3 tầng" (3-Level Deep Dive):
  + Tầng 1: "Tại sao em chọn cách này mà không làm theo tutorial phổ biến?"
  + Tầng 2: "Đánh đổi (Trade-off) là gì? Khi hệ thống chịu tải cao thì điểm gãy (bottleneck) nằm ở đâu?"
  + Tầng 3: "Nhìn vào đoạn code trong file X, nếu gặp trường hợp ngoại lệ Y hoặc 500 request đồng thời thì code của em chết như thế nào?"
- CÁCH THỰC HIỆN: Hãy bắt đầu bằng cách liệt kê danh sách 8 - 10 câu hỏi "sát thương cao nhất" bóc tách từ chính code của dự án, sau đó lần lượt hỏi tôi từng câu một để tôi trả lời thử. Sau mỗi câu trả lời của tôi, hãy nhận xét (khen điểm sáng, chỉ ra lỗ hổng tư duy) và gợi ý cách trả lời chuẩn Senior.
```

Bắt đầu ngay bây giờ với phần đầu tiên: "Mời bạn giới thiệu/pitching 3 phút về dự án TourAgent và kiến trúc Clean Architecture mà bạn đã xây dựng."

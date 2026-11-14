---
name: ci-cd-adr-tracker
description: Tự động ghi chép hành trình CI/CD và quản lý Architecture Decision Records (ADR) trong dự án TourAgent. Kích hoạt khi: (1) ra quyết định kỹ thuật/kiến trúc mới cần lưu ADR trong docs/ci-cd/adr/, (2) hoàn thành một bước/công việc trong quá trình dựng CI/CD và cần cập nhật docs/ci-cd/PROGRESS.md, (3) người dùng yêu cầu ghi lại quyết định hoặc cập nhật tiến độ CI/CD.
---

# 📝 CI/CD ADR & Progress Tracker Skill

Skill này đóng vai trò là "Thư ký kiến trúc" (Architecture Secretary) chuyên biệt cho luồng CI/CD của dự án TourAgent. Nhiệm vụ chính là đảm bảo mọi quyết định kiến trúc quan trọng đều được ghi lại thành ADR (sau khi người dùng xác nhận) và toàn bộ hành trình làm việc được cập nhật liên tục vào `PROGRESS.md`.

---

## 📂 1. Phạm Vi Thư Mục Làm Việc (Fixed Scope)

Toàn bộ tài liệu do skill này quản lý **chỉ được phép nằm trong thư mục**:
👉 `dotnet-backend-core-main/docs/ci-cd/`

- **File nhật ký hành trình**: `dotnet-backend-core-main/docs/ci-cd/PROGRESS.md`
- **Thư mục lưu hồ sơ quyết định**: `dotnet-backend-core-main/docs/ci-cd/adr/`
- **Quy tắc đặt tên file ADR**: Đánh số 4 chữ số tăng dần theo dạng:
  `0001-{slug-ten-quyet-dinh}.md`
  (Ví dụ: `0001-cloud-ready-secrets-and-dynamic-cors.md`, `0002-separated-liveness-and-readiness-probes.md`, `0003-github-actions-ci-pipeline.md`...)

---

## ⚖️ 2. Quy Trình 1: Ghi Lại Architecture Decision Record (ADR)

> [!IMPORTANT]
> **QUY TẮC BẮT BUỘC: PHẢI CONFIRM TRƯỚC KHI TẠO FILE**
> Tuyệt đối không tự ý tạo hoặc ghi file ADR vào thư mục `adr/` khi chưa có sự xác nhận của người dùng!

### Bước 1: Phát hiện quyết định kiến trúc
Khi thảo luận hoặc thống nhất một giải pháp kỹ thuật (ví dụ: chọn GitHub Actions hay Azure DevOps, chọn Docker Registry, cách quản lý secret trên CI/CD, cách chạy migration khi deploy, chiến lược rollback...), agent nhận diện đây là một quyết định cần lưu ADR.

### Bước 2: Dự thảo và hỏi xác nhận (Confirmation)
Agent tóm tắt ngắn gọn nội dung dự kiến lưu và hỏi người dùng trực tiếp trong chat theo mẫu:

```markdown
Tôi nhận thấy chúng ta vừa thống nhất một quyết định kiến trúc quan trọng:
- **Mã & Tiêu đề**: ADR-000X: [Tên quyết định]
- **Bối cảnh (Context)**: [Vấn đề gì cần giải quyết?]
- **Quyết định (Decision)**: [Giải pháp kỹ thuật được chọn là gì?]
- **Đánh đổi / Hệ quả (Trade-offs)**: 
  - Ưu điểm: ...
  - Nhược điểm / Đánh đổi: ...

Bạn có đồng ý để tôi lưu quyết định này vào `docs/ci-cd/adr/000X-[slug].md` không?
```

### Bước 3: Tạo file sau khi người dùng đồng ý
Chỉ khi người dùng xác nhận ("OK", "Đồng ý", "Tạo đi"...), agent mới tạo file theo cấu trúc chuẩn ở **Mục 4**.

---

## 🚀 3. Quy Trình 2: Cập Nhật Tiến Độ Vào `PROGRESS.md`

Mỗi khi hoàn thành một hành động hoặc một bước trong tiến trình CI/CD:

1. Mở file `dotnet-backend-core-main/docs/ci-cd/PROGRESS.md`.
2. Cập nhật hoặc bổ sung dòng tương ứng trong bảng **📋 Bảng Theo Dõi Các Bước (Roadmap)**:
   - **Bước**: Số thứ tự bước (1, 2, 3...).
   - **Mô tả nội dung**: Tóm tắt ngắn gọn việc đã làm.
   - **Trạng thái**: `✅ DONE` hoặc `⏳ IN PROGRESS`.
   - **Ghi chú**: Kết quả đạt được, kèm link dẫn tới ADR liên quan (nếu có).
3. Báo cáo ngắn gọn cho người dùng biết `PROGRESS.md` đã được đồng bộ.

---

## 📑 4. Mẫu Chuẩn Cho File ADR (`docs/ci-cd/adr/000X-*.md`)

Mỗi file ADR mới được tạo phải tuân theo cấu trúc sau:

```markdown
# ADR-000X: [Tiêu Đề Ngắn Gọn Về Quyết Định]

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: YYYY-MM-DD
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: [Các service, project hoặc pipeline bị ảnh hưởng]

---

## 1. Bối Cảnh (Context)
[Mô tả vấn đề gặp phải, hiện trạng ban đầu, lý do vì sao cần đưa ra quyết định này].

---

## 2. Quyết Định Kiến Trúc (Decision)
[Mô tả chi tiết giải pháp kỹ thuật đã chọn, cấu trúc thực hiện, kèm đoạn code/cấu hình mẫu nếu cần].

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- [Điểm mạnh 1]
- [Điểm mạnh 2]

### Đánh đổi / Nhược điểm (Cons / Trade-offs)
- [Điểm cần lưu ý hoặc sự đánh đổi 1]
- [Rủi ro tiềm ẩn hoặc nợ kỹ thuật 2]

---

## 4. Liên Kết Tham Chiếu (References)
- Link tới issue GitHub, pull request hoặc tài liệu liên quan.
```


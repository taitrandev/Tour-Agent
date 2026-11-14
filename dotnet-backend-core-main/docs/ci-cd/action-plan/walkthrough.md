# Walkthrough: Hoàn Thiện Tái Cấu Trúc Skill `build-ci-cd` & Sửa Lỗi Thực Tế

Toàn bộ các đề xuất trong kế hoạch review đã được thực thi và kiểm chứng thực tế trên mã nguồn, container và GitHub Issue.

---

## 1. Những Việc Đã Hoàn Thành

### A. Tái Cấu Trúc Toàn Diện Skill `build-ci-cd`
- **Frontmatter chuẩn**: Thêm `description` với đầy đủ điều kiện kích hoạt (triggers) để Agent ở bất kỳ chat session mới nào cũng nhận diện được khi nào cần dùng skill.
- **Áp dụng Progressive Disclosure**: Tách biệt rõ ràng nội dung điều phối chung khỏi chi tiết kỹ thuật:
  - [`SKILL.md`](file:///d:/github/Tour-Agent/.agents/skills/build-ci-cd/SKILL.md): Giữ nguyên cách đánh số (Giai đoạn 0 ➔ 4), loại bỏ checkbox rác và mẹo phỏng vấn.
  - [`references/phase0-cloud-ready.md`](file:///d:/github/Tour-Agent/.agents/skills/build-ci-cd/references/phase0-cloud-ready.md): Cẩm nang chi tiết các bước kiểm tra, lệnh quét secret (`git grep`, `git log -S`) và tiêu chí Definition of Done độc lập ngôn ngữ.
  - [`references/dotnet-azure.md`](file:///d:/github/Tour-Agent/.agents/skills/build-ci-cd/references/dotnet-azure.md): Code mẫu .NET thực chiến (CORS, Probes, Dockerfile multi-stage, quy tắc ánh xạ `__`).
  - [`references/deployment-checklist-template.md`](file:///d:/github/Tour-Agent/.agents/skills/build-ci-cd/references/deployment-checklist-template.md): Mẫu checklist cấu hình Cloud có thể copy sang bất kỳ dự án mới nào.
- **Tách Mẹo Phỏng Vấn**: Đưa toàn bộ kiến thức về Docker Layer Caching, Non-root user, User Secrets sang [`prepare-interview/study-notes/05_QuaTrinh_CICD.md`](file:///d:/github/Tour-Agent/prepare-interview/study-notes/05_QuaTrinh_CICD.md).

---

### B. Sửa Triệt Để Lỗi Thực Tế Trong Mã Nguồn Dự Án

1. **Sửa lỗi ưu tiên CORS**:
   - `appsettings.json`: Đặt `"Cors:AllowedOrigins": []`.
   - `appsettings.Development.json`: Giữ mảng localhost để phục vụ dev máy cục bộ.
   - `Program.cs`: Đổi thứ tự ưu tiên: `envOrigins` (`CORS_ALLOWED_ORIGINS`) ➔ `configuredOrigins` ➔ fallback localhost.
   - **Xác minh thực tế trên Docker**: Chạy container với `-e CORS_ALLOWED_ORIGINS="https://tour-agent.vercel.app"`, gửi request `Origin` giả lập ➔ Header trả về chính xác `Access-Control-Allow-Origin: https://tour-agent.vercel.app`.

2. **Tách biệt Liveness và Readiness Probes**:
   - `/health/live`: Chỉ kiểm tra nội tại tiến trình (`self`), trả về HTTP 200 nhanh trong vài mili-giây, không gọi ra ngoài. Đây là probe an toàn để Azure App Service giám sát restart container.
   - `/health/ready`: Kiểm tra PostgreSQL và DeepSeek AI.
   - `DeepSeekHealthCheck.cs`: Đổi toàn bộ trạng thái lỗi/timeout sang `Degraded` (thay vì `Unhealthy`) để app chính không bị kill nhầm khi mạng ngoài gặp sự cố.
   - Ẩn chi tiết nội bộ khi chạy ở môi trường `Production`.

3. **Ghi nhận nợ kỹ thuật (Technical Debt)**:
   - Đã tạo **Issue #2** trên GitHub: [[Tech Debt] Chuyển đổi lưu trữ TelegramSettings & LlmSettings từ file JSON sang Database (Stateless 12-Factor)](https://github.com/taitrandev/Tour-Agent/issues/2).

---

## 2. Kết Quả Kiểm Thử (Verification Results)

| Bài Kiểm Tra | Lệnh Thực Hiện | Kết Quả |
| :--- | :--- | :--- |
| **Unit & Arch Tests** | `dotnet test ./dotnet-backend-core-main/TourAgent.slnx` | **87/87 tests PASSED** |
| **Docker Build** | `docker build -t touragent-api:local ...` | **Success** (Tận dụng Layer Cache `RUN dotnet restore CACHED`) |
| **Liveness Probe** | `GET http://localhost:8080/health/live` | `{"status": "Healthy"}` (HTTP 200) |
| **Readiness Probe** | `GET http://localhost:8080/health/ready` | `{"status": "Degraded"}` (Xử lý mượt mà khi thiếu key DeepSeek) |
| **CORS Override** | `Invoke-WebRequest -Headers @{Origin="https://tour-agent.vercel.app"}` | `Access-Control-Allow-Origin: https://tour-agent.vercel.app` |

---

> [!IMPORTANT]
> **Hành động thủ công cần bạn thực hiện**:
> Vì DeepSeek API Key và Telegram Bot Token đã từng xuất hiện trong lịch sử commit `e8a6550`, hãy đăng nhập vào trang quản trị của DeepSeek và Telegram (@BotFather) để **thu hồi (revoke) key cũ và tạo key mới**, sau đó cập nhật lại vào `secrets.json` trên máy của bạn.

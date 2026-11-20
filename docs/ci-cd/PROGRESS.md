# PROGRESS — Trạng Thái quá trình build CI CD

**File này lưu trữ trạng thái hiện tại và tiến độ công việc giữa các session làm việc.**

---

## 📋 Bảng Theo Dõi Các Bước (Roadmap)

|   Bước   | Mô tả nội dung                                                                        | Trạng thái | Ghi chú                                                     |
| :---------: | :--------------------------------------------------------------------------------------- | :----------: | :----------------------------------------------------------- |
| **1** | Phase 0 Step 1: Dọn dẹp Secret & Hardcoded API Key khỏi code và config               |   ✅ DONE   | Trừu tượng hóa qua Env Vars                              |
| **2** | Phase 0 Step 2: Cấu hình CORS động (`CORS_ALLOWED_ORIGINS`) & Telegram WebhookUrl  |   ✅ DONE   | Hỗ trợ Cloud Deploy                                        |
| **3** | Phase 0 Step 3: Tích hợp ASP.NET Core Health Checks (`/health`) cho DB & DeepSeek AI |   ✅ DONE   | Endpoint `/health` kiểm tra song song DB và DeepSeek API |
| **4** | Phase 0 Step 4: Dockerfile Multi-stage .NET 10 & .dockerignore                           |   ✅ DONE   | Image size chỉ 102MB, non-root user, layer cache tối ưu   |
| **5** | Phase 1 Step 1: Thống nhất thiết kế CI & Nguyên lý "Build Once, Deploy The Same Artifact" |   ✅ DONE   | [ADR-0003](adr/0003-build-once-push-acr-in-ci.md) Push ACR ở CI khi merge `main` |
| **6** | Phase 1 Step 2: Cấu hình OIDC & User-Assigned Managed Identity kết nối GitHub Actions      |   ✅ DONE   | Thiết lập Federated Credential & quyền `AcrPush` / `Container Apps Contributor` |
| **7** | Phase 1 Step 3: Tạo workflow GitHub Actions CI (`.github/workflows/backend-ci.yml`)       |   ✅ DONE   | Pipeline chạy 1m 51s: Build, Unit Test, OIDC Login & Push ACR thành công tag SHA |
| **8** | Phase 2 Step 1: Di chuyển cơ sở dữ liệu lên Cloud PostgreSQL (Supabase)                   |   ✅ DONE   | [ADR-0004](adr/0004-cd-azure-container-apps-and-supabase.md) Chuỗi kết nối SSL, auto-seed |
| **9** | Phase 2 Step 2: Soạn thảo tài liệu Day-0 Provisioning Azure Container Apps                |   ✅ DONE   | Hướng dẫn chi tiết Portal & CLI tại [create-azure-container-app.md](create-azure-container-app.md) |
| **10**| Phase 2 Step 3: Cập nhật CD Pipeline trong `.github/workflows/backend-ci.yml`             |   ✅ DONE   | Thêm job `deploy` cập nhật image SHA, Smoke test `/health/ready` với `--retry-all-errors` |
| **11**| Phase 2 Step 4: Khởi tạo Azure Container App bằng script & Đúc kết bài học kinh nghiệm    |   ✅ DONE   | App chạy thông suốt, đúc kết bài học tại [lesson-learned.md](lesson-learned.md) |
| **12**| Phase 2 Step 5: Nạp biến GitHub (`ACA_NAME`, `AZURE_RG`) và chạy thử CD Pipeline tự động  | ⏳ IN PROGRESS | Kiểm thử deploy tự động khi push code lên `main` |

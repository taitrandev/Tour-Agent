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
| **7** | Phase 1 Step 3: Tạo workflow GitHub Actions CI (`.github/workflows/backend-ci.yml`)       |   ✅ DONE   | Pipeline chạy 1m 51s: Build, Unit Test, OIDC Login & Push ACR thành công tag SHA & latest |

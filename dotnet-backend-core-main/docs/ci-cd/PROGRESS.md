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

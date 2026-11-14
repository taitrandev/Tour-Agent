# 📋 AZURE DEPLOYMENT CHECKLIST & CONFIGURATION GUIDE

> **Mục đích**: Lưu trữ toàn bộ danh sách biến môi trường, checklist các bước thực hiện và lưu ý quan trọng khi triển khai TourAgent Backend lên Azure App Service. File này giúp bạn và AI ở các session sau không bao giờ bị quên hoặc cấu hình sai.

---

## 🔑 1. Danh Sách Biến Môi Trường Cần Cấu Hình Trên Azure

Khi deploy lên Azure App Service, bạn vào mục:
👉 **Azure Portal -> App Service của bạn -> Configuration -> Application settings** (hoặc **Environment variables**) và thêm các cặp Key - Value sau:

| Tên Biến (Key trên Azure) | Ý nghĩa / Mô tả | Giá trị mẫu |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Môi trường chạy | `Production` |
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối tới Database PostgreSQL trên Cloud | `Host=xxx.postgres.database.azure.com;Port=5432;Database=tour_agent_db;Username=admin;Password=YourSecurePassword;Ssl Mode=Require;` |
| `CORS_ALLOWED_ORIGINS` | Danh sách domain Frontend được phép gọi API (phân tách bởi dấu phẩy) | `https://touragent.vercel.app,https://admin.yourdomain.com` |
| `DEEPSEEK_API_KEY` | API Key của DeepSeek AI | `sk-xxxxxxxxxxxxxxxxxxxxxxxx` |
| `DEEPSEEK_MODEL` | Model AI sử dụng | `deepseek-reasoner` (hoặc `deepseek-chat`) |
| `TELEGRAM_BOT_TOKEN` | Token Bot Telegram từ @BotFather | `123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ` |
| `TELEGRAM_WEBHOOK_URL` | URL công khai của Backend trên Azure để Telegram bắn webhook | `https://<ten-app-service>.azurewebsites.net/api/v1/agent/webhook/telegram` |
| `TELEGRAM_ADMIN_CHAT_ID` | Telegram ChatId của Admin nhận cảnh báo | `5809306571` |

> [!NOTE]
> **Quy tắc phân tầng của ASP.NET Core (`__`)**:
> - Trên Linux / Azure, dấu `:` trong JSON config được thay bằng `__` (2 dấu gạch dưới).
> - Ví dụ: `ConnectionStrings:DefaultConnection` -> `ConnectionStrings__DefaultConnection`.
> - Riêng với CORS, code backend đã hỗ trợ sẵn biến môi trường phẳng `CORS_ALLOWED_ORIGINS` giúp bạn nhập danh sách URL cách nhau bằng dấu phẩy cực kỳ tiện lợi.

---

## 🚀 2. Checklist Các Bước Triển Khai Lên Azure (Step-by-Step)

### Giai đoạn 0: Chuẩn bị mã nguồn (Đã hoàn thành 100%)
- [x] **Step 1**: Dọn dẹp Secret & Hardcoded Fallback (`appsettings.json`, `LlmSettingsStore.cs`).
- [x] **Step 2**: Cấu hình CORS động ưu tiên `CORS_ALLOWED_ORIGINS` & WebhookUrl từ Environment Variables.
- [x] **Step 3**: Tích hợp Health Check Probes (tách biệt `/health/live` cho Azure restart probe và `/health/ready` kiểm tra PostgreSQL & DeepSeek AI).
- [x] **Step 4**: Viết Multi-stage `Dockerfile` (.NET 10) & `.dockerignore` (Đã build và test container thành công).

### Giai đoạn 1: Chuẩn bị hạ tầng trên Azure Portal
- [ ] Tạo **Resource Group** (ví dụ: `rg-touragent-prod`).
- [ ] Tạo **Azure App Service (Linux)**:
  - Runtime: `.NET 10` hoặc `Docker Container`.
  - Pricing Plan: `F1 (Free)` hoặc `B1 (Basic)`.
- [ ] Tạo **PostgreSQL Database**:
  - Dùng Azure Flexible Server PostgreSQL hoặc Cloud Postgres miễn phí (Neon / Supabase).
- [ ] Nhập toàn bộ 8 biến môi trường ở **Mục 1** vào Application Settings trên App Service.

### Giai đoạn 2: Thiết lập CI/CD Pipeline (GitHub Actions)
- [ ] Tạo **Publish Profile** hoặc **Azure Service Principal (OIDC)** từ App Service.
- [ ] Thêm Secret vào GitHub Repository (**Settings -> Secrets and variables -> Actions**):
  - `AZURE_WEBAPP_PUBLISH_PROFILE`
- [ ] Tạo file workflow `.github/workflows/deploy.yml`.
- [ ] Push code hoặc Merge Pull Request vào `main` để kích hoạt build và deploy tự động.

### Giai đoạn 3: Xác minh sau khi Deploy (Verification)
- [ ] Kiểm tra Health Check: `https://<ten-app>.azurewebsites.net/health` -> Phải trả về `Healthy` (HTTP 200).
- [ ] Kiểm tra Swagger UI (nếu bật ở Production hoặc test qua `/swagger`).
- [ ] Gọi thử 1 API từ Frontend (kiểm tra CORS có bị chặn không).
- [ ] Gửi thử tin nhắn qua Telegram Bot xem Webhook có kích hoạt không.

---

## 🛠️ 3. Các Lỗi Thường Gặp & Cách Khắc Phục (Troubleshooting)

1. **Lỗi `CORS error: No 'Access-Control-Allow-Origin' header`**:
   - *Nguyên nhân*: URL của Frontend trên Vercel/Netlify chưa được thêm vào `CORS_ALLOWED_ORIGINS`.
   - *Cách xử lý*: Thêm URL frontend vào biến `CORS_ALLOWED_ORIGINS` trên Azure Portal rồi nhấn **Save** (App Service sẽ tự restart).
2. **Lỗi `500 Internal Server Error` khi kết nối Database**:
   - *Nguyên nhân*: Thiếu cấu hình SSL hoặc chưa mở Firewall trên Azure PostgreSQL.
   - *Cách xử lý*: Đảm bảo chuỗi kết nối có `Ssl Mode=Require;Trust Server Certificate=true;` và đã bật `Allow public access from Azure services` trong tab Networking của Postgres.
3. **Lỗi `Telegram Webhook không nhận tin nhắn`**:
   - *Nguyên nhân*: URL `TELEGRAM_WEBHOOK_URL` chưa được cập nhật theo domain chính thức `.azurewebsites.net`.


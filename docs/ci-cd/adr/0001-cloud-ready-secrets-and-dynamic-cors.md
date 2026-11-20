# ADR-0001: Quản Lý Secrets Phân Tầng & Cấu Hình CORS Động Cho Cloud-Ready

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: 2026-11-12
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: `TourAgent.Api`, `TourAgent.Infrastructure`, Cấu hình triển khai Azure

---

## 1. Bối Cảnh (Context)

Khi chuẩn bị đưa dự án `TourAgent` (.NET 10) lên môi trường điện toán đám mây (Azure App Service) bằng pipeline CI/CD, mã nguồn ban đầu tồn tại các vấn đề vi phạm tiêu chuẩn **12-Factor App (Cloud-Native)**:

1. **Rò rỉ Secret & Chuỗi kết nối**:
   - `appsettings.json` bị hardcode chuỗi kết nối PostgreSQL local (`Host=localhost;Password=secretpassword`).
   - `LlmSettingsStore.cs` và `DeepSeekLlmClient.cs` có đoạn fallback cứng API Key DeepSeek dạng chuỗi plain-text (`sk-92e03f...`). Key này đã từng bị commit vào lịch sử Git (`e8a6550`).
2. **CORS bị fix cứng (Hardcoded CORS)**:
   - `Program.cs` cấu hình cứng `WithOrigins("http://localhost:5173", "http://localhost:3000")`. 
   - Khi Frontend SPA được deploy lên nền tảng Cloud độc lập (ví dụ Vercel/Netlify/Azure Static Web Apps), tên miền sẽ là `https://tour-agent.vercel.app`. Trình duyệt sẽ chặn đứng 100% API calls do vi phạm Same-Origin Policy nếu backend không cho phép domain này.
3. **Thứ tự ưu tiên cấu hình bị xung đột**:
   - Các lớp Store (`TelegramSettingsStore`, `LlmSettingsStore`) đọc từ file JSON cục bộ trước. Nếu file tồn tại, hệ thống bỏ qua biến môi trường Cloud, khiến việc cấu hình qua Azure Portal bị vô hiệu hóa.

---

## 2. Quyết Định Kiến Trúc (Decision)

### A. Quản Lý Secrets Phân Tầng Theo Chuẩn 12-Factor App
1. **Môi trường Git Repository & Production Base (`appsettings.json`)**:
   - Xóa bỏ toàn bộ mật khẩu, API key, webhook token và connection string thật.
   - Chỉ giữ lại các trường rỗng hoặc placeholder vô hại (`"DefaultConnection": ""`, `"ApiKey": ""`).
2. **Môi trường Development Cục Bộ**:
   - Sử dụng **`appsettings.Development.json`** để lưu chuỗi kết nối local phục vụ quá trình phát triển không bị gián đoạn.
   - Sử dụng **.NET Secret Manager (User Secrets)** với ID `<UserSecretsId>a7e61eaa-b039-4e2d-a891-7ed59e13e2a1</UserSecretsId>`. File `secrets.json` nằm tại `%APPDATA%\Microsoft\UserSecrets\`, hoàn toàn nằm ngoài Git tree.
3. **Môi trường Cloud (Production)**:
   - Vô hiệu hóa User Secrets.
   - Nạp cấu hình 100% qua **Biến Môi Trường (Environment Variables)** của Azure App Service với quy tắc phân tầng `__` (ví dụ `ConnectionStrings__DefaultConnection`, `DeepSeek__ApiKey`).
4. **Chính sách đối phó Secret đã lộ**:
   - Ghi nhận việc key cũ từng xuất hiện trong Git commit lịch sử. Yêu cầu bắt buộc phải thu hồi (revoke) và cấp mới (rotate) trên portal nhà cung cấp thay vì chỉ xóa trên Git.

### B. Cấu Hình CORS Động 3 Tầng Ưu Tiên
Trong `Program.cs`, thiết lập cơ chế nạp Origins theo thứ tự ưu tiên giảm dần:
1. **Tầng 1 (Ưu tiên cao nhất trên Cloud)**: Đọc từ biến môi trường `CORS_ALLOWED_ORIGINS` (hỗ trợ chuỗi phân tách bởi dấu phẩy, ví dụ `https://tour-agent.vercel.app,https://admin.domain.com`).
2. **Tầng 2**: Đọc mảng JSON `Cors:AllowedOrigins` từ `IConfiguration`.
3. **Tầng 3 (Fallback mặc định)**: Sử dụng danh sách localhost (`http://localhost:5173`, `http://localhost:3000`) nếu cả 2 tầng trên đều không có giá trị.

```csharp
var envOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

var originsToUse = (envOrigins is { Length: > 0 } ? envOrigins : configuredOrigins)
    ?? ["http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173"];

var allowedOrigins = originsToUse
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct()
    .ToArray();
```

### C. Đảo Ngược Thứ Tự Ưu Tiên Trong Settings Stores
- Cập nhật `LlmSettingsStore` và `TelegramSettingsStore`: Luôn ưu tiên giá trị từ `IConfiguration` và `Environment.GetEnvironmentVariable` trước khi nạp từ file JSON tĩnh trên đĩa.

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Mặt Tích Cực (Positive)
- **Bảo mật tuyệt đối trên Git**: Không còn nguy cơ rò rỉ secret lên public repository khi đẩy mã nguồn.
- **Tính độc lập cao (Decoupled Deployment)**: Frontend và Backend có thể thay đổi domain hoặc scale độc lập mà không cần phải compile/build lại source code C#.
- **Trải nghiệm Dev (DX) mượt mà**: Dev local vẫn có thể `dotnet run` bình thường nhờ `appsettings.Development.json` và User Secrets.

### Mặt Tiêu Cực & Nợ Kỹ Thuật (Negative & Tech Debt)
- **Thiết lập ban đầu cho Dev mới**: Thành viên mới clone dự án cần đọc tài liệu để nạp User Secrets hoặc tạo biến môi trường.
- **Nợ kỹ thuật Container Stateful**: Việc ghi file `llm_settings.json` cục bộ trong container vẫn còn tồn tại. Khi chạy container non-root (`USER $APP_UID`), thao tác ghi file này bị từ chối quyền và container là ephemeral (mất dữ liệu khi restart). Vấn đề này đã được tạo issue theo dõi riêng: [GitHub Issue #2](https://github.com/taitrandev/Tour-Agent/issues/2).


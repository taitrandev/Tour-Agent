# ADR-0002: Tách Biệt Liveness & Readiness Probes và Đóng Gói Docker Multi-Stage

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: 2026-11-12
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: `TourAgent.Api`, `TourAgent.Infrastructure.Health`, `Dockerfile`

---

## 1. Bối Cảnh (Context)

Khi triển khai ứng dụng dạng container lên nền tảng đám mây (Azure App Service / Azure Container Apps / Kubernetes), hệ thống điều phối (Cloud Orchestrator) liên tục gửi các tín hiệu thăm dò (Health Probes) để giám sát tình trạng container.

Trong thiết kế ban đầu, hệ sinh thái gặp các vấn đề sau:
1. **Rủi ro sập dây chuyền (Cascade Failure) vì gộp chung Health Check**:
   - Nếu chỉ dùng một endpoint duy nhất `/health` kiểm tra tất cả: Khi dịch vụ AI bên ngoài (DeepSeek API) bị quá tải (HTTP 429), bảo trì hoặc hết quota, endpoint `/health` trả về HTTP 503 (`Unhealthy`).
   - Hậu quả: Azure App Service hiểu nhầm là toàn bộ ứng dụng bị chết và **tự động KILL container**, dẫn đến toàn bộ hệ thống đặt tour, API khách hàng bị sập theo dù database và server hoàn toàn khỏe mạnh.
2. **Kích thước Image Docker quá lớn & Rủi ro bảo mật**:
   - Nếu build Docker đơn tầng (Single-stage) sử dụng SDK image, dung lượng container lên tới ~1GB, tốn băng thông truyền tải trên CI/CD pipeline và lãng phí bộ nhớ lưu trữ.
   - Image chứa trình biên dịch (`dotnet build`, `csc`) và chạy dưới quyền `root` tiềm ẩn nguy cơ bảo mật nếu container bị chiếm quyền điều khiển.

---

## 2. Quyết Định Kiến Trúc (Decision)

### A. Tách Biệt Liveness Probe và Readiness Probe
Hệ thống chuyển đổi sang mô hình 2 tầng Probe độc lập theo chuẩn Cloud-Native:

| Loại Probe | Endpoint | Mục Đích | Logic Kiểm Tra | Hành Động Khi Thất Bại |
| :--- | :--- | :--- | :--- | :--- |
| **Liveness** | `/health/live` | Kiểm tra tiến trình ASP.NET Core còn hoạt động, không bị deadlock hoặc treo CPU | Kiểm tra nội tại (`self`), thời gian phản hồi ~1ms, **tuyệt đối không gọi DB hay API ngoài** | Azure sẽ **Restart** lại container |
| **Readiness** | `/health/ready` | Kiểm tra hệ thống đã sẵn sàng nhận request của người dùng hay chưa | Kiểm tra kết nối PostgreSQL DB và DeepSeek AI | Tạm ngắt routing tải (không kill container) |

- **Xử lý mềm đối với dịch vụ bên thứ 3 (DeepSeek AI)**:
  - Bọc `CancellationToken` giới hạn thời gian probe tối đa **5 giây**.
  - Khi DeepSeek gặp lỗi kết nối, sai key (401) hoặc hết quota (429): `DeepSeekHealthCheck` trả về trạng thái **`Degraded` (Suy giảm)** thay vì `Unhealthy`. 
  - Kết quả: Endpoint trả về mã HTTP 200, app chính vẫn chạy, khách vẫn xem và đặt tour bình thường, hệ thống tự kích hoạt kịch bản fallback thông báo "AI đang bảo trì".

- **Ẩn thông tin nhạy cảm ở Production**:
  - Môi trường `Development`: Endpoint trả về JSON chi tiết (thời gian chạy từng check, tên check, mô tả).
  - Môi trường `Production`: Chỉ trả về `{ "status": "Healthy" }` hoặc `{ "status": "Degraded" }`, giấu toàn bộ exception stack trace và tên hạ tầng nội bộ.

### B. Đóng Gói Docker Multi-Stage 4 Tầng Tối Ưu
Trong `dotnet-backend-core-main/Dockerfile`:
1. **Stage 1 (`base`)**: Dùng `mcr.microsoft.com/dotnet/aspnet:10.0`, cấu hình non-root user `USER $APP_UID` (UID 1654), lắng nghe cổng không đặc quyền `8080`.
2. **Stage 2 (`build`)**: Dùng `mcr.microsoft.com/dotnet/sdk:10.0`. Áp dụng **Docker Layer Caching**: Copy `Directory.Build.props` và toàn bộ file `.csproj` trước rồi mới chạy `dotnet restore`. Lớp này sẽ được Docker cache lại, chỉ restore lại khi danh sách thư viện thay đổi.
3. **Stage 3 (`publish`)**: Chạy `dotnet publish -c Release /p:UseAppHost=false`.
4. **Stage 4 (`final`)**: Chỉ copy kết quả publish từ Stage 3 sang Stage 1. Kích thước image thu gọn từ gần 1GB xuống chỉ còn **102MB**.

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Mặt Tích Cực (Positive)
- **Hệ thống tự phục hồi chính xác (Resilience & Self-Healing)**: Azure App Service chỉ restart container khi app thực sự bị treo đơ tiến trình (`/health/live`), không bị khởi động lại nhầm khi đối tác AI DeepSeek gián đoạn.
- **Tốc độ CI/CD vượt trội**: Nhờ Docker Layer Caching, các lần build sau bỏ qua bước `dotnet restore`, rút ngắn thời gian build image xuống dưới 15 giây.
- **Tiêu chuẩn bảo mật cao**: Container không có compiler, chạy dưới user giới hạn (non-root) và cổng 8080.

### Mặt Tiêu Cực (Negative)
- **Độ phức tạp trong cấu hình**: Người vận hành Cloud cần phải nắm rõ để trỏ đúng: Liveness Probe trỏ vào `/health/live`, không trỏ nhầm vào `/health/ready`.


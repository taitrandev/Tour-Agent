# 📋 DEPLOYMENT CHECKLIST & CONFIGURATION TEMPLATE

> **Hướng dẫn sử dụng**: Sao chép file này vào thư mục `docs/AZURE_DEPLOYMENT_CHECKLIST.md` của dự án mục tiêu và cập nhật trạng thái checklist qua từng giai đoạn.

---

## 🔑 1. Danh Sách Biến Môi Trường Cần Cấu Hình Trên Cloud

| Tên Biến Môi Trường | Ý nghĩa / Mục đích | Giá trị mẫu |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Môi trường chạy của ứng dụng | `Production` |
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối Database Cloud | `Host=...;Database=...;Username=...;Password=...;Ssl Mode=Require;` |
| `CORS_ALLOWED_ORIGINS` | Danh sách domain Frontend được gọi API | `https://app.vercel.app,https://admin.domain.com` |
| `<CUSTOM_SERVICE>_API_KEY` | API Key các dịch vụ bên thứ 3 | `sk-...` |

---

## 🚀 2. Checklist Tiến Độ Triển Khai (Progress Tracking)

### Giai đoạn 0: Chuẩn bị mã nguồn (Cloud-Ready)
- [ ] Dọn dẹp toàn bộ Secret & Hardcode khỏi mã nguồn; rotate key nếu từng bị commit.
- [ ] Cấu hình CORS động ưu tiên biến môi trường `CORS_ALLOWED_ORIGINS`.
- [ ] Tách biệt Health Probes: `/health/live` (tiến trình sống) và `/health/ready` (tài nguyên sẵn sàng).
- [ ] Viết Dockerfile Multi-Stage tối ưu cache và chạy dưới user non-root.

### Giai đoạn 1: Chuẩn bị hạ tầng Cloud
- [ ] Tạo Resource Group trên Cloud.
- [ ] Tạo Web App / Container Service.
- [ ] Tạo Database Instance (PostgreSQL / MySQL / SQL Server) và cấu hình Firewall mở kết nối.
- [ ] Nhập toàn bộ biến môi trường ở Mục 1 vào Cloud App Settings.

### Giai đoạn 2: Thiết lập CI Pipeline
- [ ] Tạo workflow file `.github/workflows/ci.yml`.
- [ ] Cấu hình các bước: Checkout -> Setup SDK -> Restore -> Build -> Run Unit/Arch Tests.

### Giai đoạn 3: Thiết lập CD Pipeline
- [ ] Cấu hình thông tin xác thực an toàn (Publish Profile hoặc OIDC Federated Credentials).
- [ ] Cấu hình deploy tự động khi merge vào nhánh chính (`main`).

### Giai đoạn 4: Quản lý hạ tầng & Vận hành (IaC / Monitoring)
- [ ] Thiết lập Health Check Probe trên Cloud trỏ vào `/health/live`.
- [ ] Cấu hình Custom Domain & SSL Certificate.
- [ ] Kiểm tra cơ chế Rollback khi có lỗi.


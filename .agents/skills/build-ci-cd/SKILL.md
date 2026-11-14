---
name: build-ci-cd
description: Quy trình dựng CI/CD chuẩn 80/20 deploy ứng dụng backend lên Cloud (Azure / Docker / GitHub Actions). Kích hoạt khi người dùng muốn: (1) chuẩn bị mã nguồn cloud-ready hoặc 12-factor app, (2) viết Dockerfile multi-stage tối ưu, (3) cấu hình dynamic CORS hoặc health check probes, (4) thiết lập CI/CD pipeline tự động với GitHub Actions, (5) deploy backend lên Azure App Service hoặc Container Apps.
---

# 🚀 Build CI/CD Skill — Cẩm Nang Triển Khai Cloud-Ready & CI/CD Thực Chiến

Skill này cung cấp quy trình chuẩn mực theo nguyên lý 80/20 để đưa một dự án Backend từ môi trường local lên Cloud an toàn, bài bản và có thể tái sử dụng cho nhiều dự án (.NET, Node.js, Python, Go).

---

## 🧭 5 Giai Đoạn Triển Khai Chuẩn

Quy trình triển khai bao gồm 5 giai đoạn tuần tự:

1. **Giai đoạn 0: Cloud-Ready (Mã nguồn sẵn sàng)**: Soi và chuẩn hóa source code đạt chuẩn 12-Factor App trước khi tự động hóa.
2. **Giai đoạn 1: Chuẩn bị hạ tầng Cloud**: Khởi tạo Resource Group, Database, App Service/Container và nạp biến môi trường.
3. **Giai đoạn 2: CI (Continuous Integration)**: Tự động hóa Restore ➔ Build ➔ Run Unit & Architecture Tests khi có Push / PR.
4. **Giai đoạn 3: CD (Continuous Deployment)**: Đóng gói và phát hành tự động lên Cloud khi merge nhánh chính.
5. **Giai đoạn 4: Quản lý hạ tầng & Vận hành (IaC / Monitoring)**: Quản lý hạ tầng bằng mã (Bicep/Terraform), giám sát Health Probes và thiết lập cơ chế Rollback.

---

## 📌 Quy Trình Thực Thi Giai Đoạn 0: Cloud-Ready

Trước khi viết pipeline CI/CD, agent bắt buộc phải kiểm tra và hoàn thành 5 trụ cột Cloud-Ready:

1. **Dọn dẹp Secret & Quét lịch sử Git**:
   - Xóa bỏ mọi Connection String, API Key, Token khỏi file cấu hình chính và code logic.
   - Quét lịch sử commit bằng `git log -S`. Nếu phát hiện secret từng bị commit, hướng dẫn người dùng **xoay (rotate) key mới ngay lập tức**.
2. **Cấu hình CORS động (Dynamic CORS)**:
   - Không fix cứng domain localhost trong source code.
   - Thiết lập cơ chế ưu tiên: Biến môi trường (`CORS_ALLOWED_ORIGINS`) ➔ File config ➔ Mặc định localhost khi dev.
3. **Tách biệt Health Check Probes (Liveness & Readiness)**:
   - `/health/live`: Chỉ kiểm tra tiến trình app còn sống (dành cho Cloud restart container). Tuyệt đối không gọi tài nguyên ngoài.
   - `/health/ready`: Kiểm tra kết nối Database và các tài nguyên phụ thuộc.
   - Với các dịch vụ phụ thuộc ngoài (DeepSeek, OpenAI), luôn bọc Timeout (<= 5s) và ưu tiên trả về `Degraded` khi lỗi để app chính không bị crash.
4. **Đóng gói Dockerfile Multi-Stage & Stateless**:
   - Tách Stage Build (SDK đầy đủ) và Stage Final (Runtime tối giản).
   - Tối ưu Docker Layer Cache (copy file dependencies và restore trước khi copy source).
   - Sử dụng user không có quyền root (`non-root`).
   - Kiểm tra mã nguồn không lưu file tạm/file stateful vào container filesystem.
5. **Khởi tạo Checklist triển khai dự án**:
   - Tạo file `docs/AZURE_DEPLOYMENT_CHECKLIST.md` trong dự án mục tiêu để quản lý toàn bộ biến môi trường và tiến độ.

---

## 📚 Tài Liệu Tham Khảo Kỹ Thuật (Progressive Disclosure)

Khi thực hiện từng tác vụ chuyên sâu hoặc làm việc với framework cụ thể, hãy đọc các tài liệu sau trong thư mục `references/`:

- **Quy trình chi tiết & Lệnh quét Giai đoạn 0**: Đọc [references/phase0-cloud-ready.md](references/phase0-cloud-ready.md) để biết các lệnh `git grep`, tiêu chuẩn liveness/readiness và tiêu chí Definition of Done.
- **Mẫu mã nguồn .NET & Azure**: Đọc [references/dotnet-azure.md](references/dotnet-azure.md) khi làm việc với dự án .NET/C# để lấy code mẫu CORS động, tách Health Probes, Dockerfile multi-stage và quy tắc ánh xạ biến môi trường `__`.
- **Mẫu Checklist triển khai**: Đọc [references/deployment-checklist-template.md](references/deployment-checklist-template.md) để sao chép mẫu checklist biến môi trường và các bước thao tác trên Cloud Console sang dự án mới.

---

## 📋 Quy Tắc Quản Lý Checklist Dự Án

- Bất kỳ khi nào bổ sung biến môi trường mới hoặc thêm một dịch vụ bên ngoài vào hệ thống, agent **bắt buộc phải cập nhật ngay** vào file `docs/AZURE_DEPLOYMENT_CHECKLIST.md` của dự án.

# ADR-0004: Tích Hợp Cloud PostgreSQL (Supabase) & Chiến Lược Triển Khai CD Lên Azure Container Apps Với Zero-Downtime Smoke Testing

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: 2026-11-18
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: Cấu hình hạ tầng Cloud (Azure Container Apps, Supabase), Pipeline Continuous Deployment (`backend-ci.yml`)

---

## 1. Bối Cảnh (Context)

Sau khi hoàn thành Giai đoạn 1 (CI) với artifact Docker image đã được đẩy lên Azure Container Registry (ACR), việc triển khai ứng dụng backend lên môi trường Cloud thực tế đối mặt với các thách thức kỹ thuật:

1. **Phụ thuộc cơ sở dữ liệu trên Cloud**:
   - Cơ sở dữ liệu ban đầu chạy ở môi trường cục bộ (Local PostgreSQL).
   - Nếu deploy container lên Azure mà không có Database trên Internet, ứng dụng sẽ không vượt qua được kiểm tra `/health/ready` (do `DatabaseHealthCheck` ping thất bại), dẫn đến container bị crash hoặc không thể phục vụ các API nghiệp vụ.
   - Các dịch vụ quản lý Database trên Azure (như Azure Database for PostgreSQL Flexible Server) có chi phí tối thiểu khoảng $15 - $25/tháng và không có gói miễn phí vĩnh viễn, không phù hợp với bài toán tiết kiệm chi phí cho dự án cá nhân / MVP phỏng vấn.

2. **Lựa chọn nền tảng Compute cho Container Backend**:
   - Cần một giải pháp chạy container hiện đại, nhẹ, bảo mật, hỗ trợ microservices và có khả năng **Scale-to-Zero** (tự tắt khi không có traffic để không phát sinh chi phí).

3. **Chiến lược Continuous Deployment (CD) & Zero-Downtime**:
   - Nếu tách CD thành workflow độc lập qua sự kiện `workflow_run`, chuỗi pipeline bị phân mảnh, khó truyền vết commit SHA và dễ xảy ra race condition.
   - Khi cập nhật phiên bản container mới, nếu kiểm tra Smoke Test ngay lập tức bằng `curl --retry` thông thường, lệnh kiểm tra sẽ thất bại sớm do Azure Container Apps trả về mã **HTTP 503 (Service Unavailable)** trong thời gian revision mới đang boot.

---

## 2. Quyết Định Kiến Trúc (Decision)

### A. Chọn Supabase PostgreSQL Làm Cơ Sở Dữ Liệu Cloud MVP
1. **Lý do lựa chọn**:
   - Cung cấp dịch vụ PostgreSQL Serverless trên nền điện toán đám mây với gói Free Tier ổn định (500MB lưu trữ), đầy đủ SSL/TLS.
   - Đặt tại Region `Singapore (ap-southeast-1)` nằm sát với Azure `Southeast Asia`, tối ưu hóa độ trễ mạng (latency).
2. **Khởi tạo dữ liệu tự động (Auto-Seed Mechanism)**:
   - Tận dụng logic đã lập trình sẵn trong các seeder (`OfferDataSeeder`, `ContactDataSeeder`, `OrderDataSeeder`) sử dụng lệnh `CREATE TABLE IF NOT EXISTS`.
   - Ngay lần đầu tiên backend container trên Azure khởi động và kết nối vào database Supabase trống, toàn bộ cấu trúc bảng và dữ liệu mẫu sẽ được tự động khởi tạo mà không cần phải can thiệp thủ công bằng migration script.

### B. Chọn Azure Container Apps (ACA) Cho Môi Trường Compute
1. **Mô hình hoạt động**:
   - Đặt trong một cụm ảo chung **Container Apps Environment** (`cae-touragent-prod`).
   - Cấu hình tài nguyên tối thiểu: `0.25 vCPU` và `0.5 GiB RAM`.
   - Bật Ingress công khai lắng nghe cổng **`8080`** (đồng bộ với cổng non-root của `Dockerfile` theo [ADR-0002](0002-separated-liveness-and-readiness-probes.md)).
   - Thiết lập `min-replicas: 0` và `max-replicas: 1` để hệ thống tự động scale-to-zero khi nhàn rỗi.
2. **Health Probes trên Cloud**:
   - **Liveness Probe**: Trỏ vào `/health/live` (cổng 8080) để Azure giám sát tiến trình container.
   - **Readiness Probe**: Trỏ vào `/health/ready` (cổng 8080) để Azure xác nhận Database và AI Agent đã sẵn sàng trước khi định tuyến traffic.

### C. Hợp Nhất Pipeline CD & Khắc Phục Lỗi Smoke Test Giả
1. **Hợp nhất Workflow**:
   - Bổ sung job `deploy` nối tiếp trực tiếp sau job `docker-build-and-push` trong file duy nhất `.github/workflows/backend-ci.yml`.
   - Điều kiện kích hoạt: `needs: docker-build-and-push` và chỉ chạy khi push vào nhánh `main`.
2. **Cập nhật theo Commit SHA (Immutable Deployment)**:
   - Chỉ sử dụng tag `${{ github.sha }}` cho lệnh deploy:
     `az containerapp update --name touragent-api --resource-group rg-touragent-prod --image touragentacr.azurecr.io/touragent-api:${{ github.sha }}`
   - Loại bỏ phụ thuộc vào tag `latest`.
3. **Cơ chế Smoke Test Chống Báo Lỗi Giả**:
   - Lấy FQDN tự động qua Azure CLI: `az containerapp show ... --query "properties.configuration.ingress.fqdn"`.
   - Thêm cờ `--retry-all-errors` và delay 10 giây vào lệnh `curl`:
     ```bash
     curl -f "https://${FQDN}/health/ready" --retry 6 --retry-delay 10 --retry-all-errors --max-time 15
     ```
   - Cờ này buộc `curl` phải tiếp tục retry ngay cả khi gặp mã HTTP 503 tạm thời trong lúc Azure Container Apps đang khởi tạo Revision mới.

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- **Tối ưu chi phí 100% (Zero-Cost MVP)**: Cả Supabase Free Tier và Azure Container Apps (gói Free Grant hàng tháng) kết hợp lại tạo thành hệ thống cloud có chi phí vận hành 0đ.
- **Bảo đảm Zero-Downtime**: Nhờ tích hợp Readiness Probe với cơ chế Revision của Azure Container Apps, nếu image mới có lỗi boot, traffic người dùng vẫn được giữ an toàn ở revision cũ.
- **Trải nghiệm CI/CD mượt mà**: Pipeline hiển thị trực quan toàn bộ quy trình từ Build -> Test -> Push ACR -> Deploy ACA chỉ trong một giao diện Actions duy nhất.

### Đánh đổi / Nhược điểm (Cons / Trade-offs)
- **Day-0 Setup thủ công**: Yêu cầu phải khởi tạo tài nguyên Container App mẫu trên Azure lần đầu tiên trước khi pipeline CD có thể chạy tự động.
- **Cold Start**: Khi container scale về 0, request đầu tiên sau thời gian nhàn rỗi có thể mất từ 5 đến 10 giây để container boot up và kết nối DB.

---

## 4. Liên Kết Tham Chiếu (References)
- [create-azure-container-app.md](../create-azure-container-app.md): Hướng dẫn chi tiết Day-0 Provisioning cho Azure Container Apps.
- [PROGRESS.md](../PROGRESS.md): Nhật ký hành trình CI/CD dự án TourAgent.
- [0002-separated-liveness-and-readiness-probes.md](0002-separated-liveness-and-readiness-probes.md): Kiến trúc tách biệt Liveness và Readiness Probes.
- [0003-build-once-push-acr-in-ci.md](0003-build-once-push-acr-in-ci.md): Quyết định đóng gói và đẩy image tag SHA lên ACR.


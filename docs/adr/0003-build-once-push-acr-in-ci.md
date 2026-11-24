# ADR-0003: Áp Dụng Nguyên Tắc "Build Once, Deploy The Same Artifact" — Push Docker Image Lên ACR Trong CI Pipeline

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: 2026-11-15
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: GitHub Actions CI/CD Pipeline, Azure Container Registry (ACR), Dockerfile

---

## 1. Bối Cảnh (Context)

Khi thiết kế pipeline CI/CD cho dự án TourAgent (.NET 10 API), có sự phân vân về ranh giới trách nhiệm giữa **CI (Continuous Integration)** và **CD (Continuous Delivery/Deployment)** liên quan đến thao tác Docker Build & Push:

1. **Quan điểm ban đầu**:
   - CI chỉ dừng ở mức kiểm tra compile, unit/architecture test (`dotnet test`) và build thử Docker image trên local runner rồi hủy image.
   - CD sẽ chịu trách nhiệm build lại Docker image và push lên registry (ACR) trước khi deploy vào môi trường chạy (Azure Container App / App Service).
2. **Vấn đề phát sinh**:
   - Nếu CD build lại Docker image từ mã nguồn, image được deploy lên production thực chất **không phải là artifact đã qua kiểm thử ở CI**.
   - Có thể xảy ra sai lệch môi trường (base image tag `mcr.microsoft.com/...` cập nhật bản vá mới, cache build khác nhau, hash SHA khác nhau).
   - Tốn gấp đôi thời gian và tài nguyên runner cho bước build Docker.

---

## 2. Quyết Định Kiến Trúc (Decision)

Áp dụng nguyên lý cốt lõi của DevOps hiện đại: **"Build once, deploy the same artifact"** (Chỉ build một lần, mang đúng artifact đó đi deploy qua mọi môi trường).

Cụ thể hóa trong pipeline:
1. **Đối với Pull Request (PR Validation)**:
   - Chạy `dotnet restore`, `dotnet build`, `dotnet test`.
   - Chạy `docker build` để kiểm tra tính toàn vẹn của Dockerfile và khả năng đóng gói.
   - **Tuyệt đối không push** image lên ACR (tiết kiệm băng thông, lưu trữ và bảo mật).
2. **Đối với nhánh `main` (Push / Merge to Main)**:
   - Chạy toàn bộ luồng Build & Test.
   - Build Docker image chính thức.
   - Đăng nhập Azure Container Registry qua OIDC (không dùng admin password).
   - **Push trực tiếp Docker image** lên ACR với 2 tags:
     - Tag commit hash: `${{ github.sha }}` (Immutable artifact dùng cho truy vết và deploy).
     - Tag `latest` (Dành cho việc inspect hoặc pull nhanh).
3. **Ranh giới sang Giai đoạn 2 (CD Pipeline)**:
   - Bước CD **không compile code hay build Docker image lại**.
   - CD chỉ nhận tag `${{ github.sha }}` đã được CI phê duyệt và kích hoạt cập nhật hạ tầng (`az containerapp update --image acrtouragent.azurecr.io/touragent-api:<sha>`).

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- **Tính nhất quán tuyệt đối (Artifact Immutability)**: Đảm bảo 100% binary/image chạy ở Production chính là image đã vượt qua 87 test case ở CI.
- **Tiết kiệm thời gian CD**: CD chỉ mất vài chục giây để kích hoạt Container App trỏ sang image mới, không mất vài phút để build lại image.
- **Truy vết minh bạch (Traceability)**: Dễ dàng rollback về bất kỳ commit nào trong lịch sử chỉ bằng cách trỏ Container App về tag `${{ github.sha }}` tương ứng trên ACR.

### Đánh đổi / Nhược điểm (Cons / Trade-offs)
- **Yêu cầu phân quyền trong CI**: CI pipeline khi chạy trên `main` cần được cấp quyền kết nối Azure (`AcrPush`) thay vì chỉ chạy độc lập trên GitHub runner.
- **Lưu trữ ACR**: Mỗi commit merge vào `main` sinh ra một image tag mới trên ACR, cần thiết lập chính sách dọn dẹp (Retention Policy / ACR Purge) về lâu dài nếu kho đầy.

---

## 4. Liên Kết Tham Chiếu (References)
- [PROGRESS.md](../PROGRESS.md): Nhật ký hành trình triển khai CI/CD.
- [create-oidc-managed-identity.md](../create-oidc-managed-identity.md): Hướng dẫn thiết lập OIDC & Managed Identity cho GitHub Actions kết nối ACR.


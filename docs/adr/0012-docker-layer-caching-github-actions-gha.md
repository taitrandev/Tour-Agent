# ADR-0012: Tối Ưu Hóa Docker Layer Caching Trên GitHub Actions Bằng BuildKit GHA Backend & Dual-Scope

- **Trạng thái**: Accepted (Đã chấp thuận & thực thi)
- **Ngày quyết định**: 2026-11-25
- **Người thực hiện**: TourAgent Engineering Team
- **Phạm vi**: `CI/CD Pipeline`, `GitHub Actions`, `Docker Buildx`, `Dockerfile`

---

## 1. Bối Cảnh (Context)

Dự án TourAgent sử dụng GitHub Actions với runner tiêu chuẩn (`runs-on: ubuntu-latest`) để thực thi pipeline CI/CD cho cả Backend (.NET 10 Minimal API) và Frontend (React 19 SPA + Nginx):

1. **Bản chất runner ngắn hạn (Ephemeral Runners)**:
   - Mỗi lần kích hoạt workflow (Push hoặc Pull Request), GitHub Actions cấp phát một máy ảo hoàn toàn mới.
   - Khi workflow kết thúc, máy ảo bị hủy ngay lập tức, kéo theo toàn bộ Docker daemon state và layer cache cục bộ.
2. **Vấn đề phát sinh (Cold Start Build)**:
   - Mặc dù cả hai Dockerfile đều được thiết kế multi-stage theo chuẩn 80/20 (tách `COPY` manifest dependencies và restore trước khi copy source code), trên ephemeral runner, Docker daemon luôn khởi đầu với cache trống.
   - **Backend**: Mỗi lần build đều tải lại toàn bộ NuGet packages từ `nuget.org` dù file `.csproj` không thay đổi.
   - **Frontend**: Mỗi lần build đều tải lại hàng trăm MB npm packages từ `registry.npmjs.org` dù `package-lock.json` không thay đổi.
   - Thời gian build Docker kéo dài không cần thiết (từ 3 - 6 phút mỗi run), làm cạn kiệt quota GitHub Actions và giảm tốc độ chu kỳ phản hồi (feedback loop) của đội ngũ phát triển.

---

## 2. Quyết Định Kiến Trúc (Decision)

Áp dụng giải pháp **Docker Buildx GitHub Actions Cache Backend (`type=gha`)** kết hợp **BuildKit Cache Mounts** và **phân tách Dual-Scope Cache**:

1. **Sử dụng GitHub Actions Cache Backend (`type=gha`)**:
   - Sử dụng cơ chế remote caching native của BuildKit (`docker/build-push-action@v6`).
   - Cấu hình `cache-from: type=gha` để kéo các layer đã build từ GitHub Actions Cache service.
   - Cấu hình `cache-to: type=gha,mode=max` để lưu trữ **toàn bộ các layer của tất cả các build stages** (bao gồm cả stage build/publish trung gian chứa dependencies đã restore, không chỉ riêng stage runtime cuối cùng).
2. **Phân tách Cache Scope độc lập (Dual-Scope Caching)**:
   - Backend Docker build sử dụng `scope=backend`.
   - Frontend Docker build sử dụng `scope=frontend`.
   - Ngăn chặn triệt để tình trạng ghi đè cache lẫn nhau trong cùng repository (loại trừ hiện tượng Cache Thrashing).
3. **Chiến lược Cache giữa `main` và Pull Request**:
   - Nhánh `main` vừa đọc vừa ghi cache (`cache-to: mode=max`) tạo thành baseline cache chuẩn mực của dự án.
   - Các nhánh Pull Request tự động kế thừa cache của nhánh `main` (`cache-from`), giúp quá trình kiểm thử PR diễn ra tức thì, chỉ build lại các layer có mã nguồn thực sự thay đổi trong PR.
4. **Tích hợp BuildKit Cache Mounts trong Dockerfiles**:
   - **Backend**: Mount cache `id=nuget` trên cả 2 bước `dotnet restore` VÀ `dotnet publish` (`RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages`). Đảm bảo `dotnet publish --no-restore` đọc được đầy đủ assemblies từ cache mà không bị lỗi thiếu package `NETSDK1064`.
   - **Frontend**: `RUN --mount=type=cache,id=npm,target=/root/.npm npm ci`.
   - Đảm bảo ngay cả khi có thêm thư viện mới (layer cache bị invalidate), package manager vẫn giữ được thư mục cache và chỉ tải các thư viện mới bổ sung.
5. **Kích hoạt Cache cho CI Test Step**:
   - Tích hợp `cache: true` trong `actions/setup-dotnet@v4` cho job `build-and-test` của Backend.

---

## 3. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- **Tốc độ build siêu tốc**: Giảm thời gian Docker build xuống dưới 30 - 45 giây ở các lần chạy warm-cache kế tiếp (các step restore/download packages được `CACHED` 100%).
- **Chi phí 0 VNĐ**: Tận dụng 10GB dung lượng GitHub Actions Cache miễn phí sẵn có của repository.
- **Tiết kiệm tài nguyên ACR**: Không làm tốn 10GB dung lượng lưu trữ eo hẹp của Azure Container Registry (ACR Basic) so với giải pháp `type=registry`.
- **An toàn cho Pull Request**: Không yêu cầu quyền ghi hay credentials Azure ACR trên các pull request không đáng tin cậy.

### Đánh đổi / Nhược điểm (Cons / Trade-offs)
- **Chính sách dọn dẹp LRU của GitHub**: Nếu toàn bộ cache của repository vượt quá 10GB, GitHub sẽ tự động dọn dẹp các cache cũ nhất ít dùng (Least Recently Used). Tuy nhiên với 2 scope backend và frontend, dung lượng chiếm dụng chỉ khoảng 1.5GB - 2.5GB, hoàn toàn an toàn.

---

## 4. 🛑 NGUYÊN TẮC BẤT DI BẤT DỊCH CHO AI AGENT (Golden Rules)

1. **BẮT BUỘC chỉ định `scope` khi dùng `type=gha`**: Khi khai báo `docker/build-push-action`, tuyệt đối không dùng `type=gha` trần trụi. Luôn khai báo tường minh `scope=backend` cho Backend và `scope=frontend` cho Frontend.
2. **LUÔN dùng `mode=max` cho `cache-to`**: Nếu thiếu `mode=max`, Buildx chỉ lưu stage runtime cuối cùng và bỏ qua stage build/restore, làm mất đi giá trị của layer caching.
3. **BẢO LƯU BuildKit Cache Mounts trong Dockerfile**: Không xóa các khai báo `--mount=type=cache` trong `dotnet restore` và `npm ci`.

---

## 5. Liên Kết Tham Chiếu (References)
- [ADR-0003: Build Once, Deploy The Same Artifact](0003-build-once-push-acr-in-ci.md)
- [Docker Buildx Cache Export Documentation](https://docs.docker.com/build/cache/backends/gha/)
- [PROGRESS.md](../PROGRESS.md)


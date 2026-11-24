# 🏛️ Architecture Decision Records (ADRs) — TourAgent

> **Nguồn Chân Lý Duy Nhất (Single Source of Truth) về các quyết định kiến trúc và luật thép kỹ thuật của dự án TourAgent.**
> Mọi AI Agent và Developer khi làm việc trên dự án phải đọc và tuân thủ các quyết định tại đây.

---

## 📌 1. Bảng Mục Lục Quyết Định (Index)

| Mã số | Tiêu đề quyết định | Phạm vi ảnh hưởng | Trạng thái | Ngày quyết định | Tóm tắt cốt lõi |
| :---: | :--- | :--- | :---: | :---: | :--- |
| [**0001**](0001-cloud-ready-secrets-and-dynamic-cors.md) | **Cloud-Ready Secrets & Dynamic CORS Configuration** | `TourAgent.Api`, `Configuration` | Accepted | 2026-11-12 | Trừu tượng hóa secrets qua biến môi trường; CORS động từ `CORS_ALLOWED_ORIGINS`. |
| [**0002**](0002-separated-liveness-and-readiness-probes.md) | **Separated Liveness and Readiness Probes** | `TourAgent.Api`, `HealthChecks` | Accepted | 2026-11-12 | Tách probe liveness (`/health/live`) và readiness (`/health/ready` probe DB + AI). |
| [**0003**](0003-build-once-push-acr-in-ci.md) | **Build-Once, Push-ACR Architecture in CI Pipeline** | `CI/CD`, `GitHub Actions`, `Docker` | Accepted | 2026-11-15 | Build Docker image 1 lần duy nhất trong CI, gắn commit SHA và đẩy lên ACR. |
| [**0004**](0004-cd-azure-container-apps-and-supabase.md) | **CD to Azure Container Apps with Supabase** | `CI/CD`, `Infrastructure`, `Database` | Accepted | 2026-11-18 | Deploy CD tự động lên Azure Container Apps, kết nối Supabase PostgreSQL an toàn. |
| [**0005**](0005-strict-camelcase-api-contracts.md) | **Strict camelCase API Contracts & Ban snake_case** | `API Contracts`, `DTOs`, `Frontend` | Accepted | 2026-11-24 | Chuẩn hóa 100% camelCase cho HTTP API & Query Params; Cấm tuyệt đối snake_case. |
| [**0006**](0006-hybrid-80-20-tour-offer-adaptive-ui.md) | **Hybrid 80/20 Adaptive UI for Tour (1) — Offer (N)** | `Domain`, `Application`, `Frontend` | Accepted | 2026-11-24 | Giữ DB 1-N nhưng tự động thích ứng UI: 1 offer hiển thị phẳng, ≥2 offer hiện pill tabs. |
| [**0007**](0007-query-objects-and-unified-travels-api.md) | **Query Objects for [FromQuery] & Unified Travels API** | `TourAgent.Api`, `Controllers` | Accepted | 2026-11-24 | Đóng gói query params vào Query Objects; Xóa `ToursController`, gộp vào `TravelsController`. |
| [**0008**](0008-separate-tour-page-zero-fouc.md) | **Dedicated Tour Page (/tour/:slug) with Zero-FOUC** | `Frontend`, `Routing`, `UX` | Accepted | 2026-11-24 | Tách route `/tour/:slug`, loại bỏ mock data, conditional render và Shimmer skeleton. |

---

## 🛠️ 2. Quy Trình Quản Lý & Tạo ADR Mới (`adr-manager` Skill)

Mọi thay đổi kiến trúc, hợp nhất API, thay đổi quy ước giao tiếp hoặc cấu trúc dữ liệu phải tuân thủ nghiêm ngặt **Quy trình 2 bước** của skill `adr-manager` (theo nguyên bản Michael Nygard):

### Bước 1: Đánh Giá & Xác Nhận (Evaluation & Confirmation)
- Agent rà soát thay đổi có đủ tầm kiến trúc bất khả nghịch không (ranh giới tầng, quy ước contract, invariant).
- Nếu **KHÔNG**: Báo cáo rõ ràng không có quyết định kiến trúc mới cần tạo ADR.
- Nếu **CÓ**: Tóm tắt các điểm cốt lõi (Context, Decision, Rules, Trade-offs) và **HỎI XÁC NHẬN TỪ NGƯỜI DÙNG**.
- **Cấm tự ý tạo file ADR** khi người dùng chưa đồng ý.

### Bước 2: Khởi Tạo File & Đồng Bộ Mục Lục
- Đặt tên file: `docs/adr/000X-[slug].md` với số thứ tự 4 chữ số tăng dần.
- Áp dụng cấu trúc chuẩn mực bao gồm khối `🛑 NGUYÊN TẮC BẤT DI BẤT DỊCH CHO AI AGENT (Golden Rules)`.
- Cập nhật dòng mới vào bảng mục lục tại file này (`docs/adr/README.md`).


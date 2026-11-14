# 📋 Giai Đoạn 0: Cloud-Ready Playbook (Độc Lập Ngôn Ngữ)

Tài liệu này cung cấp tiêu chuẩn kỹ thuật và các bước kiểm tra bắt buộc để đảm bảo mã nguồn đạt chuẩn Cloud-Native (12-Factor App) trước khi xây dựng bất kỳ pipeline CI/CD nào.

---

## 1. Quét & Xử Lý Secrets / Thông Tin Nhạy Cảm

### Nguyên Tắc
- Tuyệt đối không commit Connection String, API Key, Token, Password vào Git.
- Cấu hình môi trường Production phải được nạp 100% qua **Biến Môi Trường (Environment Variables)**.
- Môi trường Local dùng file config riêng cho dev (đã đưa vào `.gitignore`) hoặc Secret Manager của hệ điều hành.

### Lệnh Quét Kiểm Tra Trong Mã Nguồn
```bash
# Quét tìm các chuỗi secret phổ biến trong source code
git grep -i -E "(password|secret|apikey|api_key|token|connectionstring)\s*[:=]\s*[\"'][^\"']+[\"']"

# Quét kiểm tra lịch sử commit xem từng có secret bị lọt vào Git hay chưa
git log -S "sk-" --all
git log -S "password=" --all
```

> [!WARNING]
> **Quy trình xử lý khi phát hiện Secret từng bị commit lên Git:**
> 1. **Xoay (Rotate) Secret ngay lập tức**: Vào trang quản trị của nhà cung cấp dịch vụ (OpenAI, DeepSeek, Telegram, Cloud Provider) để hủy key cũ và tạo key mới. Việc xóa commit trong git history chỉ hạn chế lan truyền nhưng key cũ vẫn phải coi như đã bị lộ.
> 2. Cập nhật key mới vào Secret Manager hoặc Environment Variables cục bộ.

---

## 2. Cấu Hình CORS Động (Dynamic CORS)

### Nguyên Tắc
- Không bao giờ fix cứng domain localhost trong source code.
- Cho phép ghi đè linh hoạt qua biến môi trường khi deploy Frontend lên domain độc lập (Vercel, Netlify, Azure Static Web Apps).

### Tiêu Chuẩn Triển Khai
- Thứ tự ưu tiên cấu hình:
  1. **Biến môi trường (`CORS_ALLOWED_ORIGINS`)**: Chuỗi phân tách bởi dấu phẩy (Ưu tiên cao nhất trên Cloud).
  2. **File cấu hình ứng dụng (`Cors:AllowedOrigins`)**: Danh sách mảng JSON.
  3. **Fallback mặc định**: Localhost chỉ áp dụng khi chạy môi trường Development cục bộ.

---

## 3. Kiến Trúc Health Check Probes (Liveness vs Readiness)

### Nguyên Tắc
Không dùng một endpoint `/health` duy nhất để kiểm tra tất cả. Phải phân tách rõ ràng theo chuẩn Cloud Orchestration:

| Probe Loại | Endpoint | Mục Đích | Logic Kiểm Tra | Hành Động Của Cloud Khi Lỗi |
| :--- | :--- | :--- | :--- | :--- |
| **Liveness Probe** | `/health/live` | Kiểm tra tiến trình app còn sống, không bị deadlock / CPU 100% | Chỉ kiểm tra nội tại tiến trình (In-process check), **không gọi DB hay API ngoài** | Restart lại container |
| **Readiness Probe** | `/health/ready` | Kiểm tra ứng dụng đã sẵn sàng phục vụ người dùng | Kiểm tra kết nối Database chính, Cache | Tạm ngắt routing request của khách (không restart container) |

### Xử Lý Dịch Vụ Bên Thứ 3 (AI, 3rd-Party APIs)
- Với các dịch vụ phụ thuộc ngoài (DeepSeek, OpenAI, cổng thanh toán):
  - Bắt buộc giới hạn thời gian Timeout (tối đa 3 - 5 giây).
  - Trả về trạng thái `Degraded` (thay vì `Unhealthy`) khi dịch vụ ngoài gặp lỗi để app chính và các tính năng cốt lõi không bị Cloud kill nhầm.
- Trên môi trường Production, ẩn chi tiết exception nội bộ của `/health` để tránh rò rỉ thông tin hạ tầng.

---

## 4. Đóng Gói Docker Container Multi-Stage & Stateless

### Nguyên Tắc
1. **Multi-Stage Build**:
   - Stage Build: Dùng SDK image đầy đủ để build và restore dependencies. Tận dụng Docker Layer Cache.
   - Stage Final: Dùng Runtime image tối giản nhất, không chứa trình biên dịch để giảm dung lượng (< 150MB) và tăng bảo mật.
2. **Non-Root User**:
   - Chạy container dưới quyền user bị giới hạn (`non-root`), lắng nghe cổng không đặc quyền (ví dụ `8080`).
3. **Container Phải Là Stateless**:
   - Không lưu trữ trạng thái lâu dài hoặc ghi file cấu hình vào thư mục cục bộ của container. Mọi dữ liệu bền vững phải lưu vào Database hoặc Object Storage (S3 / Azure Blob Storage).

---

## 5. Tiêu Chí Hoàn Thành (Definition of Done - Giai Đoạn 0)

Một dự án được coi là hoàn thành Giai đoạn 0 khi:
- [ ] Không còn secret hay fallback hardcode trong mã nguồn; secret lịch sử đã được rotate.
- [ ] CORS nhận domain động từ biến môi trường `CORS_ALLOWED_ORIGINS`.
- [ ] Tách biệt thành công `/health/live` (200 OK) và `/health/ready`.
- [ ] Docker image build thành công với dung lượng tối ưu, container chạy được ở local và phản hồi health probes.


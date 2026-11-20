# 🗺️ Kỳ 9: Bước 1 — Chuẩn Hóa Danh Mục & Toàn Diện CRUD Tour Du Lịch

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**  
> **Chủ đề**: Tách rời định tuyến (Route Decoupling), xóa bỏ hoàn toàn Mock Data, hiện thực hóa 100% CRUD Tour Du Lịch kết nối cơ sở dữ liệu thật PostgreSQL `tour_agent_db`, tái hiện giao diện quản trị Catalog tương đương PHP Core.

---

## 💡 1. Bối Cảnh & Vấn Đề Đặt Ra (The Problem)

Sau khi hoàn thành phần khung kiến trúc và bộ máy suy luận AI ban đầu, giao diện Web của hệ thống vẫn phụ thuộc vào dữ liệu giả lập (Mock Data) được hardcode trên Frontend. Đồng thời, cấu trúc route ban đầu chưa được phân định rõ ràng giữa phân hệ bán hàng đại chúng (Landing Page) và phân hệ quản trị vận hành nội bộ (Admin Portal).

### Điểm nghẽn từ hệ thống PHP cũ:
- Trong mã nguồn PHP Laravel cũ (`kp3-backend-core-main`), trang quản trị sản phẩm lồng ghép trong `welcome.blade.php`, gọi trực tiếp qua AJAX route `/admin/travels` nhưng lại chung đụng state với nhiều module khác.
- Dữ liệu Tour có cấu trúc nghiệp vụ phong phú: Tiêu đề, slug, mã SKU, điểm khởi hành, điểm đến, thời lượng (ngày/đêm), bảng giá niêm yết vs giá khuyến mãi, danh sách ngày khởi hành cụ thể (`departures`), hình ảnh và trạng thái công khai (`is_active`).

> 🎯 **Mục tiêu kỹ thuật của Bước 1**:
> 1. Phân tách rành mạch định tuyến (Routing Separation): Phân hệ Public Client (`/`, `/tour/:slug`) và Phân hệ Admin (`/admin/products`, `/admin/offers`,...).
> 2. Chuyển đổi 100% từ Mock Data sang CSDL PostgreSQL thật trên bảng `travels`.
> 3. Tái hiện trọn vẹn giao diện và nghiệp vụ CRUD (Thêm, Sửa, Xóa, Bật/Tắt trạng thái, Bộ lọc tìm kiếm) tương thích 100% với phiên bản PHP gốc.

---

## 🏗️ 2. Hiện Thực Hóa Backend (.NET 10 Clean Architecture)

### 2.1. Phân lớp Domain & DTOs
- Thực thể [`Travel`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Travels/Travel.cs) đóng vai trò là Aggregate Root quản lý các invariant của tour:
  - Khóa chính `long Id`.
  - Tên tour, mã định danh, mô tả chi tiết, điểm đón, điểm đến.
  - Giá gốc (`OriginalPrice`), Giá bán thực tế (`Price`).
  - Ngày khởi hành dưới dạng JSONB mảng chuỗi ngày.
  - Trạng thái hoạt động `IsActive`.

### 2.2. Xây dựng RESTful API Controller (`ToursController`)
Tại tầng `TourAgent.Api`, triển khai bộ endpoint theo chuẩn RESTful tại [`ToursController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/ToursController.cs):
- `GET /api/v1/tours`: Lấy danh sách tour có hỗ trợ lọc từ khóa tìm kiếm (`q`), phân trang và sắp xếp.
- `GET /api/v1/tours/{id}` hoặc `GET /api/v1/tours/by-slug/{slug}`: Xem chi tiết tour cho trang Public Landing hoặc Admin Modal.
- `POST /api/v1/tours`: Tạo mới tour vào CSDL thật, tự động sinh slug hợp lệ.
- `PUT /api/v1/tours/{id}`: Cập nhật thông tin tour.
- `DELETE /api/v1/tours/{id}`: Xóa tour khỏi hệ thống.
- `PATCH /api/v1/tours/{id}/toggle-status`: Bật/tắt trạng thái hiển thị nhanh trên bảng.

---

## 🎨 3. Hiện Thực Hóa Frontend SPA (React + Tailwind CSS)

### 3.1. Tách biệt Route trong `App.tsx`
Cấu trúc Router được phân định độc lập:
```tsx
<Routes>
  {/* Phân hệ Client Bán Hàng */}
  <Route path="/" element={<LandingPage />} />
  <Route path="/tour/:slug" element={<LandingPage />} />

  {/* Phân hệ Quản Trị Vận Hành */}
  <Route path="/admin/products" element={<AdminProductsPage />} />
  <Route path="/admin" element={<Navigate to="/admin/products" replace />} />
</Routes>
```

### 3.2. Xây dựng `AdminProductsPage.tsx`
Tái hiện 100% thẩm mỹ Dark Theme cao cấp từ PHP:
- **Header Top Navigation**: Nút quay về Dashboard, Huy hiệu Module Sản phẩm, Liên kết nhanh sang các module Bàn Quyết Định (`/admin/escalations`), Kênh Telegram (`/admin/channels/telegram`), Giám Sát Chat (`/admin/conversations`).
- **Thanh Công Cụ Tìm Kiếm & Lọc**: Ô tìm kiếm tour thời gian thực theo tên hoặc điểm đến, bộ lọc theo trạng thái (Tất cả, Đang mở bán, Tạm dừng).
- **Bảng Dữ Liệu Catalog (Data Table)**:
  - Hiển thị ảnh thumbnail, mã tour, tên hành trình, thời lượng (ví dụ: `4N3Đ`).
  - So sánh giá gốc gạch ngang với giá ưu đãi nổi bật.
  - Badge trạng thái trực quan (Xanh: Mở bán, Đỏ: Tạm dừng).
  - Cột thao tác: Sửa tour (mở Drawer/Modal), Bật/Tắt nhanh, Xóa tour có xác nhận bảo vệ.
- **Modal Nhập Liệu Thêm/Sửa Tour**: Đầy đủ các trường: Tiêu đề, Điểm khởi hành, Điểm đến, Thời lượng, Giá bán, Giá gốc, Danh sách ngày khởi hành phân tách bằng dấu phẩy, URL ảnh minh họa và Mô tả lịch trình tóm tắt.

---

## 🌟 4. Giá Trị Trình Bày Khi Phỏng Vấn (Talking Points for Interviewers)

1. *"Khi chuyển từ PHP Monolith sang .NET + React, bạn làm thế nào để đảm bảo hệ thống không bị xáo trộn luồng nghiệp vụ?"*  
   ➔ **Trả lời**: *"Tôi áp dụng chiến lược 'Strangler Fig Pattern' theo từng lát cắt nghiệp vụ (Vertical Slices). Bước đầu tiên là chuẩn hóa Catalog Sản phẩm (Tours) — thành phần nền tảng dữ liệu cho toàn bộ hệ thống. Tôi tách rành mạch router Client và Admin, xây dựng CRUD Controller chuẩn RESTful kết nối PostgreSQL thật và loại bỏ 100% mock data."*

2. *"Tại sao không dùng các thư viện sinh CRUD tự động mà lại viết Controller thủ công?"*  
   ➔ **Trả lời**: *"Trong kiến trúc Clean Architecture, tầng API phải kiểm soát chặt chẽ hợp đồng giao tiếp (Contract), DTO validation và mapping. Việc viết Controller tường minh giúp kiểm soát tốt HTTP Status Codes, dễ dàng viết Integration Tests và không bị phụ thuộc vào các thư viện ma thuật bên thứ ba."*

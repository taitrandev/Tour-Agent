# 📦 Bước 5: Quản Lý Đơn Tour & Đường Ống Visa (Order & Visa Pipeline Desk)

> **Mục tiêu kỹ thuật:** Chuyển đổi và nâng cấp toàn diện phân hệ quản lý đơn đặt tour và theo dõi tiến trình làm Visa (`tour_orders`) từ PHP Laravel sang .NET 10 Clean Architecture. Phân định rạch ròi giữa phễu chuyển đổi thông tin khách hàng (Contact Conversion) và chu trình chốt đơn B2B2C (Order Fulfillment).

---

## 1. Bối Cảnh Nghiệp Vụ: Mô Hình Agency Du Lịch B2B2C

Trong mô hình kinh doanh du lịch Outbound (Nhật Bản, Hàn Quốc, Châu Âu), một đại lý bán lẻ (Agency) không tự tổ chức toàn bộ đoàn mà vận hành theo chuỗi liên kết giá trị B2B2C:

```
[Khách Hàng] 
     │ (Chat tư vấn & Cung cấp SĐT)
     ▼
[AI Agent / Trụ Cột Conversion] ➔ Tự động bóc tách SĐT & Tạo Hot Lead Contact
     │ (Sales chốt tour)
     ▼
[Tour Order Pipeline]
     ├── Bước 1: Thu cọc 50% (Deposit Pending ➔ Paid)
     ├── Bước 2: Nộp hồ sơ Visa sang Cty Du Lịch đối tác (Saigontourist, Vietravel...)
     ├── Bước 3: Đậu Visa ➔ Tự động sinh tin nhắn Zalo chúc mừng & Nhắc thu 50% còn lại
     └── Bước 4: Trước ngày bay 3 ngày (T-3) ➔ Bàn giao Hướng Dẫn Viên & Gửi lịch hẹn sân bay
```

### ❓ Trả lời câu hỏi kiến trúc: *"Khi khách cho SĐT thì luồng thêm vào Contact được xử lý ở đâu?"*
- **Sự cố thực tế:** Người dùng thắc mắc tại sao khách hàng cung cấp số điện thoại trong phòng chat Telegram nhưng không thấy xuất hiện trong Contact CRM, liệu luồng này nằm ở Order hay đang implement thiếu?
- **Bản chất kiến trúc:**
  - **Không nằm ở Order:** Nếu chỉ tạo Contact khi khách chốt Order, doanh nghiệp sẽ **đánh rơi 80% khách hàng tiềm năng** (những người đã cho SĐT để xin tư vấn nhưng chưa xuống tiền cọc).
  - **Nằm ở Inbound Channel Pipeline (Pillar 3 Conversion):** Mọi tin nhắn gửi đến từ Telegram, Zalo hoặc Web Widget phải đi qua một bộ giải mã tự động (`ContactResolver`). Khi phát hiện chuỗi số điện thoại Việt Nam hợp lệ (Regex `(?:\+?84|0)(3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}\b`), hệ thống:
    1. Chuẩn hóa về định dạng 10 số `0xxxxxxxxx`.
    2. Tìm hoặc tạo mới bản ghi trong `crm_contacts`.
    3. Tự động thăng hạng khách hàng lên **Hot Lead** (`stage = 'hot'`).
    4. Gắn kết `conversation.LinkContact(contact.Id)` để liên kết toàn bộ lịch sử tư vấn.
  - Khi nhân viên Sales chuyển hóa cơ hội thành đơn hàng thực tế ở Bước 5, `TourOrder` chỉ cần liên kết tới `ContactId` đã có sẵn.

---

## 2. Thiết Kế Kiến Trúc 4 Tầng (.NET 10 Clean Architecture)

### 2.1. Domain Layer (`TourAgent.Domain/Orders/TourOrder.cs`)
- Thực thể `TourOrder` được xây dựng theo phong cách **Rich Domain Model**, bảo vệ toàn vẹn các ràng buộc bất biến (Invariants):
  - Số lượng người lớn `PaxAdults >= 1`, tổng giá trị tour `TotalPrice >= 0`.
  - Tỷ lệ cọc mặc định 50%, tự động tính `DepositAmount` và `RemainingAmount`.
  - Các phương thức thay đổi trạng thái theo nghiệp vụ:
    - `MarkDepositPaid(string? accountNote)`: Ghi nhận cọc 50%, chuyển đơn sang `visa_processing` hoặc `final_payment_pending`.
    - `UpdateVisaStatus(string status, string? notes, ...)`: Cập nhật tiến trình Visa; nếu trạng thái là `approved` (Đậu) thì tự động chuyển sang `final_payment_pending`.
    - `MarkRemainingPaid()`: Hoàn tất 100% thanh toán, chuyển sang `ready_to_depart`.
    - `MarkT3Notified(...)`: Ghi nhận thông tin Hướng Dẫn Viên (Họ tên, SĐT, Điểm hẹn) và mốc thời gian dặn dò T-3.
    - `GetZaloVisaSuccessMessage(...)` & `GetZaloT3DepartureMessage(...)`: Tự động soạn sẵn kịch bản tin nhắn cá nhân hóa chuẩn chỉnh cho Sales sao chép gửi Zalo/Telegram.

### 2.2. Infrastructure Layer
- **Entity Configuration (`TourOrderConfiguration.cs`):**
  - Ánh xạ bảng `tour_orders` trên PostgreSQL.
  - Cấu hình Global Query Filter cho Soft Delete: `builder.HasQueryFilter(o => o.DeletedAt == null);`.
  - Cấu hình chỉ mục tối ưu tìm kiếm: `order_code` (Unique), `status`, `visa_status`, `departure_date`.
- **Database Seeder (`OrderDataSeeder.cs`):**
  - Thực thi DDL tự động `CREATE TABLE IF NOT EXISTS tour_orders ...` giúp hệ thống tự phục hồi mà không cần can thiệp tay.
  - Tự động nạp 5-6 đơn hàng mẫu phủ kín toàn bộ các giai đoạn trong pipeline (Chờ tư vấn, Chờ cọc, Đang làm visa, Chờ thu đợt 2, Sắp bay T-3).

### 2.3. Application Layer (CQRS Direct Handlers)
- `GetOrdersQuery` + `GetOrdersQueryHandler`: Hỗ trợ lọc theo trạng thái (`status`), từ khóa tìm kiếm (`search` mã đơn, tên khách, SĐT, tên tour), phân trang chuẩn và tính toán **Real-time Pipeline Metrics** trong 1 lượt query.
- `GetOrderByIdQuery`: Lấy chi tiết hồ sơ đơn hàng kèm đối tượng khách hàng và tour.
- `CreateTourOrderCommand`: Tạo mới đơn hàng tour thủ công từ Sales.
- `UpdateTourOrderCommand`: Cập nhật thông tin chi tiết đơn hàng.
- `MarkDepositCommand`: Xác nhận cọc 50%, tự động nâng `stage` của Contact lên `hot`.
- `UpdateVisaStatusCommand`: Cập nhật tiến trình Visa đối tác, trả về mẫu tin nhắn Zalo chúc mừng đậu Visa.
- `MarkFinalPaymentCommand`: Xác nhận thu 100%, nâng `stage` của Contact lên `customer`.
- `MarkT3NotifiedCommand`: Lưu thông tin HDV và dặn dò khách trước ngày bay 3 ngày.
- `GetZaloTemplateQuery`: Lấy mẫu tin nhắn Zalo theo yêu cầu.
- `DeleteTourOrderCommand`: Xóa mềm đơn hàng.

### 2.4. API Controller Layer (`TourOrdersController.cs`)
- Cung cấp trọn vẹn cả 2 phong cách định tuyến:
  - Route quản trị: `/api/v1/admin/orders`
  - Route RESTful: `/api/v1/orders`
- Đầy đủ các endpoint nghiệp vụ:
  - `GET /api/v1/admin/orders`
  - `GET /api/v1/admin/orders/{id}`
  - `POST /api/v1/admin/orders`
  - `PUT /api/v1/admin/orders/{id}`
  - `POST/PATCH /api/v1/admin/orders/{id}/deposit`
  - `POST/PATCH /api/v1/admin/orders/{id}/visa`
  - `POST/PATCH /api/v1/admin/orders/{id}/final-payment`
  - `POST/PATCH /api/v1/admin/orders/{id}/t3-notified`
  - `GET /api/v1/admin/orders/{id}/zalo-template/{type}`
  - `DELETE /api/v1/admin/orders/{id}`

---

## 3. Nhật Ký Giải Quyết Các Sự Cố Kỹ Thuật (Post-Mortem Lessons)

### 🛠️ Sự cố 1: `CS1061: 'TelegramInboundMessage' does not contain a definition for 'Username'`
- **Nguyên nhân:** Khi tạo `ContactResolver`, hàm gọi `inbound.Username` nhưng bản ghi `TelegramInboundMessage` trước đó chỉ có `ChatId`, `Text`, `ChannelMessageId`, `SenderName`.
- **Khắc phục:** Bổ sung tham số tùy chọn `string? Username = null` vào `TelegramInboundMessage.cs` và cập nhật `TelegramWebhookController.cs` để trích xuất cả `username` từ Telegram Webhook JSON Payload.

### 🛠️ Sự cố 2: `CS0246: The type or namespace name 'Travel' could not be found`
- **Nguyên nhân:** Trong PHP core sử dụng tên model `Travel`, nhưng trong .NET Domain model đã được chuẩn hóa thành `Tour` và `TourDeparture`.
- **Khắc phục:** Cập nhật navigation properties trong `TourOrder.cs` sang `public Tour? Travel` và `public TourDeparture? Departure`, đồng thời giữ nguyên tên trường JSON `travel` và `title` trong DTO để tương thích 100% với giao diện Frontend.

### 🛠️ Sự cố 3: `column t.CreatedAtUtc does not exist` (EF Core Shadow Property)
- **Nguyên nhân:** Thực thể `TourOrder` kế thừa từ `Entity<long>`, trong khi `Entity<T>` có khai báo sẵn `CreatedAtUtc` và `UpdatedAtUtc`. Do bảng CSDL hiện hữu của PHP chỉ có cột `created_at` và `updated_at`, EF Core mặc định cố gắng truy vấn cột shadow `CreatedAtUtc` trên bảng `tour_orders`.
- **Khắc phục:** Thêm khai báo tường minh trong `TourOrderConfiguration.cs`:
  ```csharp
  builder.Ignore(o => o.CreatedAtUtc);
  builder.Ignore(o => o.UpdatedAtUtc);
  ```

### 🛠️ Sự cố 4: `InvalidCastException: Reading as 'System.DateTimeOffset' is not supported for 'timestamp without time zone'`
- **Nguyên nhân:** Trình điều khiển Npgsql của PostgreSQL có quy tắc kiểu dữ liệu cực kỳ khắt khe:
  - Cột `timestamp without time zone` bắt buộc phải map vào `System.DateTime` (Kind = Unspecified/Utc).
  - Cột `timestamp with time zone` mới được map vào `System.DateTimeOffset`.
  - Ban đầu `TourOrder` sử dụng `DateTimeOffset`, khiến Npgsql ném ngoại lệ khi deserialize bản ghi CSDL.
- **Khắc phục:** Chuẩn hóa các trường thời gian (`DepositPaidAt`, `RemainingPaidAt`, `PreDepartureNotifiedAt`, `CreatedAt`, `UpdatedAt`, `DeletedAt`) trong `TourOrder.cs` và `TourOrderDtos.cs` sang kiểu `DateTime` và `DateTime?`.

---

## 4. Frontend SPA: Giao Diện Quản Trị Đơn Tour & Visa (`AdminOrdersPage.tsx`)

Tái hiện hoàn chỉnh giao diện Dark Glassmorphism chuẩn cao cấp từ `admin/orders.blade.php`:
1. **6 Thẻ Chỉ Số Đo Lường Real-time:**
   - 📦 **Tổng Đơn**: Toàn bộ quy mô pipeline.
   - 🔥 **Chờ Tư Vấn**: Các Hot Lead có SĐT cần Sales gọi điện mở lời ngay.
   - 💵 **Chờ Cọc 50%**: Khách đã nhận báo giá và STK ngân hàng.
   - 🛂 **Đang Làm Visa**: Hồ sơ đang nằm tại Cty du lịch đối tác hoặc Lãnh sự quán.
   - 💰 **Cần Thu Nốt 50%**: Các đơn đã có kết quả ĐẬU VISA, cần thu tiền trước ngày xuất vé bay.
   - ✈️ **Sắp Bay (T-3)**: Các đơn khởi hành trong 3 ngày tới, cần bàn giao HDV và dặn dò lịch tập trung.
2. **Bộ Lọc Đa Năng & Tìm Kiếm Tức Thì:** Lọc nhanh theo từng trạng thái pipeline và tìm kiếm mã đơn, tên khách, số điện thoại, tên tour.
3. **Bảng Danh Sách Trực Quan:** Hiển thị chi tiết từng đợt thanh toán (Đợt 1 / Đợt 2), phù hiệu tiến trình Visa và thông tin Hướng Dẫn Viên.
4. **Hệ Thống 6 Modals Nghiệp Vụ:**
   - **Modal Deal Briefing (30s Review):** Tóm tắt cuộc gọi từ AI Agent, gợi ý câu mở lời đắt giá cho Sales trước khi bấm gọi khách.
   - **Modal Xác Nhận Cọc 50%:** Đối soát mã Ref ngân hàng, tự động thăng hạng khách thành Hot Lead trong CRM.
   - **Modal Cập Nhật Visa:** Quản lý đối tác và mã booking; tự động soạn tin nhắn Zalo chúc mừng đậu Visa kèm nút sao chép 1-click.
   - **Modal Thu Nốt 50%:** Xác nhận hoàn tất 100% tiền tour, tự động thăng hạng khách thành Customer trong CRM.
   - **Modal Dặn Dò T-3:** Bàn giao Hướng Dẫn Viên và soạn sẵn tin nhắn dặn dò chuyến bay gửi khách qua Zalo.
   - **Modal Thêm / Sửa Đơn Tour:** Tạo mới hoặc cập nhật thông tin đơn hàng thủ công linh hoạt.

---

## 5. Kết Quả Kiểm Thử & Xác Nhận Hệ Thống

- **Unit & Architecture Tests:** Đạt **56/56 tests xanh lá** (35 Domain Tests, 18 Application Tests, 3 NetArchTest Rules).
- **Frontend Build:** `tsc -b && vite build` biên dịch thành công 100% không cảnh báo lỗi type.
- **API Runtime Live Data:** Endpoint `/api/v1/admin/orders` phản hồi 200 OK với dữ liệu thực tế từ cơ sở dữ liệu PostgreSQL.

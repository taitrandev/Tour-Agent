# 👥 Kỳ 12: Bước 4 — Quản Trị Khách Hàng Tập Trung (Core CRM Contacts 360°) & Trụ Cột Conversion

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**  
> **Chủ đề**: Tái thiết kế Trụ Cột Conversion (Pillar 3) theo chuẩn Clean Architecture .NET 10, quản lý hồ sơ khách hàng 360° độc lập với nghiệp vụ du lịch, tự động nhận diện và thăng hạng Hot Lead (khi có SĐT), liên kết lịch sử hội thoại đa kênh (Telegram / Web / Zalo), và xây dựng giao diện CRM Desk hiện đại.

---

## 💡 1. Bối Cảnh & Vấn Đề Kỹ Thuật (The Challenge)

Trong các hệ thống bán hàng và tư vấn tự động bằng AI, dữ liệu khách hàng thường bị phân mảnh nghiêm trọng:
1. **Phân mảnh theo kênh (Channel Silos)**: Khách hàng nhắn tin qua Telegram Bot có một ID riêng, qua Web Widget có một session riêng, qua Zalo hay gọi hotline lại là một danh sách độc lập. Nhân viên tư vấn không có cái nhìn 360° về lịch sử tương tác của cùng một khách hàng.
2. **Khớp nối quá chặt với nghiệp vụ du lịch (Domain Coupling)**: Trong các hệ thống cũ, thông tin khách hàng thường bị gắn cứng vào bảng `bookings` hoặc `tour_orders`. Nếu khách hàng chỉ mới hỏi thăm, chưa đặt tour hoặc chỉ tham khảo chính sách thì hệ thống không có nơi lưu trữ hồ sơ và phân hạng tiềm năng.
3. **Mất dấu khách hàng tiềm năng cao (Lost Hot Leads)**: Khi khách hàng cung cấp số điện thoại trong lúc chat với AI, nếu không có cơ chế tự động nhận diện và nâng phân hạng lên `hot`, đội ngũ bán hàng (Sale Desk) sẽ bỏ lỡ thời điểm vàng (Golden Window) để gọi điện chốt đơn.

> 🎯 **Mục tiêu kỹ thuật của Bước 4**:
> 1. Xây dựng thực thể trung tâm [`CrmContact.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Contacts/CrmContact.cs) đại diện cho hồ sơ khách hàng 360°, hoàn toàn độc lập với booking du lịch.
> 2. Quản lý 5 giai đoạn vòng đời khách hàng chuẩn hóa: `prospect` (mới tiếp cận), `warm` (quan tâm), `hot` (có SĐT, sẵn sàng mua), `customer` (đã mua hàng), `lost` (hủy / từ chối).
> 3. Tự động nhận diện số điện thoại: Khi khách hàng có số điện thoại mà chưa chỉ định stage, hệ thống tự động thăng hạng lên `hot`.
> 4. Liên kết quan hệ 1-N giữa `crm_contacts` và `conversations`, cho phép từ màn hình CRM mở thẳng tới phiên chat tương ứng trên [`/admin/conversations?id=...`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminConversationsPage.tsx).
> 5. Tái hiện 100% giao diện và nghiệp vụ từ `contacts.blade.php` của PHP core sang React SPA [`AdminContactsPage.tsx`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminContactsPage.tsx).

---

## 🏗️ 2. Kiến Trúc Kỹ Thuật Triển Khai (Clean Architecture)

### 2.1. Domain Layer (`TourAgent.Domain`)
Tạo thực thể [`CrmContact.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Contacts/CrmContact.cs) kế thừa từ `Entity`:
- **Thuộc tính**:
  - `Name`, `Phone`, `Email`: Thông tin liên hệ cơ bản.
  - `TelegramChatId`, `TelegramUsername`: Định danh Telegram (tự động loại bỏ tiền tố `@`).
  - `ZaloId`: Định danh khách hàng trên Zalo.
  - `Channel`: Kênh tiếp cận ban đầu (`telegram`, `web_widget`, `zalo`, `manual`).
  - `Stage`: Phân hạng vòng đời (`prospect`, `warm`, `hot`, `customer`, `lost`).
  - `TagsJson`: Nhãn phân loại dạng JSON.
  - `Notes`: Ghi chú nhu cầu và sở thích của khách hàng.
  - `LastActiveAtUtc`: Thời điểm tương tác gần nhất.
  - `DeletedAtUtc`: Cột đánh dấu xóa mềm (Soft Delete).
- **Quy tắc nghiệp vụ (Domain Rules & Invariants)**:
  - `Create(...)`: Factory method kiểm tra tính hợp lệ của stage. Nếu có `phone` mà chưa truyền `stage` thì tự động nâng lên `StageHot`.
  - `Update(...)`: Cập nhật thông tin và kiểm tra hợp lệ của stage.
  - `RecordActivity()`: Cập nhật `LastActiveAtUtc = DateTime.UtcNow`.
  - `MarkAsHot()`: Thăng hạng trực tiếp lên `StageHot`.
  - `SoftDelete()`: Đánh dấu thời điểm xóa mềm, không xóa vật lý khỏi CSDL.

### 2.2. Infrastructure Layer (`TourAgent.Infrastructure`)
- **Mapping EF Core**: [`CrmContactConfiguration.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/CrmContactConfiguration.cs)
  - Ánh xạ vào bảng `crm_contacts` với kiểu cột snake_case.
  - Cấu hình chỉ mục tối ưu hóa tìm kiếm: `idx_crm_contacts_phone`, `idx_crm_contacts_email`, `idx_crm_contacts_telegram_chat_id`, `idx_crm_contacts_stage`, `idx_crm_contacts_last_active`.
  - Bộ lọc toàn cục xóa mềm: `builder.HasQueryFilter(c => c.DeletedAtUtc == null);`.
- **Database Context**:
  - Bổ sung `DbSet<CrmContact> Contacts { get; }` vào [`IAppDbContext.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/IAppDbContext.cs) và [`AppDbContext.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/AppDbContext.cs).
- **Data Seeder & Auto Migration**: [`ContactDataSeeder.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/ContactDataSeeder.cs)
  - Chạy câu lệnh DDL `CREATE TABLE IF NOT EXISTS crm_contacts ...`.
  - Đảm bảo bảng `conversations` có khóa ngoại `contact_id BIGINT NULL REFERENCES crm_contacts(id) ON DELETE SET NULL`.
  - Khởi tạo sẵn các hồ sơ khách hàng mẫu đại diện cho các kênh và stage nếu bảng rỗng.

### 2.3. Application Layer (`TourAgent.Application`)
Tổ chức theo mô hình CQRS Handlers tại `TourAgent.Application/Features/Contacts`:
- [`GetContactsQuery.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Features/Contacts/GetContactsQuery.cs): Hỗ trợ lọc theo `stage`, `channel`, tìm kiếm theo tên/SĐT/username, tính số lượng phiên chat (`conversations_count`) và phân trang chuẩn.
- [`GetContactByIdQuery.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Features/Contacts/GetContactByIdQuery.cs): Trả về hồ sơ 360° kèm danh sách toàn bộ các cuộc hội thoại đa kênh của khách hàng đó.
- [`CreateContactCommand.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Features/Contacts/CreateContactCommand.cs): Tiếp nhận tạo mới khách hàng thủ công hoặc từ kênh tích hợp.
- [`UpdateContactCommand.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Features/Contacts/UpdateContactCommand.cs): Cập nhật thông tin và nhu cầu của khách hàng.
- [`DeleteContactCommand.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Features/Contacts/DeleteContactCommand.cs): Thực hiện xóa mềm an toàn.

### 2.4. API Layer (`TourAgent.Api`)
Cung cấp bộ RESTful API tại [`ContactsController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/ContactsController.cs):
- Hỗ trợ cả 2 định dạng đường dẫn: chuẩn quản trị (`/api/v1/admin/contacts`) và chuẩn RESTful (`/api/v1/contacts`).
- `GET /api/v1/admin/contacts`: Danh sách kèm bộ lọc và phân trang.
- `GET /api/v1/admin/contacts/{id}`: Chi tiết hồ sơ 360° và các phiên chat.
- `POST /api/v1/admin/contacts`: Tạo khách hàng mới (Status 201 Created).
- `PUT /api/v1/admin/contacts/{id}`: Cập nhật thông tin khách hàng.
- `DELETE /api/v1/admin/contacts/{id}`: Xóa mềm khách hàng.

---

## 💻 3. Giao Diện Người Dùng React SPA (`AdminContactsPage.tsx`)

Trang quản trị [`AdminContactsPage.tsx`](file:///D:/Nghĩa/AI-Agent-Travel/tour-agent-frontend/src/pages/AdminContactsPage.tsx) tái hiện 100% phong cách thiết kế Dark Glassmorphism từ PHP core:

1. **Top Navigation Header**:
   - Nút quay lại Dashboard Quản Trị.
   - Tiêu đề "Hồ Sơ Khách Hàng Tập Trung (Core CRM Contacts 360°)" kèm huy hiệu "Trụ Cột Conversion".
   - Bộ nút điều hướng nhanh: `⚡ Grand Slam Offers`, `💬 Giám Sát Hội Thoại`, `+ Thêm Khách Hàng`, nút làm mới dữ liệu.
2. **Metrics Overview Grid (4 thẻ thống kê live)**:
   - 👥 **Tổng Khách Hàng**: Đếm tổng số khách trên toàn hệ thống.
   - 🔥 **Hot Leads (Có SĐT)**: Đếm số khách hàng tiềm năng cao sẵn sàng chốt đơn.
   - 📱 **Kênh Telegram**: Thống kê số khách tiếp cận qua Telegram Bot.
   - 💻 **Kênh Web Chat**: Thống kê số khách trao đổi qua Website Widget.
3. **Filter & Search Controls**:
   - Thanh chip lọc nhanh theo Stage: `Tất cả`, `🔥 Hot Lead`, `Warm`, `Prospect`, `Khách Hàng`.
   - Ô tìm kiếm thời gian thực theo Tên, Số điện thoại hoặc `@username`.
4. **Bảng Danh Bạ Khách Hàng (Contacts Table)**:
   - Hiển thị Tên, `@telegram_username`, SĐT định dạng font-mono nổi bật.
   - Huy hiệu kênh tiếp cận (Telegram, Zalo, Web, Trực tiếp).
   - Huy hiệu phân hạng stage (Hot Lead có hiệu ứng pulse cảnh báo).
   - Cột số phiên chat kèm thời điểm hoạt động cuối cùng (`vi-VN`).
   - Nút thao tác nhanh: `🔍 Xem`, `✏️ Sửa`, `🗑️ Xóa`.
5. **Modal Hồ Sơ 360° & Lịch Sử Hội Thoại (Detail Modal)**:
   - Khung thông tin cá nhân: Họ tên, SĐT, Email, Ghi chú & Nhu cầu.
   - Danh sách các phiên hội thoại đa kênh: Hiển thị kênh, số tin nhắn, tin nhắn cuối và liên kết mở trực tiếp sang `/admin/conversations?id={conv.id}`.
6. **Modal Thêm Mới & Chỉnh Sửa Khách Hàng**:
   - Đầy đủ các trường: Tên, SĐT (bắt buộc khi tạo), Kênh tiếp cận, Email, Stage, Ghi chú nhu cầu tour.
7. **Hệ thống Toast Notification**:
   - Phản hồi tức thì trạng thái thành công hoặc lỗi tại góc màn hình.

---

## 🧪 4. Kết Quả Kiểm Thử & Nghiệm Thu (Verification)

| Hạng mục kiểm tra | Công cụ / Môi trường | Kết quả đạt được | Chi tiết kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Domain Unit Tests** | xUnit (.NET 10) | **30/30 PASSED (100%)** | Bao gồm `CrmContactTests` kiểm thử auto-detect SĐT, thăng hạng hot, validate stage, soft delete |
| **Application Unit Tests** | xUnit (.NET 10) | **14/14 PASSED (100%)** | Kiểm thử CQRS handlers và validation pipelines |
| **Architecture Tests** | NetArchTest (.NET 10) | **3/3 PASSED (100%)** | Đảm bảo Domain không phụ thuộc tầng ngoài, Clean Architecture tuyệt đối |
| **Tổng số Tests Backend** | `dotnet test TourAgent.slnx` | **47/47 PASSED (100%)** | Toàn bộ solution xanh lá |
| **Frontend TypeScript Build** | `tsc -b && vite build` | **BUILD CLEAN (0 ERRORS)** | 1905 modules transformed thành công trong 665ms |
| **API Endpoints Testing** | `Invoke-RestMethod` (PowerShell) | **200 OK & 201 CREATED** | Test GET danh sách, POST tạo khách hàng mới tự động chuyển `stage: hot`, GET chi tiết 360° |

---

## 🎯 5. Trạng Thái Hoàn Thành & Lộ Trình Tiếp Theo

- [x] **Bước 1**: Sản Phẩm Tour & Catalog Base (`/admin/products`) — *Đã hoàn thành (Kỳ 09)*
- [x] **Bước 2**: Grand Slam Offers & Value Stack (`/admin/offers`) — *Đã hoàn thành (Kỳ 10)*
- [x] **Bước 3**: Giám Sát Hội Thoại & AI Telemetry (`/admin/conversations`) — *Đã hoàn thành (Kỳ 11)*
- [x] **Bước 4**: Hồ Sơ Khách Hàng CRM 360° (`/admin/contacts`) — *Đã hoàn thành (Kỳ 12)*
- [ ] **Bước 5**: Bàn Quyết Định & Escalations (`/admin/escalations`)
- [ ] **Bước 6**: Quản Lý Đơn Tour & Xử Lý Hồ Sơ Visa (`/admin/orders`)

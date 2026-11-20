# 🗺️ Kỳ 10: Bước 2 — Đóng Gói Grand Slam Offer & Value Stack (Triết Lý Alex Hormozi)

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**  
> **Chủ đề**: Mô hình hóa nghiệp vụ cao cấp (Domain-Driven Design) cho phân hệ Grand Slam Offer & Value Stack theo công thức kinh điển của Alex Hormozi ($100M Offers), lưu trữ cấu trúc JSONB trong PostgreSQL, và xây dựng giao diện tương tác tính toán giá trị thực tế.

---

## 💡 1. Bối Cảnh Nghiệp Vụ: Tại Sao Cần Grand Slam Offer?

Trong ngành du lịch lữ hành, việc cạnh tranh đơn thuần về giá (Price War) luôn dẫn đến biên lợi nhuận mỏng và tỷ lệ chuyển đổi thấp. Trong hệ thống PHP gốc, phân hệ **Offer Architect** được thiết kế dựa trên triết lý **Grand Slam Offer** của Alex Hormozi và Sabri Suby nhằm tạo ra những "lời đề nghị không thể chối từ":
- **Core Promise & Tagline**: Cam kết giá trị cốt lõi giải quyết đúng nỗi đau của khách hàng mục tiêu.
- **Hero Mechanism**: Cơ chế độc quyền tạo sự khác biệt hoàn toàn với đối thủ cạnh tranh.
- **Value Stack (Chồng Giá Trị)**: Không bán một tour đơn lẻ, mà đóng gói kèm các quà tặng (Bonus Items) như: vé cáp treo VIP, bảo hiểm du lịch quốc tế 2 tỷ, suất ăn buffet 5 sao, đưa đón sân bay riêng... làm tăng vọt giá trị cảm nhận (Perceived Value).
- **Đảo Ngược Rủi Ro (Risk Reversal & Guarantees)**: Cam kết hoàn tiền 100% nếu không hài lòng, hỗ trợ đổi ngày bay miễn phí.
- **Tính Khan Hiếm & Khẩn Cấp (Scarcity & Urgency)**: Giới hạn số chỗ mỗi tuần (`max_slots_per_week`) và đồng hồ đếm ngược chốt deal (`urgency_hours`).

> 🎯 **Mục tiêu kỹ thuật của Bước 2**:
> 1. Thiết kế Rich Domain Model cho `Offer` và `OfferItem` hỗ trợ các quy tắc toàn vẹn nghiệp vụ (Business Invariants).
> 2. Ánh xạ cơ sở dữ liệu với EF Core 10, tận dụng kiểu dữ liệu `JSONB` của PostgreSQL để lưu trữ cấu trúc động (`guarantees`, `payment_terms`).
> 3. Xây dựng CRUD API Handlers và giao diện React SPA tại `/admin/offers` với bộ soạn thảo Value Stack trực quan.

---

## 🏗️ 2. Hiện Thực Hóa Backend (.NET 10 Clean Architecture)

### 2.1. Thiết Kế Rich Domain Model (`TourAgent.Domain.Offers`)
- Thực thể [`Offer`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Offers/Offer.cs):
  - Khóa chính `long Id`, liên kết tùy chọn với `TravelId` (tour cơ sở).
  - Thuộc tính định giá: `OriginalPrice` (tổng giá trị niêm yết của stack), `OfferPrice` (giá ưu đãi thực tế chốt deal), `DepositPercentage` (tỷ lệ cọc, mặc định 30%), `DepositAmount` (tự động tính).
  - Thuộc tính giới hạn: `AvailableSlots`, `MaxSlotsPerWeek`, `UrgencyHours`.
  - Thuộc tính JSONB: `GuaranteesJson` (danh sách cam kết), `PaymentTermsJson` (điều khoản thanh toán).
  - Quan hệ One-to-Many với [`OfferItem`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Offers/OfferItem.cs).
- Thực thể [`OfferItem`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Offers/OfferItem.cs):
  - Phân loại hạng mục: `core` (dịch vụ chính), `bonus` (quà tặng kèm), `guarantee` (cam kết dịch vụ).
  - Thuộc tính `OriginalValue` dùng để cộng dồn vào tổng giá trị cảm nhận của bộ stack.
  - Cờ `IsHighlighted` làm nổi bật quà tặng độc quyền khi tư vấn.

### 2.2. Ánh Xạ EF Core & PostgreSQL JSONB
Tại [`OfferConfiguration.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/OfferConfiguration.cs) và [`OfferItemConfiguration.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/OfferItemConfiguration.cs):
- Cấu hình quan hệ Cascade Delete: Khi xóa một Offer, toàn bộ các `offer_items` con được dọn dẹp sạch sẽ trong một transaction.
- Các cột `guarantees` và `payment_terms` được ánh xạ sang kiểu `jsonb` tối ưu cho việc truy vấn và mở rộng trong tương lai.

### 2.3. Bộ Điều Khiển API (`OffersController`)
Triển khai bộ endpoint tại [`OffersController.cs`](file:///D:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Api/Controllers/OffersController.cs):
- `GET /api/v1/offers`: Lấy danh sách kèm phân trang, lọc theo trạng thái (`draft`, `published`, `archived`) và từ khóa.
- `GET /api/v1/offers/{id}`: Trả về chi tiết Offer đầy đủ kèm mảng `items`.
- `POST /api/v1/offers`: Tạo mới Offer cùng toàn bộ danh sách Value Stack Items.
- `PUT /api/v1/offers/{id}`: Cập nhật thông tin Offer và đồng bộ các item con.
- `DELETE /api/v1/offers/{id}`: Xóa Offer khỏi hệ thống.
- `PATCH /api/v1/offers/{id}/status`: Chuyển đổi trạng thái nhanh (`draft` ➔ `published`).

---

## 🎨 3. Hiện Thực Hóa Frontend SPA (`AdminOffersPage.tsx`)

Trang quản trị tại đường dẫn `/admin/offers` được thiết kế tương thích 100% blade PHP:
1. **Cards Thống Kê Tổng Quan**:
   - Tổng số Offers đang có trong hệ thống.
   - Số Offers đang công khai (`Published`).
   - Tổng giá trị cảm nhận trung bình của Value Stack.
2. **Danh Sách Dạng Lưới (Offer Grid View)**:
   - Thẻ hiển thị nổi bật Hero Mechanism, Tagline và Core Promise.
   - Box so sánh trực quan: Tổng giá trị thực tế của Value Stack (gạch ngang màu xám) đối chiếu với Giá chốt deal (chữ vàng nổi bật).
   - Danh sách tóm tắt các quà tặng Bonus Items kèm biểu tượng tích xanh.
   - Thanh hiển thị số lượng chỗ còn lại trong tuần (`AvailableSlots / MaxSlotsPerWeek`).
3. **Modal Soạn Thảo Value Stack Động**:
   - Cho phép Quản lý linh hoạt thêm bớt các item quà tặng (tên item, mô tả, giá trị ước tính `OriginalValue`).
   - Tự động cộng dồn tức thì tổng giá trị Value Stack khi người dùng thay đổi giá trị từng quà tặng.
   - Tự động tính toán số tiền cọc tương ứng (`DepositAmount = OfferPrice * DepositPercentage / 100`).

---

## 🌟 4. Giá Trị Trình Bày Khi Phỏng Vấn (Talking Points for Interviewers)

1. *"Làm thế nào bạn giải quyết bài toán lưu trữ các trường dữ liệu linh hoạt (như danh sách quà tặng, điều khoản cam kết) trong Relational Database?"*  
   ➔ **Trả lời**: *"Tôi kết hợp sức mạnh của mô hình quan hệ (RDBMS) và kiểu dữ liệu bán cấu trúc (Semi-structured JSONB) của PostgreSQL. Các quan hệ cốt lõi cần ràng buộc toàn vẹn dữ liệu như `Offer -> OfferItem` được thiết kế bảng riêng với Foreign Key và Cascade Delete. Còn các danh sách linh hoạt như điều khoản cam kết (`guarantees`) được lưu dưới dạng cột `JSONB` với EF Core Value Converter, vừa tối ưu hiệu năng vừa dễ mở rộng."*

2. *"Tại sao hệ thống AI Travel lại cần phân hệ Offer riêng biệt với Product/Tour thông thường?"*  
   ➔ **Trả lời**: *"Một sản phẩm tour thông thường chỉ có ngày đi và giá vé. Nhưng để AI Agent có thể chốt sale tự động và có quyền đàm phán (Discounting/Bonus Gifting), Agent cần các 'vũ khí bán hàng' — đó chính là Grand Slam Offer. Khi khách hàng chần chừ về giá, AI có thể gọi tool `add_bonus_item` hoặc `apply_offer` để gia tăng giá trị quà tặng thay vì giảm giá làm giảm giá trị thương hiệu."*

# 🧭 Hành Trình Chuyển Đổi: Kỳ 5 — Tích Hợp PostgreSQL Docker, Slice Mẫu & Controller/Swagger

> **Mục tiêu tài liệu**: Ghi lại quá trình kết nối cơ sở dữ liệu thực tế PostgreSQL trong Docker, triển khai Slice mẫu `SearchTours` theo kiến trúc Vertical Slice, quyết định chuyển từ Minimal API sang Controller đầy đủ, tích hợp Swagger UI và kiểm chứng kết nối dữ liệu thực tế.

---

## 1. 🗄️ Quyết Định Cơ Sở Dữ Liệu: Clone PostgreSQL Docker Thực Tế

### Yêu cầu thực tế của tôi:
- *"Tôi muốn dự án sẽ dùng PostgreSQL, không cần SQL Server. Hiện tại đã có sẵn DB trong Docker rồi. Hãy clone DB dùng cho dự án C#"*.
- Thay vì dựng cơ sở dữ liệu mẫu sơ sài, việc tận dụng container PostgreSQL (`agent_chat_postgres` port 5432) với dữ liệu tour thực tế của dự án du lịch giúp kiểm chứng tính đúng đắn của toàn bộ chuỗi nghiệp vụ.

### Thực thi:
- Nhân bản database `agent_chat_support` thành database mới độc lập [tour_agent_db]:
  ```sql
  CREATE DATABASE tour_agent_db WITH TEMPLATE agent_chat_support;
  ```
- Toàn bộ **28 bảng quan hệ** cùng dữ liệu các tour (như tour *Nhật Bản 6N5Đ 35.990.000đ*, tour *Thượng Hải 20.000.000đ*...) đã sẵn sàng cho project .NET 10.
- Tích hợp driver chính thức: **`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3**.

---

## 2. 🧩 Kiến Trúc Vertical Slice: Slice Mẫu `SearchTours`

Trong tầng `TourAgent.Application`, thay vì chia thư mục theo kiểu ngang (folder chứa tất cả Command, folder chứa tất cả Query), tôi tổ chức theo **Vertical Slice**:

```
TourAgent.Application/Features/Tours/SearchTours/
├── SearchToursQuery.cs       # Record tham số đầu vào
├── SearchToursValidator.cs   # Kiểm tra tính hợp lệ của tham số phân trang
├── SearchToursResponse.cs    # DTO trả về + Manual Mapping FromEntity
└── SearchToursHandler.cs     # Logic xử lý chính, truy vấn IAppDbContext
```

### Điểm nhấn thiết kế:
- **Khuôn mẫu chuẩn (Template Slice)**: Slice này đóng vai trò là "khuôn mẫu chuẩn mực" để AI Assistant hoặc các developer khác bắt chước cho tất cả các tính năng tiếp theo của dự án.
- **Không dùng AutoMapper**: Dùng hàm `TourSummaryDto.FromEntity(tour)` tĩnh, chuyển đổi dữ liệu nhanh chóng và an toàn.
- **Không dùng Generic Repository**: Dùng trực tiếp `_dbContext.Tours.Include(t => t.Departures).AsNoTracking()` giúp tận dụng tối đa sức mạnh của LINQ và EF Core compile query.

---

## 3. 🔄 Bước Ngoặt: Chuyển Đổi Từ Minimal API Sang Controller Đầy Đủ

### Phản biện của tôi:
Khi AI assistant mặc định cấu hình Minimal API trong `Program.cs`, tôi đã đưa ra yêu cầu điều chỉnh rõ ràng:
- *"Tôi không muốn dùng Minimal API. Hãy điều chỉnh lại thành Controller đầy đủ."*
- *"Hiện tại có vẻ chưa có Swagger đúng không?"*

### Lý do lựa chọn Controller truyền thống:
1. **Quen thuộc & Chuẩn mực Enterprise/SME**: Trong các dự án thực tế và các buổi phỏng vấn tuyển dụng, Controller-based API (`[ApiController]`, `[Route]`, `ControllerBase`) vẫn là cấu trúc phổ biến nhất, dễ tổ chức code khi số lượng endpoint tăng lên hàng chục cái.
2. **Swagger UI trực quan**: Tôi đã cài đặt **`Swashbuckle.AspNetCore`** phiên bản mới nhất trên .NET 10. Giao diện Swagger UI được cấu hình tại:
   👉 **`http://localhost:5132/swagger`**
   Cho phép nhà tuyển dụng hoặc team frontend test API trực tiếp chỉ bằng một cú click chuột.

---

## 4. 🚀 Kiểm Chứng Thực Tế: Live Data & SQL Execution

Tôi và AI đã khởi chạy API và thực hiện gọi thử nghiệm trực tiếp với PostgreSQL Docker:

### Câu lệnh SQL do EF Core 10 sinh ra trên PostgreSQL:
```sql
SELECT t1.id, t1.cancellation_policy, t1.child_policy, t1.code, t1.country, 
       t1.departure_city, t1.route, t1.duration_days, t1.duration_nights, 
       t1.price_from, t1.title, t0.departure_date, t0.price_adult, t0.available_slots
FROM travels AS t1
LEFT JOIN travel_departures AS t0 ON t1.id = t0.travel_id
WHERE t1.status = 'published' AND lower(t1.route) LIKE '%tokyo%'
ORDER BY t1.price_from;
```

### Kết quả JSON thực tế trả về:
```json
{
  "items": [
    {
      "id": 1,
      "code": "TOUR-JP-6N5D-OSAKA-TYO",
      "name": "Cung Đường Vàng Nhật Bản 6N5Đ: Kansai – Osaka – Kyoto – Gifu – Shirakawago – Núi Phú Sĩ – Tokyo – Haneda",
      "destination": "TP.HCM – Osaka – Kyoto – Gifu – Takayama – Shirakawago – Toyama/Azumino – Yamanashi – Tokyo – Haneda – TP.HCM",
      "country": "Nhật Bản",
      "departureCity": "TP.HCM",
      "durationDays": 6,
      "durationNights": 5,
      "priceFrom": 35990000.00,
      "currency": "VND",
      "nextDepartureDate": "2026-10-15",
      "departuresCount": 4
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

---

## 5. 🧪 Tổng Kết Trạng Thái Kiểm Thử Sau Giai Đoạn 3

- **24 tests**: `TourAgent.Domain.UnitTests` (Entities, Value Objects, Result pattern)
- **4 tests**: `TourAgent.Application.UnitTests` (SearchToursHandler: phân trang, lọc theo điểm đến, lọc giá trần, bắt lỗi)
- **3 tests**: `TourAgent.ArchitectureTests` (NetArchTest bảo vệ ranh giới kiến trúc)
- 👉 **TỔNG CỘNG: 31/31 TESTS XANH 100%**, không có bất kỳ warning nào.

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Giữa Minimal API và Controller-based API trong .NET, tại sao dự án của bạn lại chọn Controller và tổ chức theo Vertical Slice?"*

**Câu trả lời chuẩn mực**:
> *"Minimal API rất tuyệt vời cho các microservice siêu nhỏ hoặc service có ít endpoint. Tuy nhiên, với một hệ thống quản lý nghiệp vụ du lịch và AI Agent có hàng chục endpoint (CRUD tour, quản lý booking, chat agent, webhook đa kênh), Controller-based API cung cấp cấu trúc gom nhóm rõ ràng, dễ phân quyền qua middleware/filter và tích hợp Swagger UI cực kỳ tự nhiên. Kết hợp với Vertical Slice trong Application layer, mỗi tính năng (như `SearchTours`) là một lát cắt độc lập chứa đủ Query, Handler, Validator và DTO. Cách tiếp cận này giúp codebase dễ mở rộng mà không làm phình to Controller hay gây phụ thuộc chéo."*

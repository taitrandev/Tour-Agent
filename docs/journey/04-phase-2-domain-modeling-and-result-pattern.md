# 🧭 Hành Trình Chuyển Đổi: Kỳ 4 — Thiết Kế Rich Domain Model, Result Pattern & Quản Trị Git

> **Mục tiêu tài liệu**: Ghi lại bài học thực tế về quản trị Git khi xây dựng dự án con, thiết kế Rich Domain Model với quy tắc bất biến (Invariants), áp dụng Result Pattern để thay thế Exception, và quyết định căn chỉnh kiểu khóa chính `long` (bigint) tương thích với PostgreSQL.

---

## 1. 💡 Tình Huống Thực Tế & Quản Lý Git: .gitignore & Cherry-pick

### Sự cố ban đầu:
- Khi khởi tạo và build lần đầu, thư mục `bin/` và `obj/` đã vô tình bị commit vào Git do file `.gitignore` gốc chưa khai báo các định dạng artifact của .NET.

### Nhận thức & Cách giải quyết chuyên nghiệp:
1. **Tạo `.gitignore` cục bộ cho `dotnet-backend-core-main`**:
   - Thay vì chỉ sửa file `.gitignore` ngoài thư mục gốc, tôi yêu cầu tạo hẳn một file [.gitignore](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/.gitignore) độc lập bên trong `dotnet-backend-core-main/`.
   - **Lý do**: Khi dự án hoàn thành, tôi sẽ dùng lệnh **`git cherry-pick`** các commit liên quan sang một repository Git độc lập hoàn toàn mà không làm mất cấu hình Git của dự án C#.
2. **Gỡ bỏ triệt để file đã track khỏi Git Index**:
   ```powershell
   Get-ChildItem -Path "dotnet-backend-core-main" -Recurse -Include bin,obj -Directory | ForEach-Object { git rm -r --cached $_.FullName }
   ```
3. **Quy tắc kiểm soát commit**: Tôi chỉ thị AI: *"Lần sau không tự commit nữa, để tôi tự commit"*. Điều này đảm bảo Developer luôn là người kiểm soát tối cao (human-in-the-loop) đối với lịch sử commit và Git diff.

---

## 2. 💎 Thiết Kế Rich Domain Model Thay Vì Anemic Model

Thay vì tạo các model "vô hồn" (Anemic Data Model với public getter/setter bừa bãi), tầng `TourAgent.Domain` được xây dựng theo chuẩn **Rich Domain Model**:

### Nguyên tắc thiết kế:
- **Private Setters**: Không cho phép bên ngoài tự ý thay đổi trạng thái entity.
- **Factory Method `Create()`**: Mọi đối tượng bắt buộc phải đi qua hàm factory để kiểm tra toàn vẹn nghiệp vụ (Business Invariants).
- **Behavior Methods**: Các phương thức nghiệp vụ tự quản lý trạng thái của chính nó (ví dụ: `tour.AddDeparture(...)`, `departure.ReserveSlots(...)`, `conversation.EscalateToHuman(...)`).

### Ví dụ thực tế trong `TourDeparture.cs`:
```csharp
public static Result<TourDeparture> Create(
    long tourId,
    DateOnly departureDate,
    decimal priceAdult,
    decimal priceChild,
    decimal singleSupplement,
    int totalSlots,
    int? availableSlots = null,
    TourDepartureStatus status = TourDepartureStatus.Open,
    long id = 0)
{
    if (priceAdult <= 0)
    {
        return Result.Failure<TourDeparture>(TourErrors.InvalidAdultPrice);
    }

    var actualAvailableSlots = availableSlots ?? totalSlots;

    if (actualAvailableSlots < 0 || actualAvailableSlots > totalSlots || totalSlots <= 0)
    {
        return Result.Failure<TourDeparture>(TourErrors.InvalidSlots);
    }

    var determinedStatus = actualAvailableSlots == 0 ? TourDepartureStatus.Full : status;

    return Result.Success(new TourDeparture(
        id, tourId, departureDate, priceAdult, priceChild, singleSupplement,
        totalSlots, actualAvailableSlots, determinedStatus));
}
```

---

## 3. 🎯 Result Pattern: Loại Bỏ Exception-Driven Architecture

### Vấn đề:
Lạm dụng `throw new Exception("Giá không hợp lệ")` cho các lỗi nghiệp vụ thông thường là một anti-pattern:
- Rất tốn chi phí hiệu năng (chi phí tạo stack trace của CLR).
- Làm đứt gãy luồng xử lý tự nhiên của code.
- Khó kiểm soát lỗi ở các tầng trên.

### Giải pháp: Cặp đôi `Result<T>` và `Error`:
- [Error.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Common/Error.cs): Record định danh lỗi dạng code/description rõ ràng (`Tour.InvalidPrice`, `Tour.NotFound`...).
- [Result.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Domain/Common/Result.cs): Chứa cờ `IsSuccess`, `IsFailure`, `Value`, `Error`.

```csharp
var result = Tour.Create(code, name, destination, country, city, days, nights, price);
if (result.IsFailure)
{
    return BadRequest(result.Error); // Trả về HTTP 400 cùng mã lỗi rõ ràng
}
```

---

## 4. 🔄 Nhận Thức Chuyển Đổi Khóa Chính: `Guid` sang `long` (bigint)

- Ban đầu theo template chung, `Entity` sử dụng khóa chính dạng `Guid`.
- Tuy nhiên, khi kết nối với cơ sở dữ liệu PostgreSQL thực tế đã tồn tại (cloned từ `agent_chat_support`), các bảng `travels`, `travel_departures`, `conversations` đều sử dụng khóa chính kiểu `bigint` auto-increment.
- **Quyết định refactor kịp thời**:
  - Tinh chỉnh `Entity` kế thừa `Entity<long>`.
  - Cập nhật `TourId`, `ConversationId`, `MessageId` sang kiểu `long`.
  - Giúp việc ánh xạ vào cơ sở dữ liệu thực tế diễn ra trơn tru 100%, không cần phải drop/tạo lại schema bảng.

---

## 5. 🧪 Kết Quả Kiểm Thử Tầng Domain

Toàn bộ các quy tắc bất biến được bao phủ bằng **24 Unit Tests** tại `tests/TourAgent.Domain.UnitTests`:
- `TourTests.cs`: Kiểm tra giá sàn > 0, số ngày >= 1, mã tour không rỗng, chống trùng lặp ngày khởi hành.
- `TourDepartureTests.cs`: Kiểm tra logic giữ chỗ (`ReserveSlots`) khi đủ chỗ hoặc khi hết chỗ tự động chuyển sang `Full`.
- `ConversationTests.cs` & `ResultTests.cs`.
- **Kết quả: 24/24 tests PASSED**.

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Bạn thiết kế Domain Model như thế nào và tại sao lại dùng Result Pattern thay vì Exception?"*

**Câu trả lời chuẩn mực**:
> *"Tôi áp dụng triết lý Rich Domain Model của DDD: mọi entity đều có private setters và chỉ được khởi tạo qua static factory method `Create()` để đảm bảo tính bất biến (Invariants) không bao giờ bị vi phạm ở trạng thái không hợp lệ. Đối với xử lý lỗi, tôi áp dụng Result Pattern (`Result<T>` và `Error`). Trong .NET, việc throw exception rất tốn tài nguyên do CLR phải unwind stack frame và chụp stack trace. Exception chỉ nên dùng cho các sự cố hệ thống ngoài tầm kiểm soát (mất kết nối mạng, crash DB), còn với luồng nghiệp vụ thông thường, Result Pattern giúp code tường minh, type-safe và đạt hiệu năng tối đa."*

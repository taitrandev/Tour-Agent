# ADR-0005: Chuẩn Hóa 100% camelCase Cho API Contracts & Cấm Tuyệt Đối snake_case

- **Trạng thái**: Accepted (Đã chấp thuận & Áp dụng)
- **Ngày quyết định**: 2026-11-24
- **Người tham gia**: User (Product Owner) & Antigravity Agent
- **Phạm vi ảnh hưởng**: `TourAgent.Api`, DTOs, Minimal APIs/Controllers, `tour-agent-frontend` (React TypeScript)

---

> [!IMPORTANT]
> ### 🛑 NGUYÊN TẮC BẤT DI BẤT DỊCH CHO AI AGENT (Golden Rules)
> - **✅ ĐƯỢC PHÉP**: 
>   - Khai báo DTOs bằng C# record/class theo quy ước PascalCase thông thường (ví dụ: `IsFeatured`, `TravelId`, `OfferPrice`, `OrderCode`).
>   - Để ASP.NET Core với `JsonNamingPolicy.CamelCase` tự động serialize thành camelCase khi gửi qua HTTP API (`isFeatured`, `travelId`, `offerPrice`, `orderCode`).
>   - Toàn bộ query parameters trong Action Controller bắt buộc dùng camelCase (`[FromQuery] bool? isFeatured, [FromQuery] long? travelId`).
>   - Phía Frontend (React/TypeScript): Mọi interface, state và payload gửi đi (`apiFetch`) đều dùng 100% camelCase.
> - **❌ TUYỆT ĐỐI CẤM**:
>   - CẤM đặt `[JsonPropertyName("ten_snake_case")]` trong C# DTOs.
>   - CẤM đặt tên tham số API query string bằng `snake_case` (ví dụ: `travel_id`, `is_featured`, `order_code`).
>   - CẤM sử dụng `snake_case` trong React state, props hay request body.

---

## 1. Bối Cảnh & Vấn Đề (Context & Problem Statement)

Dự án TourAgent được nâng cấp và chuyển đổi (migration) từ mã nguồn PHP legacy sang .NET 10 Clean Architecture kết hợp React 19 SPA:
1. **Di chứng từ PHP Legacy**: Trong cơ sở dữ liệu PostgreSQL và code PHP cũ, các trường được đặt theo quy ước `snake_case` (như `travel_id`, `is_featured`, `departure_date`).
2. **Sự cố không đồng nhất**: Khi viết code mới, một số controller (như `OffersController.cs`) do quán tính nhìn theo code cũ đã tiếp tục đặt tham số `[FromQuery] long? travel_id`, `[FromQuery] bool? is_featured`. Điều này gây ra sự không nhất quán: nơi dùng `camelCase`, nơi dùng `snake_case`, khiến Frontend dễ gặp lỗi `undefined` khi đọc response hoặc gửi sai query parameter.
3. Người dùng đã quy định rõ ràng trong `AGENTS.md` nhưng Agent vẫn bị tái phạm do thiếu cơ chế chốt hạ bằng ADR.

---

## 2. Các Lựa Chọn Đã Xem Xét (Options Considered)

- **Phương án A: Thống nhất 100% camelCase cho toàn bộ HTTP API (Được chọn)**:
  - C# sử dụng chuẩn PascalCase tự nhiên cho class/record.
  - ASP.NET Core serialize tự động sang camelCase qua `JsonNamingPolicy.CamelCase`.
  - Frontend TypeScript tiêu thụ chuẩn camelCase tự nhiên của JavaScript.
- **Phương án B: Giữ snake_case cho API để giống PHP cũ (Bị bác bỏ)**:
  - Gây xung đột với hệ sinh thái React / Vite / TypeScript hiện đại.
  - Buộc C# DTOs phải gắn hàng trăm thuộc tính `[JsonPropertyName("...")]` rất rườm rà và dễ sai sót.

---

## 3. Quyết Định Kiến Trúc (Decision Outcome)

Toàn bộ hệ thống thống nhất chuẩn giao tiếp **camelCase 100%**:

### Mã Nguồn Mẫu Chuẩn (Standard Code Example):

**Backend .NET 10 (DTO & Controller)**:
```csharp
// DTOs viết PascalCase tự nhiên — KHÔNG dùng [JsonPropertyName("...")]
public sealed record TourFilterDto(
    string? SearchText,
    bool? IsFeatured,
    long? TravelId);

// Controller Action nhận query parameters chuẩn camelCase
[HttpGet]
public async Task<IActionResult> GetAllOffers(
    [FromQuery] string? search,
    [FromQuery] string? status,
    [FromQuery] bool? isFeatured,
    [FromQuery] long? travelId,
    CancellationToken cancellationToken)
{ ... }
```

**Frontend React 19 (TypeScript Interface)**:
```typescript
export interface OfferData {
  id: number;
  title: string;
  isFeatured: boolean;
  travelId?: number;
  offerPrice: number;
}
```

---

## 4. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- Mã nguồn C# và TypeScript đều tuân thủ 100% coding convention của ngôn ngữ mình (Idiomatic Code).
- Không cần cấu hình tùy biến thủ công cho từng field.
- Loại bỏ hoàn toàn lỗi giao tiếp do lệch chuẩn `camelCase` vs `snake_case`.

### Đánh đổi (Cons)
- Khi đọc dữ liệu trực tiếp từ các cột SQL của PostgreSQL (dạng `snake_case`), EF Core Configurations cần ánh xạ tường minh bằng Fluent API `.HasColumnName("travel_id")` trong tầng Infrastructure (việc này đã hoàn thành).

---

## 5. Kiểm Thử Bảo Vệ (Verification & Test Guard)

- Chạy kiểm thử Backend: `dotnet test dotnet-backend-core-main` (126 tests).
- Chạy kiểm thử Frontend: `cmd /c "npm --prefix tour-agent-frontend run build"`.
- Quét kiểm tra vi phạm: `git grep "\[FromQuery\].*_"` (phải trả về 0 kết quả).


# ADR-0007: Chuẩn Hóa [FromQuery] Sang Query Objects & Hợp Nhất API Tour Vào TravelsController

- **Trạng thái**: Accepted (Đã chấp thuận & Áp dụng)
- **Ngày quyết định**: 2026-11-24
- **Người tham gia**: User (Product Owner) & Antigravity Agent
- **Phạm vi ảnh hưởng**: `TourAgent.Api/Controllers`, `TourAgent.Application/Features/Tours`

---

> [!IMPORTANT]
> ### 🛑 NGUYÊN TẮC BẤT DI BẤT DỊCH CHO AI AGENT (Golden Rules)
> - **✅ ĐƯỢC PHÉP**: 
>   - Toàn bộ action HTTP GET có nhiều hơn 1 tham số lọc/phân trang phải dùng **Query Object Binding**: `[FromQuery] TQuery query` (ví dụ: `[FromQuery] GetContactsQuery query`).
>   - Toàn bộ nghiệp vụ sản phẩm Tour (CRUD, tra cứu, tìm kiếm, reviews, faqs) phải tập trung tại **`TravelsController.cs`**.
>   - Endpoint tìm kiếm tour nâng cao là `GET /api/v1/travels/search`.
> - **❌ TUYỆT ĐỐI CẤM**:
>   - CẤM viết danh sách tham số rời rạc trong Controller method (như `[FromQuery] string a, [FromQuery] string b, [FromQuery] int c`).
>   - CẤM tạo thêm controller riêng rẽ như `ToursController.cs` gây phân mảnh và trùng lặp với `TravelsController.cs`.

---

## 1. Bối Cảnh & Vấn Đề (Context & Problem Statement)

Trong quá trình rà soát mã nguồn tầng `TourAgent.Api`:
1. **Code Smell "Long Parameter List"**: Nhiều Action GET khai báo 4-5 tham số rời rạc, rồi bên trong method lại phải tự gọi constructor `new Query(param1, param2, ...)`. Cách viết này vi phạm nguyên tắc Clean Code và làm phình to Controller method signature.
2. **Controller dư thừa & Phân mảnh**: Tồn tại đồng thời `ToursController.cs` (chỉ có duy nhất 1 action `SearchTours`) và `TravelsController.cs` (chứa toàn bộ CRUD tour, ngày bay, lịch trình, reviews, faqs). Cả hai cùng xử lý chung 1 đối tượng nghiệp vụ là Tour du lịch nhưng lại bị xé làm 2 controller riêng biệt do vết tích lịch sử lúc dựng slice mẫu ban đầu.

---

## 2. Các Lựa Chọn Đã Xem Xét (Options Considered)

- **Phương án A: Chuẩn hóa Model Binding bằng Query Object & Hợp nhất về TravelsController (Được chọn)**:
  - Khai báo Query Object nhận diện trực tiếp bằng Model Binder của ASP.NET Core.
  - Gộp `SearchTours` vào `TravelsController.cs` với route `GET /api/v1/travels/search` và xóa bỏ `ToursController.cs`.
- **Phương án B: Giữ nguyên tham số rời rạc và giữ 2 controllers (Bị bác bỏ)**:
  - Gây khó bảo trì, vi phạm nguyên tắc Single Responsibility của Clean Architecture.

---

## 3. Quyết Định Kiến Trúc (Decision Outcome)

### Mã Nguồn Mẫu Chuẩn (Standard Code Example):

**Chuẩn Hóa Query Object trong Controller**:
```csharp
// Thay vì 5 tham số rời rạc, Action chỉ cần nhận 1 Query Object
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> GetContactsAsync(
    [FromQuery] GetContactsQuery query,
    CancellationToken cancellationToken = default)
{
    var result = await _getContactsHandler.HandleAsync(query, cancellationToken);
    if (result.IsFailure)
    {
        return BadRequest(new { success = false, message = result.Error.Description });
    }
    return Ok(result.Value);
}
```

**Thống Nhất Toàn Bộ Nghiệp Vụ Tour tại `TravelsController.cs`**:
```csharp
[ApiController]
[Produces("application/json")]
public class TravelsController : ControllerBase
{
    // 1. Quản lý danh mục & Catalog
    [HttpGet("api/v1/travels")]
    public async Task<IActionResult> GetAllTravels([FromQuery] GetAllToursQuery query, CancellationToken ct) { ... }

    // 2. Tìm kiếm nâng cao (kế thừa từ ToursController cũ)
    [HttpGet("api/v1/travels/search")]
    public async Task<IActionResult> SearchTours([FromQuery] SearchToursQuery query, CancellationToken ct) { ... }

    // 3. Chi tiết theo Id / Slug
    [HttpGet("api/v1/travels/{id:long}")]
    public async Task<IActionResult> GetTravelById(long id, CancellationToken ct) { ... }

    [HttpGet("api/v1/travels/slug/{slug}")]
    public async Task<IActionResult> GetTravelBySlug(string slug, CancellationToken ct) { ... }

    // 4. CRUD Tour, Reviews & FAQs...
}
```

---

## 4. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
- Signature các Action trong Controller cực kỳ ngắn gọn, thanh lịch.
- Tận dụng 100% sức mạnh Model Binding của ASP.NET Core và Swagger tự động unpack properties.
- Hệ thống chỉ còn 1 Controller duy nhất quản lý toàn bộ tour, không còn file rác dư thừa.

### Đánh đổi (Cons)
- Endpoint tìm kiếm tour cũ `GET /api/v1/tours` được chuyển sang `GET /api/v1/travels/search` (phía Frontend hiện tại không dùng endpoint này nên zero-impact).

---

## 5. Kiểm Thử Bảo Vệ (Verification & Test Guard)

- `TourAgent.ArchitectureTests`: `Controllers_ShouldNot_DependOn_Infrastructure` đảm bảo tất cả controllers đều tuân thủ ranh giới layer.
- `dotnet test dotnet-backend-core-main` (126 tests passed).


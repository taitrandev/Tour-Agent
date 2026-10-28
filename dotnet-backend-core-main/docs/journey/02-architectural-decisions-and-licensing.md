# 🧭 Hành Trình Chuyển Đổi: Kỳ 2 — Nhận Thức Bản Quyền & Quyết Định Kiến Trúc

> **Mục tiêu tài liệu**: Ghi lại quá trình phát hiện rủi ro pháp lý của các thư viện mã nguồn mở phổ biến trong .NET (MediatR, AutoMapper...), quyết định lựa chọn Clean Architecture tinh giản cho phân khúc SME, và tư duy tách rời 100% giữa Backend và Frontend.

---

## 1. ⚠️ Nhận Thức Sâu Về Bản Quyền: Vấn Đề MediatR & AutoMapper

Trong quá trình lên kế hoạch, tôi đã chủ động đặt vấn đề với AI Assistant về **rủi ro bản quyền (Licensing risks)**:

### Thực trạng cộng đồng .NET:
- Tác giả Jimmy Bogard đã thông báo chuyển đổi mô hình kinh doanh của **AutoMapper (từ v14)** và **MediatR (từ v13)** sang giấy phép thương mại / sponsorware / PolyForm cho môi trường doanh nghiệp.
- Nếu một dự án mới vẫn nhắm mắt cài `MediatR` và `AutoMapper` theo thói quen cũ từ vài năm trước, dự án sẽ đối mặt với:
  1. Rủi ro pháp lý khi chuyển giao code cho doanh nghiệp.
  2. Dính vào thói quen "MediatR anti-pattern": Lạm dụng reflection ngầm, làm mờ luồng thực thi (indirection), gây khó khăn khi debug (không thể F12 thẳng vào logic xử lý) và làm chậm thời gian khởi động.
  3. Lạm dụng AutoMapper: Chạy reflection runtime chậm chạp, đổi tên thuộc tính dễ sinh lỗi runtime khó phát hiện.

---

## 2. 🛠️ Giải Pháp Thay Thế Chuẩn Mực Hiện Đại

### A. Thay thế MediatR: Explicit Command/Query Handlers
Tôi quyết định **không sử dụng MediatR**. Thay vào đó, tự định nghĩa các interface trừu tượng tối giản trong `Application/Common/IHandlers.cs`:

```csharp
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

**Tại sao đây là Best Practice?**
- **0% rủi ro bản quyền** (0 dependency ngoài).
- **Code tường minh tuyệt đối**: Ở Controller/Endpoint, chỉ cần inject trực tiếp `IQueryHandler<SearchToursQuery, Result<SearchToursResult>>`. Nhấn `F12` lập tức chuyển đến `SearchToursHandler`.
- **Hiệu năng cao nhất**: Trực tiếp gọi hàm, không qua trung gian pipeline reflection của MediatR.

### B. Thay thế AutoMapper: Manual Mapping (Extension Methods)
Thay vì dùng AutoMapper, tôi áp dụng **Manual Mapping** qua static factory method `TourSummaryDto.FromEntity(tour)`:

```csharp
public static TourSummaryDto FromEntity(Tour tour)
{
    var openDepartures = tour.Departures
        .Where(d => d.Status == TourDepartureStatus.Open)
        .OrderBy(d => d.DepartureDate)
        .ToList();

    return new TourSummaryDto(
        tour.Id,
        tour.Code,
        tour.Name,
        tour.Destination,
        tour.Country,
        tour.DepartureCity,
        tour.DurationDays,
        tour.DurationNights,
        tour.PriceFrom,
        tour.Currency,
        tour.Highlights,
        openDepartures.FirstOrDefault()?.DepartureDate,
        openDepartures.Count);
}
```

**Tại sao đây là Best Practice?**
- Nhanh gấp 20 - 50 lần AutoMapper runtime.
- Kiểm soát kiểu dữ liệu ở compile-time (đổi tên thuộc tính là compiler báo lỗi ngay).
- Dễ đọc, dễ unit test, không có ma thuật ngầm.

### C. Cảnh giác với các "Bẫy Bản Quyền" khác trong .NET:
- **Duende IdentityServer**: Tránh xa (đã chuyển sang thương mại đắt đỏ); thay bằng **ASP.NET Core Identity** native.
- **QuestPDF**: Tránh dùng nếu doanh thu doanh nghiệp > $1M; thay bằng các thư viện mở hoặc template HTML-to-PDF.
- **Z.EntityFramework.Extensions**: Tránh dùng; EF Core 7/8/9/10 đã có sẵn native `ExecuteUpdateAsync()` và `ExecuteDeleteAsync()`.

---

## 3. 📐 Định Vị Kiến Trúc: Clean Architecture Tinh Giản Cho SME

Tôi thống nhất nguyên tắc: **Không over-engineering**. Dự án nhắm đến quy mô doanh nghiệp vừa và nhỏ (SME) và bài toán thực tế:
- ❌ **Không dùng**: CQRS tách biệt 2 database Read/Write, Event Sourcing, Microservices cồng kềnh.
- ✅ **Sử dụng**: **Clean Architecture 4 tầng** kinh điển kết hợp **Vertical Feature Folders**:
  - `TourAgent.Domain`: Thực thể nghiệp vụ, Value Objects, Invariants, `Result<T>`, `Error`.
  - `TourAgent.Application`: Use cases gom theo thư mục tính năng (`Features/{Area}/{UseCase}/`), DTOs, Handlers, Abstractions.
  - `TourAgent.Infrastructure`: EF Core DbContext kết nối PostgreSQL, AI Client, Background Channels.
  - `TourAgent.Api`: Controllers, Swagger UI, Middleware.

---

## 4. 🌐 Quyết Định Tách Rời Hoàn Toàn Backend và Frontend

- Thay vì dùng Razor Pages hoặc Blazor Monolith chung project, tôi quyết định tách riêng:
  - **Backend**: Đóng vai trò là Headless Web API thuần túy, cung cấp RESTful endpoints chuẩn mực và SSE (Server-Sent Events) để stream dữ liệu AI.
  - **Frontend**: Ứng dụng SPA riêng biệt (React / Vite) gọi qua REST API.
- Lợi ích: Kiến trúc chuẩn decoupling, dễ dàng phát triển ứng dụng di động hoặc tích hợp đa kênh (Web, Telegram Bot, Zalo OA) trong tương lai.

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Tại sao trong Solution của bạn không thấy sử dụng MediatR và AutoMapper - hai thư viện gần như mặc định trong các template Clean Architecture trên mạng?"*

**Câu trả lời thể hiện đẳng cấp**:
> *"Thứ nhất, xét về mặt quản trị rủi ro, cả MediatR và AutoMapper đều đã chuyển hướng sang mô hình thương mại/sponsorware cho các phiên bản mới, có thể gây rủi ro về mặt bản quyền cho doanh nghiệp. Thứ hai, về mặt kiến trúc, cộng đồng .NET hiện đại đang dần loại bỏ 'MediatR anti-pattern' vì nó tạo ra sự gián tiếp không cần thiết (unnecessary indirection) và lạm dụng reflection. Bằng cách định nghĩa trực tiếp `ICommandHandler` và `IQueryHandler`, code của tôi đạt được tính type-safe 100%, dễ debug (F12 nhảy thẳng vào handler), tốc độ biên dịch và thực thi cao hơn mà không cần phụ thuộc vào thư viện bên ngoài. Với mapping, manual mapping giúp tối ưu hiệu năng gấp hàng chục lần so với AutoMapper và tránh được các lỗi runtime ngầm."*

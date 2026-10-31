# 📐 NHÓM 1: Clean Architecture & Design Principles

> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

## Em dùng AI như thế nào trong SDLC?

e ko dùng AI để code nhanh hơn, mà dùng AI xuyên suốt SDLC: từ giai đoạn system review, architecture 

## Tại sao e không dùng MediatR?

Nguyên nhân chính e không dùng là vì trước đó nghe nói bắt đầu apply license. Nhưng thông qua việc không apply thì e cũng có thêm góc nhìn và kiến thức, bỏ được ngộ nhận là khi dùng CQRS cần phải dùng MediatR. trong giai đoạn system design thì e áp dụng tư duy AI-First để debate về những kỹ thuật để biết ưu điểm, nhược điểm, trade-off giữa việc dùng MediatR và tự viết handler

Những ưu điểm khi tự viết handler là:

* dễ debug & trace code: dùng tính năng go to implementation để từ controller vào handler
* controller thấy được rõ ràng các handler, không bị che giấu, tránh biến controller thành 1 túi mù
* dự án của e nhỏ, nếu dùng MediatR thì dễ bị over engineering, gây phức tạp hóa

## Em biết gì về MediatR?

Bản chất MediatR là 1 thư viện giúp giảm sự phụ thuộc giữa caller và handler. Controller ko cần biết handler cụ thể, nó chỉ gửi request còn MediatR sẽ dùng reflection tìm và điều phối đến handler tương ứng lúc runtime. 

ưu điểm là: 

* xử lý cross cutting mạnh và tiện. Nên với hệ thống lớn có rất nhiều usecase và nhiều cross cutting phức tạp. Lúc đó chi phí dùng thư viện sẽ rẻ hơn chi chí tự viết và bảo trì pipeline riêng

nhược điểm là

* khó trace code: khó theo luồng code khi đọc, vì ko đi thẳng từ controller tới handler đc
* rủi ro dời về runtime: quên đăng ký handler thì lúc chạy app và request gọi đến thì mới văng exception, compile time ko phát hiện được
* dễ bị dev lạm dụng pub/sub để làm message broker trong bộ nhớ, gây mất dữ liệu khi server có vấn đề
* rủi ro về license

## Tại sao dùng Result thay vì Exception rồi bắt tại middleware?

Vì e dùng Result cho những lỗi liên quan business dự đoán được như là not found, validation fail. Còn Exception chỉ nên dùng cho các tình huống bất thường, không lường trước được như là timeout, mất kết nối đến db thì sẽ bắt ở middleware để trả 500

còn lý do phụ là hiệu suất. có 1 benchmark trên .NET cho thấy khi có lỗi thì Result chạy nhanh hơn Exception 40 lần, còn cấp phát bộ nhớ thì ít hơn khoảng 26 lần. Còn khi không có lỗi thì hai cách gần như ngang nhau. Nên chênh lệch chỉ đáng kể khi lỗi xảy ra thường xuyên, ví dụ validation fail nhiều hoặc bị tấn công bằng cách spam request sai

* trade-off khi dùng Result là
  * Code dài hơn: vì Result phải truyền ngược qua từng tầng như là Domain lên Handler
  * Tầng controller phải check và map status code: phải tạo extension method để map các error, status code theo chuẩn ProblemsDetails
  * Dù dùng Result nhưng vẫn cần phải có middleware để bắt exception và trả 500. Tức là hệ thống phải dùng cả 2
* Thông tin thêm
  * đối với authentication và authorization (401) thì sẽ bắt ở middleware trước khi đến controller, handler. result chỉ nên chứa lỗi bussiness như Validation (400), Not Found (404)


## Các sai lầm thường gặp khi throw exception trong .NET

Top 3 lỗi gây hại nhiều nhất là:

* throw ex thay vì throw: throw ex sẽ làm mất stack trace, dấu vết chỉ bắt đầu từ dòng throw ex, những dấu vết trước bị reset
* wrap exception nhưng không truyền innerException: cần luôn truyền innerException
* Nuốt exception: catch rỗng ko làm gì, hoặc catch xong return null

3 lỗi nhỏ hơn

* throw exception chung chung: nên throw cụ thể, có ngữ nghĩa như là ArgumentException, hoặc custom exception
* message thiếu ngữ cảnh hoặc làm lộ dữ liệu nhạy cảm
* log rồi rethrow nhiều tầng: việc này dẫn đến lỗi xuất hiện nhiều lần trong log, trong lúc đọc log dễ bị nhiễu. chỉ log ở những nơi thực sự xử lý lỗi. các tầng giữa nếu cần context thì thêm bằng cách wrap exception, không log lại


## CancellationToken là gì? Tại sao cần dùng (WHY)?

Là cơ chế giúp hủy tác vụ đang chạy trong .NET, giống như một "Lá cờ". Các tác vụ thấy cờ bật lên sẽ chủ động dừng lại và ném exception

- Đối với server thì sẽ tiết kiệm tài nguyên, đối với AI thì sẽ tiết kiệm token
- Hệ thống sẽ dừng khi code chạm các hàm async await và được truyền cancellation token. còn code sync hoặc async await không truyền cancellation token sẽ không tự dừng. dev sẽ phải tự kiểm tra cờ và dừng.
- đối với database driver thì có cơ chế dừng riêng, db driver sẽ lập tức gửi CancelRequest xuống db để dừng lập tức query đang chạy

Nguyên lý cốt lõi ngắt là:

* ví dụ khi user F5 web hoặc bấm Cancel postman thì sẽ gửi gửi 1 gói tin TCP segment có gắn cờ FIN hoặc RST để phát tín hiệu đóng TCP socket. Kestrel nhận được cờ này sẽ biết là socket đã bị ngắt, Kestrel sẽ update CancellationToken thành true
* thêm thông tin chi tiết:

  * đối với ReactJs gõ tìm kiếm, muốn ngắt thì phải dùng AbortController để chủ động ngắt TCP socket
  * đôi với postman nếu send 2 request thì xem như 2 lần độc lập, không ngắt socket. muốn ngắt thì phải bấm Cancel



## 1️⃣ 4-Layer Structure & Dependency Rule

### Sơ đồ phụ thuộc (chỉ đi vào trong)

```
Api  ──►  Application  ──►  Domain
 │                             ▲
 └──►  Infrastructure  ────────┘
```

### Rule bất di bất dịch

| Layer                    | Được phụ thuộc vào    | KHÔNG được phụ thuộc vào           |
| ------------------------ | --------------------------- | ----------------------------------------- |
| **Domain**         | (không ai)                 | Application, Infrastructure, Api, EF Core |
| **Application**    | Domain                      | Infrastructure, Api                       |
| **Infrastructure** | Application, Domain         | Api                                       |
| **Api**            | Application, Infrastructure | (tất cả đều được)                  |

### File tham chiếu trong TourAgent

```
src/
├── TourAgent.Domain/           → Zero external dependencies
├── TourAgent.Application/      → Chỉ ref Domain
├── TourAgent.Infrastructure/   → Ref Application + Domain
│   ├── TourAgent.Infrastructure.csproj
│   │   PackageReference: Npgsql.EF, MS.Extensions.Http
└── TourAgent.Api/              → Ref tất cả
```

### Ẩn dụ để nhớ

> **Não** (Domain) không biết cơ thể có gì → không import gì cả
> **Thần kinh** (Application) điều phối nhưng không tự làm
> **Tay chân** (Infrastructure) làm việc cụ thể theo lệnh thần kinh
> **Miệng** (Api) nhận yêu cầu từ bên ngoài, ra lệnh cho bên trong

### Q&A

**Q: Tại sao Domain không được import EF Core?**

> Vì EF Core là **implementation detail** của cách lưu dữ liệu. Ngày mai đổi sang Dapper hay MongoDB → Domain không được biết/không được thay đổi. Đây là Dependency Inversion Principle.

**Q: NetArchTest bảo vệ điều gì?**

> Tự động kiểm tra ranh giới layer trong CI/CD. Dev vô tình `using Microsoft.EntityFrameworkCore` trong Domain → test fail ngay. Không cần review bằng mắt.

---

## 2️⃣ Domain Layer Patterns

### 2.1 Entity + Private Setter + Factory Method

```csharp
// ✅ ĐÚNG — TourAgent pattern
public sealed class Tour : Entity<Guid>
{
    public string Name { get; private set; }     // private setter
    public decimal BasePrice { get; private set; }

    private Tour() { }  // EF Core cần constructor rỗng

    // Factory method — kiểm soát invariants tại điểm tạo
    public static Result<Tour> Create(string name, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Tour>.Failure(TourErrors.NameRequired);

        if (basePrice <= 0)
            return Result<Tour>.Failure(TourErrors.InvalidPrice);

        return Result<Tour>.Success(new Tour { Name = name, BasePrice = basePrice });
    }
}

// ❌ SAI — public setter = không kiểm soát được invariant
public class Tour
{
    public string Name { get; set; }
    public decimal BasePrice { get; set; }
}
```

**Bản chất**: Entity không được rơi vào trạng thái invalid. Private setter + Factory Method = "hàng rào" bảo vệ tính nhất quán dữ liệu.

### 2.2 Result\<T\> Pattern

```csharp
// Domain/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

// Domain/Common/Error.cs
public record Error(string Code, string Description);

// Cách dùng trong Handler
public async Task<Result<TourResponse>> HandleAsync(...)
{
    var tour = await _db.Tours.FindAsync(id, ct);

    if (tour is null)
        return Result<TourResponse>.Failure(TourErrors.NotFound);  // Không throw exception

    return Result<TourResponse>.Success(tour.ToResponse());
}
```

**Q: Khi nào dùng Result\<T\>, khi nào dùng Exception?**

> - **Result\<T\>**: Lỗi business bình thường, caller biết trước phải handle (tour không tồn tại, validation fail)
> - **Exception**: Lỗi không mong đợi (DB bị down, network fail, lỗi lập trình)
> - Rule: Exception = "emergency exit". Result = "expected branching"

---

## 3️⃣ Application Layer — Feature Folders

### Cấu trúc mỗi Feature

```
Features/Tours/SearchTours/
├── SearchToursQuery.cs        → Định nghĩa input (record)
├── SearchToursHandler.cs      → Business logic (sealed class)
├── SearchToursResponse.cs     → Output DTO (record)
└── SearchToursValidator.cs    → Validation rules
```

**Q: Feature Folder khác Layer Folder như thế nào?**

> - **Layer Folder (horizontal)**: `/Commands/`, `/Queries/`, `/DTOs/` — mở file Commands thì thấy commands của TẤT CẢ feature
> - **Feature Folder (vertical)**: `/SearchTours/` — mở folder thì thấy TẤT CẢ file liên quan đến SearchTours
> - Khi maintain feature SearchTours → chỉ cần mở 1 folder, không phải nhảy qua nhiều folder

### Command / Query Pattern (không MediatR)

```csharp
// Handler inject trực tiếp vào Endpoint
app.MapPost("/api/v1/tours/search", async (
    SearchToursQuery query,
    SearchToursHandler handler,    // Direct injection
    CancellationToken ct) =>
{
    var result = await handler.HandleAsync(query, ct);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(result.Error);
});
```

**Q: Tại sao không dùng MediatR?**

> MediatR thêm 1 lớp indirection không cần thiết. Handler được inject trực tiếp → `Ctrl+Click` đến Handler ngay, không qua magic pipeline. Dễ debug, dễ trace.
> **Trade-off**: Mất "decoupling" ở Api layer, nhưng gain được simplicity + performance + zero magic.

---

## 4️⃣ Quyết Định "Không Dùng" — YAGNI Analysis

### Không AutoMapper

| Tiêu chí  | AutoMapper              | Manual Extension Method     |
| ----------- | ----------------------- | --------------------------- |
| Setup       | Cần cấu hình Profile | Viết method tường minh   |
| Debug       | Lỗi runtime khó trace | Lỗi compile-time rõ ràng |
| Performance | Reflection, chậm hơn  | Direct property assignment  |
| License     | v11+ có thể thu phí  | Free forever                |
| Readability | Config ở nơi khác    | Code đọc là hiểu ngay   |

```csharp
// Manual mapping — TourAgent cách làm
public static class TourMappingExtensions
{
    /// <summary>Chuyển đổi Tour entity sang TourResponse DTO.</summary>
    public static TourResponse ToResponse(this Tour tour) => new(
        Id: tour.Id,
        Name: tour.Name,
        BasePrice: tour.BasePrice,
        Destination: tour.Destination
    );
}
```

### Không Generic Repository

```csharp
// ❌ KHÔNG làm — Boilerplate vô nghĩa vì EF Core đã là Repository
public interface IRepository<T> { ... }
public class TourRepository : IRepository<Tour> { ... }

// ✅ TourAgent làm — EF Core DbSet đã là Repository
public interface IAppDbContext
{
    DbSet<Tour> Tours { get; }
    DbSet<Conversation> Conversations { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

**Bản chất**: EF Core `DbSet<T>` đã implement Repository pattern. Bọc thêm 1 lớp = thêm complexity, không có thêm benefit nào ở quy mô SME.

---

## 5️⃣ Architecture Tests

```csharp
// tests/TourAgent.ArchitectureTests/LayerTests.cs
[Fact]
public void Domain_Should_Not_HaveDependencyOn_Infrastructure()
{
    var result = Types.InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("TourAgent.Infrastructure")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

**Tại sao quan trọng?**

> Quy trình tốt = tự động kiểm soát, không dựa vào kỷ luật cá nhân. NetArchTest = "người gác cửa tự động" cho kiến trúc.

---

## 📝 Checklist Tự Kiểm Tra

- [ ] Vẽ được sơ đồ 4 layer và chiều phụ thuộc không nhìn tài liệu
- [ ] Giải thích tại sao Domain không import EF Core (bằng ẩn dụ não/tay chân)
- [ ] Giải thích Result\<T\> vs Exception với ví dụ cụ thể
- [ ] Nói được trade-off của việc không dùng MediatR / AutoMapper
- [ ] Giải thích Feature Folder vs Layer Folder
- [ ] Viết tay được Factory Method pattern cho một Entity đơn giản

---

## 🆕 Cập Nhật Mới (Ghi chú chuyên sâu phản biện phỏng vấn)

### 1. "Clean Architecture Tinh Giản" là gì?

- **Vấn đề của Clean Architecture truyền thống**: Thường bị over-engineering khi tạo quá nhiều lớp trung gian (mỗi entity có Generic Repository, UnitOfWork, MediatR Commands/Queries, AutoMapper Profile, Handler). Một thay đổi nhỏ phải sửa 8-10 file.
- **Tinh giản (Pragmatic)**:
  - **Giữ cốt lõi**: Giữ nghiêm ngặt hướng phụ thuộc một chiều (Domain <- Application <- Infrastructure <- Api).
  - **Cắt bỏ phần thừa**: Không Generic Repository (dùng trực tiếp EF Core qua `IAppDbContext`), Không MediatR (inject Handler trực tiếp), Không AutoMapper (dùng Extension Methods).

### 2. Cô lập Logic Nghiệp Vụ và Logic AI trong tầng Application

- **Vị trí**: Cùng nằm trong `TourAgent.Application` nhưng tách biệt folder:
  - `Features/`: Chứa nghiệp vụ du lịch thuần túy (Search, GetDetails). Hoàn toàn **không biết** về sự tồn tại của AI.
  - `Agent/`: Chứa điều phối LLM (`AgentLoop`), System Prompts, Tool Definitions.
- **Cơ chế**: AI Agent gọi vào `Features` qua Tool giống như một HTTP Controller gọi. Tắt AI thì core business vẫn hoạt động 100% nguyên vẹn.

### 3. So Sánh Kiến Trúc: SME (TourAgent) vs Enterprise

| Tiêu chí              | SME (TourAgent hiện tại)              | Enterprise (Hệ thống lớn)                                     |
| ----------------------- | --------------------------------------- | ---------------------------------------------------------------- |
| **Cấu trúc**    | Modular Monolith (4 projects)           | Microservices / Distributed Domains (DDD)                        |
| **Message Queue** | In-memory `System.Threading.Channels` | Distributed Broker: RabbitMQ, Apache Kafka, Azure Service Bus    |
| **Database**      | 1 DB PostgreSQL                         | CQRS: Read DB (Elasticsearch/Replicas) tách riêng Write DB     |
| **AI Gateway**    | Gọi API trực tiếp qua `HttpClient` | LiteLLM / Azure APIM Gateway (caching, rate limit, multi-tenant) |
| **Auth**          | Simple JWT / API Key                    | OAuth2 / OIDC (Keycloak, Azure Entra ID), RBAC/ABAC              |
| **Telemetry**     | Bảng audit `LlmCall` trong DB        | OpenTelemetry, Jaeger, Datadog / ELK Stack                       |

### 4. Handler Trực Tiếp vs MediatR

- **Bản chất MediatR**: In-process Message Bus. Dùng Reflection/Service Locator để tìm Handler tại runtime.
- **Hạn chế của MediatR**: Khó debug (không F12 được), che giấu dependencies, overhead runtime.
- **Bù đắp tính năng của MediatR**:
  - Tính năng mạnh nhất của MediatR là Pipeline Behaviors (Logging, Validation).
  - Trong ASP.NET Core hiện đại (.NET 7/8/10), ta bù đắp bằng **`EndpointFilter`** native của Minimal API (`.AddEndpointFilter<ValidationFilter>()`), vừa không cần thư viện ngoài vừa chạy nhanh hơn.

### 5. Cơ Chế Hoạt Động của NetArchTest.Rules

- **Chạy khi nào**: Là **Unit Test thông thường**, chạy trong **vài chục mili-giây** ngay trên local qua `dotnet test` hoặc IDE Test Explorer (không phải đợi đến CI/CD).
- **Cơ chế**: Dùng Reflection quét metadata trong file compiled `.dll`. Đọc toàn bộ class/method xem có tham chiếu sang namespace bị cấm hay không. Nếu vi phạm, test lập tức fail đỏ đèn.

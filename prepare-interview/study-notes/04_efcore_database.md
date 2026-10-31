# 🗄️ NHÓM 4: EF Core 10 & Database

> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10 + PostgreSQL 16)

---

## 🗺️ Bản Đồ Tư Duy

```
EF Core 10 & Database
├── 1. IAppDbContext — Abstraction pattern
├── 2. Entity Configuration (Fluent API)
├── 3. Query Optimization
│   ├── AsNoTracking
│   ├── AsSplitQuery
│   ├── Include / ThenInclude (Eager Loading)
│   └── N+1 Problem
├── 4. Migrations
│   ├── dotnet ef migrations add
│   ├── dotnet ef database update
│   └── Migration trong production
├── 5. PostgreSQL với Npgsql
│   └── So sánh với SQL Server
├── 6. Transaction & Unit of Work
└── 7. Common Pitfalls
```

---

## 1️⃣ IAppDbContext — Abstraction Pattern

### Tại sao không inject DbContext trực tiếp?

```csharp
// Application/Abstractions/IAppDbContext.cs
public interface IAppDbContext
{
    DbSet<Tour> Tours { get; }
    DbSet<TourDeparture> TourDepartures { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<LlmCall> LlmCalls { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

// Infrastructure/Persistence/AppDbContext.cs
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    // ...
}
```

**Lý do cần IAppDbContext:**

|            | Inject DbContext trực tiếp           | Inject IAppDbContext   |
| ---------- | -------------------------------------- | ---------------------- |
| Unit Test  | Cần InMemory DB hoặc Mock phức tạp | Dễ mock interface     |
| Dependency | Application phụ thuộc vào EF Core   | Application độc lập |
| Layer rule | Vi phạm Clean Architecture            | Tuân thủ đúng      |

**Q: Nếu không có IAppDbContext, vi phạm gì?**

> Application layer sẽ phụ thuộc vào EF Core (`using Microsoft.EntityFrameworkCore`) → vi phạm Clean Architecture. Domain/Application phải độc lập với implementation detail.

---

## 2️⃣ Entity Configuration — Fluent API

### Cách cấu hình trong TourAgent

```csharp
// Infrastructure/Persistence/Configurations/TourConfiguration.cs
public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    /// <summary>Cấu hình mapping giữa Tour entity và bảng database.</summary>
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("tours");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.BasePrice)
            .HasColumnType("decimal(18,2)");

        // Relationship: Tour có nhiều TourDeparture
        builder.HasMany(t => t.Departures)
            .WithOne(d => d.Tour)
            .HasForeignKey(d => d.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index để tăng tốc tìm kiếm theo destination
        builder.HasIndex(t => t.Destination);
    }
}
```

**Q: Fluent API vs Data Annotations — trade-off?**

> - **Data Annotations** (`[MaxLength(200)]`): Nhanh, đơn giản, nhưng làm bẩn Domain entity (phụ thuộc EF Core)
> - **Fluent API**: Tách biệt config ra Infrastructure → Domain entity sạch, không biết EF Core
> - TourAgent dùng **Fluent API** vì Domain phải zero-dependency

---

## 3️⃣ Query Optimization

### 3.1 AsNoTracking — Tắt Change Tracker

```csharp
// ✅ Dùng AsNoTracking cho query chỉ đọc
var tours = await _db.Tours
    .AsNoTracking()         // ← Không track, tiết kiệm memory/CPU
    .Where(t => t.Destination.Contains(query.Destination))
    .ToListAsync(ct);

// ❌ Không cần tracking khi chỉ đọc
var tours = await _db.Tours   // EF track tất cả entities → tốn tài nguyên
    .Where(...)
    .ToListAsync(ct);
```

**Khi nào KHÔNG dùng AsNoTracking?**

```csharp
// Khi cần update entity sau khi query → phải track
var conversation = await _db.Conversations
    .FirstOrDefaultAsync(c => c.Id == id, ct);  // Không AsNoTracking

conversation!.AddMessage(role, content);  // Modify entity
await _db.SaveChangesAsync(ct);           // EF biết cần UPDATE
```

### 3.2 N+1 Problem — Nguy Hiểm Ẩn

```csharp
// ❌ N+1 — 1 query lấy tours + N queries lấy departures
var tours = await _db.Tours.ToListAsync(ct);            // 1 query
foreach (var tour in tours)
{
    var deps = tour.Departures.ToList();                // N queries!!! (Lazy loading)
}

// ✅ Eager Loading — 1 query JOIN
var tours = await _db.Tours
    .Include(t => t.Departures)     // JOIN trong 1 query
    .ToListAsync(ct);

// ✅ AsSplitQuery — 2 queries riêng (tốt hơn khi nhiều Include)
var tours = await _db.Tours
    .Include(t => t.Departures)
    .Include(t => t.Policies)
    .AsSplitQuery()                 // 2 queries riêng thay vì 1 query JOIN lớn
    .ToListAsync(ct);
```

**Q: Tại sao AsSplitQuery đôi khi tốt hơn Include?**

> Khi có nhiều `.Include()` với collection → SQL query tạo ra cartesian product rất lớn. Ví dụ: 100 tours × 10 departures × 5 policies = 5000 rows trả về từ 1 query. AsSplitQuery = 3 queries nhỏ, tổng data ít hơn nhiều.

**Root cause N+1**: Lazy loading + loop = anti-pattern phổ biến nhất trong EF Core. Cần `Include()` explicit.

### 3.3 Projection — Chỉ Lấy Dữ Liệu Cần

```csharp
// ✅ Select ra DTO ngay trong query — giảm data transfer
var tourSummaries = await _db.Tours
    .AsNoTracking()
    .Where(t => t.IsActive)
    .Select(t => new TourSummaryResponse(   // Chỉ lấy fields cần
        t.Id,
        t.Name,
        t.BasePrice,
        t.Destination
    ))
    .ToListAsync(ct);

// ❌ Lấy full entity rồi mới map
var tours = await _db.Tours.ToListAsync(ct);   // Lấy 100 fields
var dtos = tours.Select(t => t.ToResponse());  // Map sau
```

---

## 4️⃣ Migrations

### Workflow cơ bản

```bash
# Tạo migration mới
dotnet ef migrations add AddTourPoliciesTable \
  --project src/TourAgent.Infrastructure \
  --startup-project src/TourAgent.Api

# Apply lên DB
dotnet ef database update \
  --project src/TourAgent.Infrastructure \
  --startup-project src/TourAgent.Api

# Xem migration đang pending
dotnet ef migrations list

# Rollback về migration trước
dotnet ef database update PreviousMigrationName
```

**Q: Migration trong production thì làm sao?**

> - **Không** chạy `database update` trực tiếp trong production app startup
> - Script SQL từ migration: `dotnet ef migrations script`
> - Apply script qua DBA hoặc CI/CD pipeline có approval
> - Hoặc `app.Services.GetRequiredService<AppDbContext>().Database.MigrateAsync()` — có controlled startup

---

## 5️⃣ PostgreSQL vs SQL Server

| Tiêu chí       | PostgreSQL                                | SQL Server                                   |
| ---------------- | ----------------------------------------- | -------------------------------------------- |
| License          | Free, Open Source                         | Có bản free (Express), paid cho enterprise |
| Performance      | Tốt với read-heavy                      | Tốt với write-heavy transaction            |
| JSON support     | Native `jsonb` type                     | JSON functions                               |
| Full-text search | Built-in                                  | Full-text index                              |
| Docker           | Nhẹ, phổ biến                          | Nặng hơn                                   |
| Cloud            | AWS RDS, Supabase, Neon                   | Azure SQL                                    |
| Npgsql           | `Npgsql.EntityFrameworkCore.PostgreSQL` | `Microsoft.EntityFrameworkCore.SqlServer`  |

**TourAgent chọn PostgreSQL vì**: Docker-friendly, free, phù hợp SME, cloud agnostic.

### Npgsql specific features

```csharp
// Cài Npgsql
// Infrastructure.csproj:
// <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />

// Program.cs / DI setup
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions
            .MigrationsAssembly("TourAgent.Infrastructure")
            .EnableRetryOnFailure(3)  // Tự động retry khi transient failure
    ));
```

---

## 6️⃣ Transaction & SaveChangesAsync

### Unit of Work qua DbContext

```csharp
// Handler xử lý nhiều thay đổi trong 1 transaction
public async Task<Result<Unit>> HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
{
    // Tạo order
    var orderResult = Order.Create(cmd.TourId, cmd.CustomerId);
    if (!orderResult.IsSuccess) return Result<Unit>.Failure(orderResult.Error!);

    await _db.Orders.AddAsync(orderResult.Value, ct);

    // Cập nhật departure availability
    var departure = await _db.TourDepartures.FindAsync(cmd.DepartureId, ct);
    departure!.DecrementAvailability();

    // Lưu cả 2 thay đổi trong 1 transaction
    await _db.SaveChangesAsync(ct);  // EF Core tự wrap trong transaction

    return Result<Unit>.Success(Unit.Value);
}
```

**Q: DbContext có thread-safe không?**

> **KHÔNG**. DbContext là `Scoped` (1 per HTTP request). Không được share DbContext giữa các thread. BackgroundService phải tạo DbContext mới qua `IServiceScopeFactory`.

```csharp
// BackgroundService — cần tạo scope riêng
public class TelegramBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(ct))
        {
            // Tạo scope mới cho mỗi message — DbContext mới
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<HandleIncomingMessageHandler>();
            await handler.HandleAsync(new HandleIncomingMessageCommand(message), ct);
        }
    }
}
```

---

## 📝 Checklist Tự Kiểm Tra

- [ ] Giải thích tại sao cần IAppDbContext (không inject DbContext trực tiếp)
- [ ] Biết khi nào dùng / không dùng AsNoTracking
- [ ] Nhận diện được N+1 Problem và cách fix
- [ ] Giải thích AsSplitQuery vs Include thông thường
- [ ] Viết được migration workflow (3 lệnh dotnet ef)
- [ ] Giải thích DbContext không thread-safe và cách xử lý trong BackgroundService

---

## 🆕 Cập Nhật Mới

- Contract: [IAppDbContext.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/IAppDbContext.cs)
- Cài đặt & Fluent API: [AppDbContext.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/AppDbContext.cs) & [TourConfiguration.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/TourConfiguration.cs)
- Query Handler: [GetAllToursHandler.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Features/Tours/GetAllTours/GetAllToursHandler.cs)
- Command Handler: [CreateTourHandler.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Features/Tours/CreateTour/CreateTourHandler.cs)
- Rich Domain Model: [Tour.cs](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Domain/Tours/Tour.cs)

Dưới đây là **5 cụm câu hỏi bắt bẻ thực chiến nhất** (kèm file code dẫn chứng) được trích xuất thẳng từ dự án để bạn bổ sung và hoàn thiện vào file [04_efcore_database.md](file:///d:/github/Tour-Agent/prepare-interview/study-notes/04_efcore_database.md).

---

## 🥊 Cụm 1: Bắt bẻ về `IAppDbContext` vs `Repository Pattern`

### 📍 Dẫn chứng trong code:

- [IAppDbContext.cs:L11-L69](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/IAppDbContext.cs#L11-L69): Khai báo trực tiếp `DbSet<Tour> Tours { get; }`, `DbSet<TourDeparture> TourDepartures { get; }`...
- [GetAllToursHandler.cs:L15-L25](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Features/Tours/GetAllTours/GetAllToursHandler.cs#L15-L25): Inject thẳng `IAppDbContext` vào Handler.

### Tại sao e không dùng Repository để bọc DbContext và DbSet

Vì những dự án trước đó e dùng Repository và Unit Of Work, chủ yếu là vì thói quen và template có sẵn, nên gần như không hiểu lý do thực sự. Đợt này nhờ không áp dụng nên e cũng có thêm được nhiều góc nhìn mới. E từng nghe ko cần dùng Unit Of Work và Repository, vì bản thân DbContext và DbSet có cơ chế giống Unit Of Work và Repository rồi. Lúc đó e ko hiểu, nhưng giờ thật sự e đã hiểu

Còn việc nếu ko dùng repository thì sẽ được những:

* ưu điểm là

  * giữ được các tính năng mạnh của EF Core
  * Handler tự do dùng LinQ, projection để select những column cần thiết để tiết kiệm RAM và băng thông. AsNoTracking để entity không bị context theo dõi nhằm tăng tốc độ đọc. Không cần xin phép qua lớp Repository
* còn nhược điểm là:

  * khó mock, khó unit test hơn cần dùng in memory db
  * Application bị phụ thuộc EF Core
* nếu dùng repository sẽ có ưu điểm

  * Có 1 lớp đứng giữa business và database. Tầng App không cần biết đến ORM hay DbSet
  * dễ mock nên sẽ dễ unit test hơn
  * Việc thêm repository e nghĩ là sẽ tốt với dự án lớn, nhiều business phức tạp cần được bảo vệ khỏi công nghệ. Việc bảo vệ ở đây là handler ko cần phải using EF Core. thêm cache thì chỉ cần update trong repository, thay vì update trong từng handler
* Nhược điểm

  * mất tính linh hoạt của LinQ: khi cần get theo 1 filter nào đó thì cần tạo method mới, hoặc viết specification pattern. repository bị phình nhanh

### Brainstorm với AI để hiểu hơn từng khái niệm

* DbContext: Là 1 phiên làm việc với database, bên trong có 2 thứ quan trọng. ChangeTracker để theo dõi entity vừa được create, update, delete. SaveChangesAsync() sẽ gom mọi thay đổi đã theo dõi thành câu lệnh sql và mặc định chạy trong 1 transaction
* DbSet là cửa vào của entity dùng để lấy hoặc thao tác dữ liệu như là get, create, update, delete
* Repository: bản chất là 1 lớp đứng giữa business logic và database. Code business chỉ cần yêu cầu lấy hoặc thao tác dữ liệu, phía sau có thể là sql server, postgres, api hay file thì ko cần biết. Mục đích là che giấu SQL, ORM, table, column

  * ưu điểm:

    * tách biệt ORM và Mock cực dễ: tầng App không cần biết đến ORM hay DbSet
    * khi tạo unit test dễ mock
  * nhược điểm:

    * mất tính linh hoạt của LINQ: mỗi khi cần kiểu lọc mới thì sẽ phải thêm method mới vào repository hoặc thêm specification pattern. repository rất nhanh bị phình to
* IDbContext

  * ưu điểm:

    * khai thác tối đa sức mạnh của LINQ: tự do filter, pagination, include, hoặc tối ưu bộ nhớ AsNoTracking mà ko phải xin phép qua lớp Repository
  * nhược điểm:

    * khó mock khi tạo unit test: thường phải dùng In Memory db
* Unit of work: là 1 đối tượng ghi nhớ mọi thay đổi từ create, update, delete trong 1 business, rồi ghi xuống database 1 lần duy nhất khi commit. Ghi được thì ghi hết, lỗi thì không ghi gì

---

## 🥊 Cụm 2: Bắt bẻ về Hiệu năng Query: `AsNoTracking`

### tại sao e dùng AsNoTracking trong query?

Mặc định thì EF Core sẽ lưu snapshot của mọi entity đc query và đưa vào ChangeTracker để phát hiện thay đổi. Việc dùng AsNoTracking để không track entity, giúp cải thiện tốc độ cũng như tiết kiệm tài nguyên cho ứng dụng. Nếu query đã dùng projection để select sang DTO thì việc thêm AsNoTracking sẽ bị thừa và ko có tác dụng, vì EF chỉ track entity type

---

## 🥊 Cụm 3: Bắt bẻ về Rich Domain Model & EF Core Encapsulation

### 📍 Dẫn chứng trong code:

- [Tour.cs:L16-L18](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Domain/Tours/Tour.cs#L16-L18): Constructor rỗng `private Tour() {}`.
- [Tour.cs:L184](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Domain/Tours/Tour.cs#L184): `public IReadOnlyCollection<TourDeparture> Departures => _departures.AsReadOnly();`.
- [Tour.cs:L209](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Domain/Tours/Tour.cs#L209): Factory method `public static Result<Tour> Create(...)`.

### ❓ Câu hỏi phỏng vấn & vặn vẹo của Interviewer:

1. **Level 1 (Concept)**: *"Tại sao các properties trong `Tour.cs` đều là `private set`, không có constructor public, mà lại phải có một `private Tour() {}` rỗng?"*
2. **Level 2 (Bắt bẻ EF Core mapping)**: *"Property `Departures` chỉ là `IReadOnlyCollection`, bên ngoài không thể `tour.Departures.Add(...)`. Vậy khi em gọi `_dbContext.Tours.Add(tour);`, làm sao EF Core biết để insert các bản ghi con vào bảng `travel_departures`?"*
3. **Level 3 (Bảo vệ Invariants)**: *"Tại sao em tạo entity qua `Tour.Create(...)` trả về `Result<Tour>`? Nếu ở tầng API đã có `FluentValidation` validate request rồi thì việc validate trong Domain `Tour.Create` có bị thừa thãi (duplicate) không?"*

### 💡 Gợi ý trả lời chuẩn Senior:

- **Private Constructor**: EF Core khi load data từ database lên không dùng constructor nghiệp vụ mà sử dụng Reflection/IL Emitting để khởi tạo instance qua constructor không tham số (kể cả private), sau đó set giá trị thẳng vào backing field hoặc property.
- **Encapsulation Backing Field**: Trong `Tour.cs`, ta có private field `private readonly List<TourDeparture> _departures = [];`. Theo quy ước (Convention) của EF Core, khi gặp navigation property `Departures`, nó tự động nhận diện và ánh xạ vào backing field `_departures` tương ứng. Điều này giúp bảo toàn nguyên tắc OOP/DDD: **Bên ngoài không được tùy tiện Add/Remove item làm vỡ ràng buộc của Aggregate Root, mà phải thông qua method nghiệp vụ `tour.AddDeparture(...)`**.
- **Tại sao không sợ duplicate validation giữa FluentValidation và Domain**:
  - `FluentValidation` ở tầng ngoài (API/Application) bảo vệ **Input Format**: định dạng chuỗi, kiểu số, null/empty.
  - `Domain Invariant` trong `Tour.Create` bảo vệ **Business Rules sống còn**: ví dụ: `PriceAdult >= 0`, `SingleSupplement` không vượt quá giá tour... Domain Entity phải luôn luôn ở trạng thái hợp lệ (Always-valid domain model), dù dữ liệu đến từ Web API, Telegram Webhook hay Background Job.

---

## 🥊 Cụm 4: Bắt bẻ về Database Mapping: Value Converter & Cột JSON

### 📍 Dẫn chứng trong code:

- [TourConfiguration.cs:L18-L36](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/TourConfiguration.cs#L18-L36): `builder.ToTable("travels");`, map `Name` thành `title`, `Destination` thành `route`...
- [TourConfiguration.cs:L66-L72](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/TourConfiguration.cs#L66-L72): Cột `inclusions`, `exclusions`, `itinerary` được cấu hình kiểu `json`.
- [TourConfiguration.cs:L90-L96](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Infrastructure/Persistence/Configurations/TourConfiguration.cs#L90-L96): `HasConversion(...)` giữa `bool IsActive` và chuỗi `"published"` / `"draft"`.

### ❓ Câu hỏi phỏng vấn & vặn vẹo của Interviewer:

1. **Level 1 (Thiết kế)**: *"Tại sao Entity tên là `Tour` nhưng bảng trong PostgreSQL lại là `travels`? Các property như `Name` lại map vào cột `title`? Tại sao không dùng chuẩn đặt tên đồng nhất?"*
2. **Level 2 (Value Converter)**: *"Cột `status` trong DB lưu dạng text (`'published'`, `'draft'`), tại sao trên C# em lại map thành `bool IsActive` bằng `HasConversion`? Đánh đổi của việc dùng Value Converter trong EF Core là gì?"*
3. **Level 3 (JSON Column Trade-off)**: *"Lịch trình (`itinerary`), điều khoản bao gồm (`inclusions`) em lưu thẳng vào cột `json` trong PostgreSQL. Khi nào thì nên lưu JSON trong DB, và khi nào bắt buộc phải chuẩn hóa tách ra bảng con (1-N)?"*

### 💡 Gợi ý trả lời chuẩn Senior:

- **Tách biệt Domain Model và Database Schema**: Đây là một trong những điểm mạnh nhất của Clean Architecture. Database schema có thể là cơ sở dữ liệu cũ (Legacy DB) hoặc được thiết kế theo chuẩn chung của công ty (`travels`, `title`, `route`). Tầng Domain có toàn quyền đặt tên theo đúng Ubiquitous Language nghiệp vụ (`Tour`, `Name`, `Destination`). Fluent API của EF Core đóng vai trò là "cầu nối phiên dịch", giúp code nghiệp vụ không bị méo mó theo database.
- **Value Converter Trade-off**:
  - *Ưu điểm*: Code C# thể hiện đúng kiểu dữ liệu logic (`bool` bật/tắt bán), còn DB vẫn tương thích với schema text cũ.
  - *Đánh đổi*: Nếu câu query phức tạp hoặc group by trên cột có Value Converter, EF Core đôi khi không thể dịch tối ưu xuống SQL hoặc không tận dụng được index nếu converter can thiệp vào kiểu dữ liệu quá sâu.
- **Lưu JSON vs Tách bảng con**:
  - *Lưu JSON khi*: Dữ liệu mang tính chất **Document / Read-only / Bundle** (như Lịch trình chi tiết ngày 1, ngày 2; danh sách vật dụng mang theo), AI chỉ đọc để hiển thị hoặc nhúng vào prompt, hệ thống **không bao giờ có nhu cầu JOIN, Index hoặc Aggregation (SUM/COUNT)** trên từng ngày riêng lẻ.
  - *Bắt buộc tách bảng con khi*: Dữ liệu cần quản lý trạng thái, tồn kho, giá biến động theo thời gian (ví dụ: `TourDeparture` có ngày bay, giá riêng từng ngày, số chỗ còn trống phải tách ra bảng `travel_departures` để truy vấn và update độc lập).

---

## 🥊 Cụm 5: Bắt bẻ về Concurrency & Transaction trong CQRS Handler

### 📍 Dẫn chứng trong code:

- [CreateTourHandler.cs:L137-L138](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Features/Tours/CreateTour/CreateTourHandler.cs#L137-L138):
  ```csharp
  _dbContext.Tours.Add(tour);
  await _dbContext.SaveChangesAsync(cancellationToken);
  ```
- [IAppDbContext.cs:L68](file:///d:/github/Tour-Agent/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/IAppDbContext.cs#L68): Chỉ có method `SaveChangesAsync`, không có `BeginTransactionAsync`.

### ❓ Câu hỏi phỏng vấn & vặn vẹo của Interviewer:

1. **Level 1 (Unit of Work)**: *"Trong `CreateTourHandler`, em gọi `_dbContext.Tours.Add(tour)` kèm cả departures rồi mới gọi `SaveChangesAsync`. Nếu quá trình lưu departures bị lỗi thì tour cha có bị lưu vào DB không?"*
2. **Level 2 (Transaction liên bảng)**: *"Nếu trong 1 use case em vừa phải trừ số chỗ khởi hành của tour, vừa phải tạo đơn hàng `TourOrder`, vừa phải cập nhật CRM Contact. Nếu bước cuối bị lỗi thì sao? Trong `IAppDbContext` của em không có `BeginTransactionAsync`, em xử lý Transaction như thế nào?"*
3. **Level 3 (Race Condition / Overselling)**: *"Nếu chỉ còn đúng 1 chỗ cuối cùng cho chuyến đi Nhật Bản, có 2 khách hàng đồng thời bấm chốt đơn qua AI Agent. Code của em có cơ chế gì để chống bán quá số lượng (Overselling / Concurrency conflict) không?"*

### 💡 Gợi ý trả lời chuẩn Senior:

- **Mặc định của `SaveChangesAsync`**: Mọi thao tác trong cùng một lượt gọi `SaveChangesAsync` của EF Core đều được tự động bọc trong một **Database Transaction ngầm định (Implicit Transaction)**. Nếu bất kỳ bản ghi nào lỗi (ví dụ lỗi foreign key của departure), toàn bộ thao tác sẽ tự động rollback.
- **Khi cần Transaction phức tạp**:
  - `SaveChangesAsync` của EF Core đã đảm bảo tính nguyên tử (Atomicity) cho tất cả các bảng nằm trong cùng một DbContext.
  - Nếu cần phối hợp nghiệp vụ nhiều bước có side-effect ngoài DB (như gọi Telegram / thanh toán cổng thứ 3), áp dụng mô hình **Outbox Pattern**: ghi event vào bảng Outbox trong cùng DbContext transaction, sau đó Background Service sẽ quét Outbox để gửi đi.
- **Chống Concurrency Conflict (Overselling)**:
  - Có 2 chiến lược:
    1. **Optimistic Concurrency**: Thêm cột `xmin` (trong PostgreSQL) hoặc `byte[] RowVersion` vào bảng `TourDeparture`. Khi có xung đột cập nhật đồng thời, EF Core sẽ ném `DbUpdateConcurrencyException`, lúc này Handler sẽ bắt lỗi và trả về `Result.Failure(TourErrors.DepartureFull)`.
    2. **Pessimistic Locking**: Dùng SQL Raw hoặc query với clause `FOR UPDATE` trong PostgreSQL (`SELECT * FROM travel_departures WHERE id = @id FOR UPDATE`) để khóa dòng đó cho đến khi transaction kết thúc.

---

### 📝 Hướng dẫn cập nhật vào [04_efcore_database.md](file:///d:/github/Tour-Agent/prepare-interview/study-notes/04_efcore_database.md):

Bạn có thể mở file `04_efcore_database.md` ra và copy nguyên văn 5 cụm này vào tương ứng các mục:

- Cụm 1 ➔ Mục **1️⃣ IAppDbContext — Abstraction Pattern**
- Cụm 2 ➔ Mục **3️⃣ Query Optimization & Pitfalls**
- Cụm 3 ➔ Mục **2️⃣ Entity Configuration & Domain Invariants**
- Cụm 4 ➔ Mục **5️⃣ PostgreSQL & Mapping Details**
- Cụm 5 ➔ Mục **6️⃣ Transaction & Concurrency Control**

Bạn có muốn tôi giúp bạn ghi đè cập nhật nội dung hoàn chỉnh này vào thẳng file [04_efcore_database.md](file:///d:/github/Tour-Agent/prepare-interview/study-notes/04_efcore_database.md) luôn không?

<!-- Thêm kiến thức/câu hỏi mới vào đây -->

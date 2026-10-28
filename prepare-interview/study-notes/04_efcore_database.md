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
| | Inject DbContext trực tiếp | Inject IAppDbContext |
|---|---|---|
| Unit Test | Cần InMemory DB hoặc Mock phức tạp | Dễ mock interface |
| Dependency | Application phụ thuộc vào EF Core | Application độc lập |
| Layer rule | Vi phạm Clean Architecture | Tuân thủ đúng |

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

| Tiêu chí | PostgreSQL | SQL Server |
|---|---|---|
| License | Free, Open Source | Có bản free (Express), paid cho enterprise |
| Performance | Tốt với read-heavy | Tốt với write-heavy transaction |
| JSON support | Native `jsonb` type | JSON functions |
| Full-text search | Built-in | Full-text index |
| Docker | Nhẹ, phổ biến | Nặng hơn |
| Cloud | AWS RDS, Supabase, Neon | Azure SQL |
| Npgsql | `Npgsql.EntityFrameworkCore.PostgreSQL` | `Microsoft.EntityFrameworkCore.SqlServer` |

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

<!-- Thêm kiến thức/câu hỏi mới vào đây -->

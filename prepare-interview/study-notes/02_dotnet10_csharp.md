# ⚡ NHÓM 2: .NET 10 & C# Modern Features

> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

## 1️⃣ Project Configuration

* Nullable enable: compiler sẽ cảnh báo khi có nguy cơ null, cần phải xử lý nullable rõ ràng hơn
* TreatWarningsAsErrors enable: Warning bị xem như Error và không build được. Giải quyết vấn đề dev có xu hướng bỏ warning. setting này là quy trình tự động kiểm soát chất lượng code

---

## 2️⃣ C# Language Features

### 2.1 null conditional assignment (c# 14)

vấn đề auto-property {get;set} rất gọn. nhưng chỉ cần thêm 1 dòng kiểm tra dữ liệu thì sẽ phải kèm theo backing-field

* ưu điểm:
  * vế trái chỉ tính 1 lần
  * an toàn hơn với field dùng chung giữa nhiều thread. (tránh được kiểu lỗi **check-then-act race** , hay TOCTOU (time-of-check to time-of-use))

```
if (_customer != null)
    _customer.Status = "hot";   // thread khác có thể set _customer = null giữa 2 dòng

// Với _customer?.Status = "hot", field chỉ được đọc một lần vào biến tạm nên không có khe hở này
```

* nhược điểm:

  * nếu vế trái bị null, phép gán bên phải sẽ ko báo gì. việc này sẽ dấn đến việc che mất bug
* Bản chất của null condition:

  * là 1 quyết định về business. Compiler đang hỏi mình "nếu null thì bạn muốn xử lý thế nào", đây là câu hỏi liên quan business chứ ko phải về cú pháp. Nếu mình gõ ?. chỉ vì IDE warning, nghĩa là mình đang quyết định điều đó mà ko hề cân nhắc. cảnh báo biến mất nhưng bug vẫn còn và chuyển thành lỗi im lặng
  * Không nên phản xạ sai bằng cách thêm ?. để hết cảnh báo
* Cùng nhóm còn có dấu (!) null-forgiving

  * nó nói với compiler "tin tôi đi, không null đâu" và yêu cầu compiler đừng cảnh báo nữa
  * dấu ! còn nguy hiểm hơn ?. nếu giá trị thật sự null thì chương trình vẫn crash, chỉ là crash trễ hơn, null bị lan truyền đi xa, và khó tìm nguyên nhân hơn
  * Compiler thật sự muốn báo là "tôi ko chứng mình được giá trị này khác null". Chỉ dùng khi mình thực sự chứng minh đc giá trị ko null mà compiler ko suy ra được
* Quy tắc cho team và cho code AI viết

  * không dùng ! mà ko có comment giải thích tại sao chắc chắn ko null. Nếu ko viết đc lý do, nghĩa là ko chắc
  * khi review code thì soi ! . Đây là các điểm rủi ro cần xem kỹ. code AI sinh ra hay dùng ! để cho compile qua
  * có thể bật WarningAsError, khi đó cảnh báo lỗi buộc dev phải thật sự xử lý thay vì để đó
* Mẹo tự kiểm tra

  * Đọc ! thành câu "tôi cam đoan giá trị này ko null vì ..." => cần phải điền được lý do cụ thể, kiểm chứng được, kèm comment

### 2.2 Primary Constructors (C# 12)

* mục đích chính là giảm thiểu tối đa code thừa (boilerplate), và tăng độ sạch của mã (readability)
* đối với interface, khi inject thì chỉ cần khai báo 1 lần ở class, ko cần private field, ko cần constructor body
* nhược điểm là tham số interface ko phải là read-only nên có thể bị gán lại. nhưng primary constructor vẫn phổ biến và an toàn vì
  * rủi ro gán lại rất thấp
  * giải quyết lỗi quên gán ở cách cũ
* giải quyết nhược điểm bị gán lại và để an toàn tuyệt đối thì có thể gán tham số vào field read-only như cách cũ. cách này thì vẫn bỏ được phần constructor body, nhưng giữ đc tính readonly

```csharp
// ✅ C# 12 — Primary Constructor giảm boilerplate DI
public sealed class SearchToursHandler(IAppDbContext db)
{
  	private readonly IAppDbContext _db = db; // giải quyết vấn đề readonly
    /// <summary>Tìm kiếm tour theo các tiêu chí lọc.</summary>
    public async Task<Result<IReadOnlyList<TourResponse>>> HandleAsync(
        SearchToursQuery query, CancellationToken ct)
    {
        // db được capture tự động từ primary constructor
        return await _db.Tours...
    }
}

// ❌ Cách cũ — verbose hơn
public sealed class SearchToursHandler
{
    private readonly IAppDbContext _db;
    public SearchToursHandler(IAppDbContext db) => _db = db;
}
```

### 2.5 Global Usings

Mục đích: để giải phóng file code khỏi những using lặp lại ở đầu mỗi file và tăng độ sạch của file c#

nhược điểm: của global using chỉ là xung đột tên class và nhiễu tính năng gợi ý của IDE

best practice: để tận dụng tốt file global using là, nếu 70% số file trong project dùng library đó thì hãy đưa vào global using. ví dụ serilog, automapper, fluentvalidation

Thông tin thêm:

* về bản chất thì using chỉ là 1 chỉ thị cho trình biên dịch, giúp bạn viết code ngắn hơn chứ bản thân nó ko sinh ra mã máy khi ứng dụng chạy
  * cơ chế của c# là khi viết using system.text, thì bạn đang nói với compiler rằng "nếu tôi viết StringBuilder, hãy hiểu là tôi đang gọi System.Text.StringBuilder"
  * kết quả sau khi biên dịch (IL code) sang ngôn ngữ trung gian IL, thì tất cả tên class đều đc viết dưới dạng đầy đủ. vd StringBuilder sẽ thành System.Text.StringBuilder
  * ko làm tăng kích thước file, ko chiếm RAM, ko chạy chậm vì thay báo thừa using. kích thước chỉ tăng khi bạn thực sự viết code gọi đến các class trong library đó thôi
  * việc using mà ko dùng đến thì không gây runtime performance, chỉ ảnh hưởng cực kỳ nhỏ đến compile time

```csharp
// GlobalUsings.cs (trong mỗi project) → Không cần lặp lại ở đầu mỗi file
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Microsoft.EntityFrameworkCore;
```

---

## 3️⃣ ASP.NET Core .NET 10

### Minimal APIs vs Controller

ưu điểm

* **Minimal API**: Ít code hơn, performance tốt hơn một chút (~5-10%), phù hợp microservice/SME
* **Controller**: Convention rõ ràng, dễ organize trong team lớn, nhiều tính năng built-in hơn

có thể dùng bộ câu hỏi sau để xác định dùng minimal API vs controller

* đã có codebase sẵn chưa? nếu có thì giữ nguyên
* cần Native AOT, trimming thì => minimal API, nếu ko thì dùng controller
* team biết gì và bảo trì trong bao lâu? chọn cái team viết nhanh và debug đc
* codebase dự kiến nhiều hay ít endpoint. nếu ít thì có thể dùng minimal api, nhiều thì cần tổ chức convention + route group hoặc dùng controller

---

## 4️⃣ Async Best Practices

### CancellationToken — PHẢI truyền qua mọi hàm async

```csharp
// ✅ Đúng — TourAgent rule bắt buộc
public async Task<Result<IReadOnlyList<TourResponse>>> HandleAsync(
    SearchToursQuery query,
    CancellationToken ct)   // ← PHẢI có
{
    var tours = await _db.Tours
        .ToListAsync(ct);   // ← Truyền xuống tận EF Core query
    // ...
}

// ❌ Sai — Không có CancellationToken
public async Task<List<Tour>> GetToursAsync()
{
    return await _db.Tours.ToListAsync();  // Nếu client disconnect → vẫn chạy
}
```

**Bản chất**: Khi client disconnect, nếu không propagate CancellationToken → server vẫn tiếp tục tốn CPU, memory, DB connection → resource leak.

**Root cause của nhiều timeout issue**: Thiếu CancellationToken propagation ở tầng giữa, thường là bị quên pass vào khi gọi library.

### IAsyncEnumerable — Streaming dữ liệu lớn

```csharp
// SSE Streaming endpoint
app.MapPost("/api/v1/agent/chat/stream", async (
    ChatRequest request,
    AgentStreamingHandler handler,
    HttpResponse response,
    CancellationToken ct) =>
{
    response.ContentType = "text/event-stream";

    await foreach (var chunk in handler.StreamAsync(request, ct))
    {
        await response.WriteAsync($"data: {chunk}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
});

// Handler trả về IAsyncEnumerable
public async IAsyncEnumerable<string> StreamAsync(
    ChatRequest request,
    [EnumeratorCancellation] CancellationToken ct)
{
    // yield return từng chunk token từ LLM
    await foreach (var token in _llmClient.StreamAsync(request.Message, ct))
    {
        yield return token;
    }
}
```

---

## 5️⃣ Dependency Injection Patterns

### DependencyInjection.cs — Extension Method Pattern

```csharp
// Application/DependencyInjection.cs
public static class DependencyInjection
{
    /// <summary>Đăng ký tất cả services của Application layer.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký tất cả Handlers
        services.AddScoped<SearchToursHandler>();
        services.AddScoped<HandleIncomingMessageHandler>();
        services.AddScoped<AgentLoop>();

        return services;
    }
}

// Program.cs — sử dụng
builder.Services.AddApplication();
```

**Q: Scoped vs Singleton vs Transient — khi nào dùng cái nào?**

| Lifetime      | Tạo mới khi       | Dùng cho                                 |
| ------------- | ------------------- | ----------------------------------------- |
| `Transient` | Mỗi lần resolve   | Stateless services nhẹ                   |
| `Scoped`    | Mỗi HTTP request   | DbContext, Handlers (mặc định cho web) |
| `Singleton` | Một lần duy nhất | Config, HttpClient factory, Cache         |

> **Captive Dependency trap**: Singleton inject Scoped → Scoped sống lâu như Singleton → Bug khó tìm. Compiler không cảnh báo, runtime mới lỗi.

---

## 📝 Checklist Tự Kiểm Tra

- [ ] Giải thích được tại sao cần `TreatWarningsAsErrors`
- [ ] Viết được Primary Constructor syntax
- [ ] Giải thích CancellationToken tại sao phải propagate qua mọi layer
- [ ] Phân biệt Scoped / Singleton / Transient và Captive Dependency trap

<!-- Thêm kiến thức/câu hỏi mới vào đây -->

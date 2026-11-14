# 🛠️ Hướng Dẫn Kỹ Thuật: .NET & Azure App Service

Tài liệu tham khảo chuyên biệt cho các dự án .NET (.NET 8/9/10) triển khai lên Azure App Service Linux.

---

## 1. Cấu Hình CORS Động Trên ASP.NET Core

Trong `Program.cs`:
```csharp
// Ưu tiên biến môi trường CORS_ALLOWED_ORIGINS (chuỗi cách nhau dấu phẩy), sau đó đến appsettings.json
var envOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

var originsToUse = (envOrigins is { Length: > 0 } ? envOrigins : configuredOrigins)
    ?? ["http://localhost:5173", "http://localhost:3000"];

var allowedOrigins = originsToUse
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

---

## 2. Thiết Lập Health Probes (Live & Ready)

Trong `Program.cs`:
```csharp
// 1. Đăng ký các check kèm Tag tương ứng
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("App is running"), tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: new[] { "db", "ready" })
    .AddCheck<DeepSeekHealthCheck>("deepseek_ai", tags: new[] { "ai", "ready" });

// 2. Định tuyến Liveness Probe (Dành cho Azure Health Check probe)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    }
});

// 3. Định tuyến Readiness Probe (Kiểm tra DB & Dependencies)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        if (app.Environment.IsDevelopment())
        {
            var response = new
            {
                status = report.Status.ToString(),
                total_duration_ms = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration_ms = e.Value.Duration.TotalMilliseconds
                })
            };
            await context.Response.WriteAsJsonAsync(response);
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
        }
    }
});
```

---

## 3. Dockerfile Multi-Stage Chuẩn Cho .NET Solution Nhiều Project

```dockerfile
# Stage 1: Runtime base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Tận dụng Layer Cache: Copy file giải pháp và .csproj trước
COPY ["Directory.Build.props", "./"]
COPY ["src/YourApp.Domain/YourApp.Domain.csproj", "src/YourApp.Domain/"]
COPY ["src/YourApp.Application/YourApp.Application.csproj", "src/YourApp.Application/"]
COPY ["src/YourApp.Infrastructure/YourApp.Infrastructure.csproj", "src/YourApp.Infrastructure/"]
COPY ["src/YourApp.Api/YourApp.Api.csproj", "src/YourApp.Api/"]

# Restore trước khi copy toàn bộ source code
RUN dotnet restore "src/YourApp.Api/YourApp.Api.csproj"

# Copy source code và build
COPY src/ src/
WORKDIR "/src/src/YourApp.Api"
RUN dotnet build "YourApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "YourApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "YourApp.Api.dll"]
```

---

## 4. Quy Tắc Ánh Xạ Biến Môi Trường Trên Azure (`__`)

- Trong ASP.NET Core, dấu `:` của cấu hình JSON khi chuyển sang biến môi trường Linux/Azure sẽ được thay bằng **hai dấu gạch dưới (`__`)**.
  - `ConnectionStrings:DefaultConnection` ➔ `ConnectionStrings__DefaultConnection`
  - `DeepSeek:ApiKey` ➔ `DeepSeek__ApiKey` hoặc `DEEPSEEK_API_KEY`
- Khi kết nối Azure PostgreSQL Flexible Server: Chuỗi kết nối bắt buộc phải có `Ssl Mode=Require;Trust Server Certificate=true;`.


using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using TourAgent.Application;
using TourAgent.Infrastructure;
using TourAgent.Infrastructure.Health;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ thuộc tầng Application và Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Đăng ký Controllers đầy đủ kèm hỗ trợ snake_case và không phân biệt hoa thường
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 2.1 Cấu hình CORS cho phép Frontend SPA kết nối linh hoạt theo môi trường
var envOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

var originsToUse = (envOrigins is { Length: > 0 } ? envOrigins : configuredOrigins)
    ?? ["http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173"];

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

// 2.2 Cấu hình Health Checks kiểm tra sức khỏe ứng dụng, PostgreSQL và DeepSeek AI
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Ứng dụng đang chạy bình thường."), tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: new[] { "db", "ready" })
    .AddCheck<DeepSeekHealthCheck>("deepseek_ai", tags: new[] { "ai", "ready" });

// 3. Cấu hình Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TourAgent API",
        Version = "v1",
        Description = "Hệ thống API Backend TourAgent (.NET 10 Clean Architecture) phục vụ AI Agent tư vấn du lịch."
    });
});

var app = builder.Build();

// 4. Cấu hình HTTP request pipeline & Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TourAgent API v1");
        options.RoutePrefix = "swagger"; // Truy cập tại: http://localhost:<port>/swagger
    });
}

// Bỏ qua ép buộc chuyển hướng HTTPS khi test local HTTP để tiện dev
// app.UseHttpsRedirection();

app.UseCors("SpaCorsPolicy");

app.UseAuthorization();

// 4.1 Khởi tạo cấu trúc bảng và nạp dữ liệu mẫu ban đầu cho Grand Slam Offers và CRM Contacts
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TourAgent.Infrastructure.Persistence.AppDbContext>();
    await TourAgent.Infrastructure.Persistence.OfferDataSeeder.EnsureSeededAsync(dbContext);
    await TourAgent.Infrastructure.Persistence.ContactDataSeeder.EnsureSeededAsync(dbContext);
    await TourAgent.Infrastructure.Persistence.OrderDataSeeder.EnsureSeededAsync(dbContext);
}

// 5. Định tuyến Health Checks (Tách biệt Liveness và Readiness Probes)
// 5.1 Liveness Probe: Dành riêng cho Cloud Orchestrator kiểm tra app process còn sống, không check tài nguyên ngoài
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    }
});

// 5.2 Readiness Probe: Kiểm tra tính sẵn sàng phục vụ người dùng (PostgreSQL DB & DeepSeek AI)
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

// 5.3 Endpoint tổng quát /health tương thích ngược
app.MapHealthChecks("/health", new HealthCheckOptions
{
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

app.MapControllers();

app.Run();

using Microsoft.OpenApi;
using TourAgent.Application;
using TourAgent.Infrastructure;

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

// 2.1 Cấu hình CORS cho phép Frontend SPA kết nối
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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

// 5. Định tuyến Controllers
app.MapControllers();

app.Run();

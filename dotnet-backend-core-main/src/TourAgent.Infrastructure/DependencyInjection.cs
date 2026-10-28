using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourAgent.Application.Abstractions;
using TourAgent.Infrastructure.Persistence;

namespace TourAgent.Infrastructure;

/// <summary>
/// Mở rộng IServiceCollection để đăng ký các dịch vụ thuộc tầng Infrastructure vào DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký AppDbContext kết nối PostgreSQL và các dịch vụ hạ tầng vào hệ thống.
    /// </summary>
    /// <param name="services">Tập hợp các dịch vụ DI của ứng dụng.</param>
    /// <param name="configuration">Đối tượng cấu hình ứng dụng.</param>
    /// <returns>IServiceCollection để hỗ trợ method chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'DefaultConnection' trong cấu hình.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<AppDbContext>());

        // Đăng ký Cửa hàng cấu hình LLM runtime bền vững
        services.AddSingleton<Application.Abstractions.ILlmSettingsStore, Llm.LlmSettingsStore>();

        // Đăng ký HttpClient và DeepSeekLlmClient
        services.AddHttpClient<ILlmClient, Llm.DeepSeekLlmClient>();

        // Đăng ký In-Process Telegram Channel Queue
        services.AddSingleton<Application.Channels.Telegram.ITelegramMessageQueue, Channels.TelegramMessageQueue>();

        // Đăng ký Cửa hàng cấu hình Telegram bền vững
        services.AddSingleton<Application.Abstractions.ITelegramSettingsStore, Telegram.TelegramSettingsStore>();

        // Đăng ký Telegram HTTP Adapter
        services.AddHttpClient<ITelegramAdapter, Telegram.TelegramAdapter>();

        // Đăng ký ContactResolver nhận diện và đồng bộ hồ sơ CRM
        services.AddScoped<IContactResolver, Services.ContactResolver>();

        // Đăng ký Background Service tiêu thụ tin nhắn Telegram
        services.AddHostedService<Channels.TelegramWebhookBackgroundService>();

        return services;
    }
}

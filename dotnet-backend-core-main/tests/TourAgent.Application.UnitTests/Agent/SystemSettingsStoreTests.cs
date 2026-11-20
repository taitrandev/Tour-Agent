using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TourAgent.Application.Abstractions;
using TourAgent.Infrastructure.Llm;
using TourAgent.Infrastructure.Persistence;
using TourAgent.Infrastructure.Telegram;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Kiểm thử tính bền vững của LlmSettingsStore và TelegramSettingsStore khi lưu vào CSDL (PostgreSQL/Supabase).
/// </summary>
public sealed class SystemSettingsStoreTests
{
    /// <summary>
    /// Tạo IServiceScopeFactory với DbContext InMemory độc lập.
    /// </summary>
    private static IServiceScopeFactory CreateScopeFactory(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Kiểm tra LlmSettingsStore lưu cấu hình thành công vào bảng system_settings trong CSDL.
    /// </summary>
    [Fact]
    public async Task LlmSettingsStore_UpdateSettingsAsync_PersistsToDatabase()
    {
        // Arrange
        var dbName = $"LlmSettingsDb_{Guid.NewGuid()}";
        var scopeFactory = CreateScopeFactory(dbName);
        var config = new ConfigurationBuilder().Build();
        var store = new LlmSettingsStore(config, NullLogger<LlmSettingsStore>.Instance, scopeFactory);

        // Act: Cập nhật cấu hình Claude / Fable AI
        await store.UpdateSettingsAsync(
            apiKey: "sk-ant-fai-test-token-12345",
            model: "haiku-4.5",
            baseUrl: "https://api.fableaiapi.com",
            temperature: 0.3f,
            driver: "claude");

        // Assert: Đọc trực tiếp từ CSDL xem đã được lưu chưa
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var record = await dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "llm_settings");

        Assert.NotNull(record);
        Assert.Contains("sk-ant-fai-test-token-12345", record.Value);
        Assert.Contains("haiku-4.5", record.Value);
        Assert.Contains("api.fableaiapi.com", record.Value);
        Assert.Contains("claude", record.Value);

        // Khởi tạo một store mới cùng kết nối DB này (mô phỏng container restart)
        var restartedStore = new LlmSettingsStore(config, NullLogger<LlmSettingsStore>.Instance, scopeFactory);
        var settingsAfterRestart = restartedStore.GetSettings();

        Assert.Equal("claude", settingsAfterRestart.Driver);
        Assert.Equal("sk-ant-fai-test-token-12345", settingsAfterRestart.ApiKey);
        Assert.Equal("haiku-4.5", settingsAfterRestart.Model);
        Assert.Equal("https://api.fableaiapi.com", settingsAfterRestart.BaseUrl);
    }

    /// <summary>
    /// Kiểm tra TelegramSettingsStore lưu cấu hình thành công vào bảng system_settings trong CSDL.
    /// </summary>
    [Fact]
    public async Task TelegramSettingsStore_UpdateSettingsAsync_PersistsToDatabase()
    {
        // Arrange
        var dbName = $"TelegramSettingsDb_{Guid.NewGuid()}";
        var scopeFactory = CreateScopeFactory(dbName);
        var config = new ConfigurationBuilder().Build();
        var store = new TelegramSettingsStore(config, NullLogger<TelegramSettingsStore>.Instance, scopeFactory);

        // Act: Cập nhật cấu hình Telegram Bot
        await store.UpdateBotTokenAsync("123456789:ABCDEF_TEST_BOT_TOKEN");
        await store.UpdateWebhookUrlAsync("https://my-webhook.example.com/api/v1/agent/webhook/telegram");
        await store.UpdateAdminChatIdAsync("987654321");

        // Assert: Đọc trực tiếp từ CSDL xem đã được lưu chưa
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var record = await dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "telegram_settings");

        Assert.NotNull(record);
        Assert.Contains("123456789:ABCDEF_TEST_BOT_TOKEN", record.Value);
        Assert.Contains("my-webhook.example.com", record.Value);
        Assert.Contains("987654321", record.Value);

        // Khởi tạo store mới cùng kết nối DB này (mô phỏng container restart)
        var restartedStore = new TelegramSettingsStore(config, NullLogger<TelegramSettingsStore>.Instance, scopeFactory);
        var settingsAfterRestart = restartedStore.GetSettings();

        Assert.Equal("123456789:ABCDEF_TEST_BOT_TOKEN", settingsAfterRestart.BotToken);
        Assert.Equal("https://my-webhook.example.com/api/v1/agent/webhook/telegram", settingsAfterRestart.WebhookUrl);
        Assert.Equal("987654321", settingsAfterRestart.AdminChatId);
    }
}


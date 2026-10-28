using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TourAgent.Application.Channels.Telegram;
using TourAgent.Infrastructure.Channels;
using TourAgent.Infrastructure.Telegram;
using Xunit;

namespace TourAgent.Application.UnitTests.Channels;

/// <summary>
/// Các bài kiểm tra đơn vị cho kênh tích hợp Telegram (Hàng đợi Channel & Telegram Adapter).
/// </summary>
public class TelegramIntegrationTests
{
    /// <summary>
    /// Kiểm tra TelegramMessageQueue đẩy và đọc tin nhắn bất đồng bộ chính xác.
    /// </summary>
    [Fact]
    public async Task TelegramMessageQueue_EnqueueAndRead_ShouldRetrieveMessage()
    {
        // Arrange
        var queue = new TelegramMessageQueue();
        var inbound = new TelegramInboundMessage(
            ChatId: "123456789",
            Text: "Có tour đi Nhật tháng 10 không em?",
            ChannelMessageId: "tg_123456789_101",
            SenderName: "Nguyen Van A");

        // Act
        await queue.EnqueueAsync(inbound);

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var retrieved = false;

        await foreach (var item in queue.ReadAllAsync(cts.Token))
        {
            Assert.Equal("123456789", item.ChatId);
            Assert.Equal("Có tour đi Nhật tháng 10 không em?", item.Text);
            Assert.Equal("Nguyen Van A", item.SenderName);
            retrieved = true;
            break;
        }

        Assert.True(retrieved, "Tin nhắn phải được đọc thành công từ Channel.");
    }

    /// <summary>
    /// Kiểm tra TelegramAdapter hoạt động ở chế độ mô phỏng an toàn khi chưa có BotToken.
    /// </summary>
    [Fact]
    public async Task TelegramAdapter_WithoutBotToken_ShouldSimulateAndReturnTrue()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build(); // Cấu hình rỗng không có token
        var httpClient = new HttpClient();
        var logger = NullLogger<TelegramAdapter>.Instance;
        var adapter = new TelegramAdapter(httpClient, config, logger);

        // Act
        var resultText = await adapter.SendTextMessageAsync("123456", "Dạ chào anh/chị!");
        var resultPhoto = await adapter.SendPhotoAsync("123456", "https://example.com/tour.jpg", "Ảnh tour");

        // Assert
        Assert.True(resultText);
        Assert.True(resultPhoto);
    }
}

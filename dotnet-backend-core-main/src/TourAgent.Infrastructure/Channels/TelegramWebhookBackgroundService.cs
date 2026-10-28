using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Channels.Telegram;
using TourAgent.Domain.Conversations;

namespace TourAgent.Infrastructure.Channels;

/// <summary>
/// Dịch vụ nền (Background Worker) liên tục lắng nghe hàng đợi In-Process Channel
/// để xử lý tin nhắn Telegram từ Webhook, chạy AI AgentLoop và gửi câu trả lời về cho người dùng.
/// </summary>
public sealed class TelegramWebhookBackgroundService : BackgroundService
{
    private readonly ITelegramMessageQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramWebhookBackgroundService> _logger;

    /// <summary>
    /// Khởi tạo Background Service với hàng đợi tin nhắn và Service Scope Factory.
    /// </summary>
    /// <param name="queue">Hàng đợi tin nhắn Telegram Channel.</param>
    /// <param name="scopeFactory">Nhà máy tạo Scope DI để resolve Scoped Services (DbContext, AgentLoop).</param>
    /// <param name="logger">Logger theo dõi tiến trình nền.</param>
    public TelegramWebhookBackgroundService(
        ITelegramMessageQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramWebhookBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Webhook Background Service đã khởi động và đang chờ tin nhắn...");

        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessTelegramMessageAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi xử lý tin nhắn ngầm từ Telegram ChatId {ChatId}", message.ChatId);
            }
        }

        _logger.LogInformation("Telegram Webhook Background Service đang dừng lại...");
    }

    /// <summary>
    /// Xử lý một tin nhắn Telegram đơn lẻ bên trong một Scoped Lifetime độc lập.
    /// </summary>
    private async Task ProcessTelegramMessageAsync(TelegramInboundMessage inbound, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var agentLoop = scope.ServiceProvider.GetRequiredService<IAgentLoop>();
        var telegramAdapter = scope.ServiceProvider.GetRequiredService<ITelegramAdapter>();

        // 1. Kiểm tra Idempotency chống trùng lặp tin nhắn nếu Telegram gửi lại webhook
        var isDuplicate = await dbContext.Messages
            .AsNoTracking()
            .AnyAsync(m => m.ChannelMessageId == inbound.ChannelMessageId, cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Bỏ qua tin nhắn trùng lặp ChannelMessageId: {ChannelMessageId}", inbound.ChannelMessageId);
            return;
        }

        // 2. Tìm hoặc tạo phiên hội thoại Telegram cho người dùng
        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Channel == "telegram" && c.ChannelUserId == inbound.ChatId, cancellationToken);

        if (conversation is null)
        {
            var createResult = Conversation.Create(
                channel: "telegram",
                channelUserId: inbound.ChatId,
                metadataJson: $"{{\"sender_name\": \"{inbound.SenderName ?? string.Empty}\"}}");

            conversation = createResult.Value;
            dbContext.Conversations.Add(conversation);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Tự động nhận diện hồ sơ CRM Contact và trích xuất số điện thoại (Auto Hot Lead)
        var contactResolver = scope.ServiceProvider.GetRequiredService<IContactResolver>();
        await contactResolver.ResolveAndSyncContactAsync(
            channel: "telegram",
            channelUserId: inbound.ChatId,
            text: inbound.Text,
            senderName: inbound.SenderName,
            username: inbound.Username,
            conversation: conversation,
            cancellationToken: cancellationToken);

        // 4. Thực thi chu trình suy luận đa lượt tự động của AI Agent
        var chatInput = new AgentChatInput(
            ConversationId: conversation.Id,
            UserMessage: inbound.Text);

        var agentResult = await agentLoop.RunAsync(chatInput, cancellationToken);

        // 4. Gửi câu trả lời của AI trở lại phòng chat Telegram
        if (!string.IsNullOrWhiteSpace(agentResult.ResponseText))
        {
            var sentSuccess = await telegramAdapter.SendTextMessageAsync(inbound.ChatId, agentResult.ResponseText, cancellationToken);
            if (!sentSuccess)
            {
                _logger.LogWarning("Không thể gửi phản hồi tới Telegram ChatId {ChatId}", inbound.ChatId);
            }
        }
    }
}

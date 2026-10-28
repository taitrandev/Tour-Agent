using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Prompts;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Conversations;

namespace TourAgent.Application.Features.Conversations.GetConversationDetail;

/// <summary>
/// Handler xử lý lấy dữ liệu chi tiết một cuộc hội thoại gồm danh sách tin nhắn, nhật ký LLM và System Prompt tương ứng.
/// </summary>
public sealed class GetConversationDetailHandler : IQueryHandler<GetConversationDetailQuery, Result<ConversationDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetConversationDetailHandler với abstraction cơ sở dữ liệu.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetConversationDetailHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện truy vấn chi tiết hội thoại từ cơ sở dữ liệu và ánh xạ sang DTO hoàn chỉnh.
    /// </summary>
    /// <param name="query">Chứa ID của cuộc hội thoại cần tra cứu.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa ConversationDetailDto hoặc lỗi không tìm thấy.</returns>
    public async Task<Result<ConversationDetailDto>> HandleAsync(
        GetConversationDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<ConversationDetailDto>(
                Error.NotFound("Conversation.NotFound", "Không tìm thấy hội thoại."));
        }

        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == query.ConversationId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        var llmCalls = await _dbContext.LlmCalls
            .AsNoTracking()
            .Where(l => l.ConversationId == query.ConversationId)
            .OrderByDescending(l => l.Id)
            .ToListAsync(cancellationToken);

        var statusString = conversation.Status == ConversationStatus.BotActive ? "bot_active"
            : conversation.Status == ConversationStatus.HumanActive ? "human_active"
            : "resolved";

        var headerDto = new ConversationHeaderDto
        {
            Id = conversation.Id,
            Channel = conversation.Channel,
            ChannelUserId = conversation.ChannelUserId,
            Status = statusString,
            ContactId = conversation.ContactId,
            Metadata = TryParseJsonObject(conversation.MetadataJson),
            CreatedAt = conversation.CreatedAtUtc,
            UpdatedAt = conversation.UpdatedAtUtc
        };

        var messageDtos = messages.Select(m => new MessageItemDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString().ToLowerInvariant(),
            Content = m.Content,
            ToolName = m.ToolName,
            ToolPayload = TryParseJsonObject(m.ToolPayload),
            ChannelMessageId = m.ChannelMessageId,
            CreatedAt = m.CreatedAtUtc
        }).ToList();

        var llmCallDtos = llmCalls.Select(c => new LlmCallItemDto
        {
            Id = c.Id,
            ConversationId = c.ConversationId,
            Model = c.Model,
            Provider = c.Provider ?? "deepseek",
            PromptTokens = c.PromptTokens,
            CompletionTokens = c.CompletionTokens,
            CostUsd = c.CostUsd,
            LatencyMs = c.LatencyMs,
            ToolsCalled = TryParseStringList(c.ToolsCalledJson),
            FinishReason = c.FinishReason ?? "stop",
            StatusBarSnapshot = c.StatusBarSnapshot,
            StateSnapshot = TryParseJsonObject(c.StateSnapshotJson),
            ToolPayloads = TryParseJsonObject(c.ToolPayloadsJson),
            CreatedAt = c.CreatedAtUtc
        }).ToList();

        // Trích xuất travel_id từ metadata nếu có để tạo System Prompt chính xác
        long? currentTravelId = null;
        if (!string.IsNullOrWhiteSpace(conversation.MetadataJson))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(conversation.MetadataJson);
                if (jsonDoc.RootElement.TryGetProperty("travel_id", out var travelProp) && travelProp.TryGetInt64(out var tid))
                {
                    currentTravelId = tid;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON metadata
            }
        }

        var systemPrompt = TravelAgentPrompts.GetCustomerConsultantPrompt(currentTravelId);

        var detailDto = new ConversationDetailDto
        {
            Conversation = headerDto,
            Messages = messageDtos,
            LlmCalls = llmCallDtos,
            SystemPrompt = systemPrompt
        };

        return Result.Success(detailDto);
    }

    /// <summary>
    /// Giải mã chuỗi JSON sang đối tượng object để serialize thành JSON nguyên bản trả về client.
    /// </summary>
    /// <param name="rawJson">Chuỗi JSON cần giải mã.</param>
    /// <returns>Đối tượng object hoặc null nếu chuỗi rỗng/lỗi.</returns>
    private static object? TryParseJsonObject(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<object>(rawJson);
        }
        catch
        {
            return rawJson;
        }
    }

    /// <summary>
    /// Giải mã chuỗi JSON mảng danh sách tên tools sang List string.
    /// </summary>
    /// <param name="toolsJson">Chuỗi JSON mảng tool.</param>
    /// <returns>Danh sách tên tool hoặc null.</returns>
    private static List<string>? TryParseStringList(string? toolsJson)
    {
        if (string.IsNullOrWhiteSpace(toolsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(toolsJson);
        }
        catch
        {
            return null;
        }
    }
}

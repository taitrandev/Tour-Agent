using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Conversations;

namespace TourAgent.Application.Features.Conversations.GetConversations;

/// <summary>
/// Handler xử lý truy vấn danh sách hội thoại, đếm tin nhắn và lượt gọi LLM từ cơ sở dữ liệu.
/// </summary>
public sealed class GetConversationsHandler : IQueryHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationSummaryDto>>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo GetConversationsHandler với abstraction cơ sở dữ liệu.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public GetConversationsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực thi truy vấn danh sách các cuộc hội thoại mới nhất kèm số lượng tin nhắn và lượt gọi LLM.
    /// </summary>
    /// <param name="query">Tham số lọc theo kênh và từ khóa tìm kiếm.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách tóm tắt các cuộc hội thoại.</returns>
    public async Task<Result<IReadOnlyList<ConversationSummaryDto>>> HandleAsync(
        GetConversationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.Conversations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Channel) && !query.Channel.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var channelFilter = query.Channel.Trim().ToLower();
            baseQuery = baseQuery.Where(c => c.Channel == channelFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(c =>
                c.ChannelUserId.ToLower().Contains(keyword) ||
                c.Channel.ToLower().Contains(keyword) ||
                c.Id.ToString().Contains(keyword));
        }

        var rawList = await baseQuery
            .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .Take(50)
            .Select(c => new
            {
                c.Id,
                c.Channel,
                c.ChannelUserId,
                c.Status,
                c.ContactId,
                c.LastMessageAtUtc,
                c.CreatedAtUtc,
                c.MetadataJson,
                MessagesCount = _dbContext.Messages.Count(m => m.ConversationId == c.Id),
                LlmCallsCount = _dbContext.LlmCalls.Count(l => l.ConversationId == c.Id)
            })
            .ToListAsync(cancellationToken);

        var resultList = new List<ConversationSummaryDto>(rawList.Count);

        foreach (var item in rawList)
        {
            var statusStr = item.Status == ConversationStatus.BotActive ? "bot_active"
                : item.Status == ConversationStatus.HumanActive ? "human_active"
                : "resolved";

            resultList.Add(new ConversationSummaryDto
            {
                Id = item.Id,
                Channel = item.Channel,
                ChannelUserId = item.ChannelUserId,
                Status = statusStr,
                ContactId = item.ContactId,
                LastMessageAt = item.LastMessageAtUtc,
                CreatedAt = item.CreatedAtUtc,
                Metadata = TryParseJsonObject(item.MetadataJson),
                MessagesCount = item.MessagesCount,
                LlmCallsCount = item.LlmCallsCount
            });
        }

        return Result.Success<IReadOnlyList<ConversationSummaryDto>>(resultList);
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
}

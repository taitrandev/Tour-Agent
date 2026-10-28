using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Conversations.GetConversations;

/// <summary>
/// Query lấy danh sách tóm tắt các cuộc hội thoại khách hàng phục vụ giám sát và kiểm toán.
/// </summary>
/// <param name="Channel">Kênh giao tiếp cần lọc ('telegram', 'web_widget', 'zalo') hoặc null nếu lấy tất cả.</param>
/// <param name="Search">Từ khóa tìm kiếm theo ID, ChannelUserId hoặc Channel.</param>
public sealed record GetConversationsQuery(
    string? Channel = null,
    string? Search = null) : IQuery<Result<IReadOnlyList<ConversationSummaryDto>>>;

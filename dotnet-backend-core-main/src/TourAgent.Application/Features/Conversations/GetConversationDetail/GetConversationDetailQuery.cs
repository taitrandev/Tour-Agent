using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Conversations.GetConversationDetail;

/// <summary>
/// Query lấy thông tin chi tiết một phiên hội thoại bao gồm lịch sử tin nhắn, telemetry các cuộc gọi LLM và System Prompt.
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại cần tra cứu.</param>
public sealed record GetConversationDetailQuery(long ConversationId) : IQuery<Result<ConversationDetailDto>>;

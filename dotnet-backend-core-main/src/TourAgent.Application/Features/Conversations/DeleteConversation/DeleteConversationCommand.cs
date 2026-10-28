using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Conversations.DeleteConversation;

/// <summary>
/// Lệnh xóa vĩnh viễn một phiên hội thoại cùng toàn bộ tin nhắn và nhật ký LLM liên quan.
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại cần xóa.</param>
public sealed record DeleteConversationCommand(long ConversationId) : ICommand<Result<string>>;

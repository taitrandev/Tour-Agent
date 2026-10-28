using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Conversations.DeleteConversation;

/// <summary>
/// Handler xử lý xóa một cuộc hội thoại kèm theo toàn bộ tin nhắn và nhật ký LLM tương ứng trong cơ sở dữ liệu.
/// </summary>
public sealed class DeleteConversationHandler : ICommandHandler<DeleteConversationCommand, Result<string>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo DeleteConversationHandler với abstraction cơ sở dữ liệu.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public DeleteConversationHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện xóa an toàn các bảng phụ thuộc trước khi xóa phiên hội thoại chính.
    /// </summary>
    /// <param name="command">Chứa mã ID hội thoại cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa thông báo thành công hoặc lỗi không tìm thấy.</returns>
    public async Task<Result<string>> HandleAsync(
        DeleteConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == command.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<string>(
                Error.NotFound("Conversation.NotFound", "Không tìm thấy hội thoại."));
        }

        // Xóa tin nhắn trong cuộc hội thoại
        var messages = await _dbContext.Messages
            .Where(m => m.ConversationId == command.ConversationId)
            .ToListAsync(cancellationToken);
        if (messages.Count > 0)
        {
            _dbContext.Messages.RemoveRange(messages);
        }

        // Xóa các lượt gọi LLM liên kết với cuộc hội thoại
        var llmCalls = await _dbContext.LlmCalls
            .Where(l => l.ConversationId == command.ConversationId)
            .ToListAsync(cancellationToken);
        if (llmCalls.Count > 0)
        {
            _dbContext.LlmCalls.RemoveRange(llmCalls);
        }

        // Xóa cuộc hội thoại
        _dbContext.Conversations.Remove(conversation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success($"Đã xóa thành công hội thoại #{command.ConversationId}.");
    }
}

using TourAgent.Domain.Common;

namespace TourAgent.Domain.Conversations;

/// <summary>
/// Đại diện cho một tin nhắn trong phiên hội thoại tư vấn tour.
/// Hỗ trợ cả vai trò User, Assistant và kết quả tra cứu Tool.
/// </summary>
public sealed class Message : Entity
{
    /// <summary>
    /// Constructor rỗng phục vụ ORM EF Core.
    /// </summary>
    private Message()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng Message với đầy đủ thông số.
    /// </summary>
    private Message(
        long id,
        long conversationId,
        MessageRole role,
        string content,
        string? channelMessageId,
        string? toolName,
        string? toolPayload)
        : base(id)
    {
        ConversationId = conversationId;
        Role = role;
        Content = content;
        ChannelMessageId = channelMessageId;
        ToolName = toolName;
        ToolPayload = toolPayload;
    }

    /// <summary>
    /// Mã định danh của phiên hội thoại sở hữu tin nhắn này.
    /// </summary>
    public long ConversationId { get; private set; }

    /// <summary>
    /// Vai trò của thực thể gửi tin nhắn.
    /// </summary>
    public MessageRole Role { get; private set; }

    /// <summary>
    /// Nội dung văn bản của tin nhắn.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Mã định danh tin nhắn từ kênh bên ngoài (Telegram/Zalo) phục vụ Idempotency chống trùng tin.
    /// </summary>
    public string? ChannelMessageId { get; private set; }

    /// <summary>
    /// Tên của công cụ đã gọi (nếu role là Tool).
    /// </summary>
    public string? ToolName { get; private set; }

    /// <summary>
    /// Dữ liệu đầu vào hoặc kết quả JSON của công cụ đã gọi.
    /// </summary>
    public string? ToolPayload { get; private set; }

    /// <summary>
    /// Tạo tin nhắn mới từ người dùng (Khách hàng).
    /// </summary>
    /// <param name="conversationId">Mã phiên hội thoại.</param>
    /// <param name="content">Nội dung câu hỏi.</param>
    /// <param name="channelMessageId">Mã tin nhắn ngoại bộ để chống trùng lặp.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa Message mới.</returns>
    public static Result<Message> CreateUserMessage(
        long conversationId,
        string content,
        string? channelMessageId = null,
        long id = 0)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<Message>(Error.Validation("Message.EmptyContent", "Nội dung tin nhắn không được để trống."));
        }

        var message = new Message(
            id,
            conversationId,
            MessageRole.User,
            content.Trim(),
            channelMessageId?.Trim(),
            null,
            null);

        return Result.Success(message);
    }

    /// <summary>
    /// Tạo tin nhắn phản hồi từ trợ lý AI.
    /// </summary>
    /// <param name="conversationId">Mã phiên hội thoại.</param>
    /// <param name="content">Nội dung phản hồi của AI.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa Message trợ lý.</returns>
    public static Result<Message> CreateAssistantMessage(
        long conversationId,
        string content,
        long id = 0)
    {
        var message = new Message(
            id,
            conversationId,
            MessageRole.Assistant,
            content.Trim(),
            null,
            null,
            null);

        return Result.Success(message);
    }

    /// <summary>
    /// Tạo tin nhắn ghi lại kết quả tra cứu của công cụ (Tool execution).
    /// </summary>
    /// <param name="conversationId">Mã phiên hội thoại.</param>
    /// <param name="toolName">Tên công cụ đã thực thi.</param>
    /// <param name="toolPayload">Dữ liệu JSON kết quả trả về của công cụ.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa Message của Tool.</returns>
    public static Result<Message> CreateToolMessage(
        long conversationId,
        string toolName,
        string toolPayload,
        long id = 0)
    {
        var message = new Message(
            id,
            conversationId,
            MessageRole.Tool,
            toolPayload,
            null,
            toolName,
            toolPayload);

        return Result.Success(message);
    }
}

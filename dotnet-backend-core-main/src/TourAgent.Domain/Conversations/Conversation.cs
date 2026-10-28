using TourAgent.Domain.Common;

namespace TourAgent.Domain.Conversations;

/// <summary>
/// Đại diện cho một phiên hội thoại tư vấn tour của khách hàng trên một kênh cụ thể (Web / Telegram / Zalo).
/// Quản lý vòng đời tiếp khách của AI và chuyển giao cho người thật (Escalation).
/// </summary>
public sealed class Conversation : Entity
{
    private readonly List<Message> _messages = [];

    /// <summary>
    /// Constructor rỗng phục vụ ORM EF Core.
    /// </summary>
    private Conversation()
    {
    }

    /// <summary>
    /// Khởi tạo đối tượng Conversation mới.
    /// </summary>
    private Conversation(
        long id,
        string channel,
        string channelUserId,
        ConversationStatus status,
        string? metadataJson)
        : base(id)
    {
        Channel = channel;
        ChannelUserId = channelUserId;
        Status = status;
        MetadataJson = metadataJson;
    }

    /// <summary>
    /// Kênh giao tiếp khách hàng sử dụng (ví dụ: 'web', 'telegram', 'zalo').
    /// </summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>
    /// Mã định danh người dùng trên kênh tương ứng (duy nhất theo kênh).
    /// </summary>
    public string ChannelUserId { get; private set; } = string.Empty;

    /// <summary>
    /// Trạng thái phiên hội thoại hiện tại (BotActive, HumanActive, Resolved).
    /// </summary>
    public ConversationStatus Status { get; private set; }

    /// <summary>
    /// Thông tin metadata dạng JSON (lưu SĐT, ghi chú tóm tắt nhu cầu tour, thông tin deal).
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>
    /// Thời điểm gửi tin nhắn gần nhất trong cuộc hội thoại.
    /// </summary>
    public DateTime? LastMessageAtUtc { get; private set; }

    /// <summary>
    /// Mã định danh của hồ sơ Contact CRM liên kết với hội thoại này (nếu có).
    /// </summary>
    public long? ContactId { get; private set; }

    /// <summary>
    /// Mã nhân viên được phân công xử lý phiên hội thoại khi chuyển giao (Escalation).
    /// </summary>
    public long? AssignedTo { get; private set; }

    /// <summary>
    /// Danh sách các tin nhắn đã trao đổi trong phiên hội thoại này.
    /// </summary>
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Phương thức Factory khởi tạo một phiên hội thoại mới cho khách hàng.
    /// </summary>
    /// <param name="channel">Tên kênh giao tiếp.</param>
    /// <param name="channelUserId">Mã người dùng trên kênh.</param>
    /// <param name="metadataJson">Dữ liệu bổ sung ban đầu.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa Conversation mới.</returns>
    public static Result<Conversation> Create(
        string channel,
        string channelUserId,
        string? metadataJson = null,
        long id = 0)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return Result.Failure<Conversation>(Error.Validation("Conversation.EmptyChannel", "Kênh hội thoại (Channel) không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(channelUserId))
        {
            return Result.Failure<Conversation>(Error.Validation("Conversation.EmptyChannelUserId", "Mã người dùng trên kênh (ChannelUserId) không được để trống."));
        }

        var conversation = new Conversation(
            id,
            channel.Trim().ToLowerInvariant(),
            channelUserId.Trim(),
            ConversationStatus.BotActive,
            metadataJson);

        return Result.Success(conversation);
    }

    /// <summary>
    /// Thêm một tin nhắn mới vào lịch sử hội thoại.
    /// </summary>
    /// <param name="message">Tin nhắn cần thêm.</param>
    public void AddMessage(Message message)
    {
        _messages.Add(message);
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Chuyển giao phiên hội thoại cho nhân viên tư vấn con người (Escalation).
    /// </summary>
    /// <param name="reason">Lý do chuyển giao (khách để lại SĐT, khiếu nại, muốn cọc...).</param>
    /// <param name="metadataJson">Dữ liệu cập nhật mới bổ sung SĐT và nhu cầu.</param>
    public void EscalateToHuman(string reason, string? metadataJson = null)
    {
        Status = ConversationStatus.HumanActive;
        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            MetadataJson = metadataJson;
        }

        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Đánh dấu phiên hội thoại đã hoàn thành hoặc chốt đơn thành công.
    /// </summary>
    public void Resolve()
    {
        Status = ConversationStatus.Resolved;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Gắn liên kết phiên hội thoại với một hồ sơ khách hàng CRM cụ thể.
    /// </summary>
    /// <param name="contactId">Mã định danh khách hàng CRM.</param>
    public void LinkContact(long contactId)
    {
        ContactId = contactId;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Cập nhật dữ liệu metadata JSON của phiên hội thoại (agent state, tour context...).
    /// </summary>
    /// <param name="metadataJson">Chuỗi JSON metadata mới.</param>
    public void UpdateMetadata(string metadataJson)
    {
        MetadataJson = metadataJson;
        SetUpdatedAtUtc();
    }
}

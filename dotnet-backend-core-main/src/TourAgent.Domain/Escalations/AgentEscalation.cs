namespace TourAgent.Domain.Escalations;

using TourAgent.Domain.Common;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Tours;

/// <summary>
/// Đại diện cho một phiếu yêu cầu chuyển tiếp tư vấn (Human Escalation / Bàn Quyết Định).
/// Hỗ trợ quy trình Human-in-the-Loop khi khách hàng cần chốt deal, để lại số điện thoại hoặc khiếu nại.
/// Ánh xạ vào bảng 'agent_escalations' trong cơ sở dữ liệu.
/// </summary>
public sealed class AgentEscalation : Entity<long>
{
    /// <summary>
    /// Trạng thái chờ chuyên viên tiếp nhận xử lý.
    /// </summary>
    public const string StatusPending = "pending";

    /// <summary>
    /// Trạng thái chuyên viên đang gọi điện hoặc nhắn tin tư vấn trực tiếp cho khách.
    /// </summary>
    public const string StatusContacting = "contacting";

    /// <summary>
    /// Trạng thái đã duyệt deal / chốt đơn thành công.
    /// </summary>
    public const string StatusApproved = "approved";

    /// <summary>
    /// Trạng thái từ chối đề xuất hoặc đóng phiếu hỗ trợ.
    /// </summary>
    public const string StatusRejected = "rejected";

    /// <summary>
    /// Trạng thái đã đóng phiếu sau khi xử lý xong.
    /// </summary>
    public const string StatusClosed = "closed";

    /// <summary>
    /// Loại đề xuất: Duyệt chiết khấu giá tour.
    /// </summary>
    public const string TypeDiscountApproval = "discount_approval";

    /// <summary>
    /// Loại đề xuất: Khách hàng tiềm năng nóng (có SĐT, đoàn đông, sẵn sàng cọc).
    /// </summary>
    public const string TypeDealHot = "deal_hot";

    /// <summary>
    /// Loại đề xuất: Hỗ trợ tư vấn thông thường từ nhân viên con người.
    /// </summary>
    public const string TypeHumanSupport = "human_support";

    /// <summary>
    /// Constructor rỗng phục vụ Entity Framework Core.
    /// </summary>
    private AgentEscalation()
    {
    }

    /// <summary>
    /// Khóa ngoại liên kết tới phiên hội thoại tương ứng.
    /// </summary>
    public long? ConversationId { get; private set; }

    /// <summary>
    /// Loại ticket đề xuất (discount_approval, deal_hot, human_support).
    /// </summary>
    public string Type { get; private set; } = TypeHumanSupport;

    /// <summary>
    /// Trạng thái của ticket (pending, contacting, approved, rejected, closed).
    /// </summary>
    public string Status { get; private set; } = StatusPending;

    /// <summary>
    /// Tên của khách hàng (nếu đã nhận diện được).
    /// </summary>
    public string? CustomerName { get; private set; }

    /// <summary>
    /// Số điện thoại hoặc Zalo của khách hàng.
    /// </summary>
    public string? CustomerPhone { get; private set; }

    /// <summary>
    /// Chat ID từ kênh tiếp nhận (ví dụ Telegram Chat ID).
    /// </summary>
    public string? ChatId { get; private set; }

    /// <summary>
    /// Phần trăm chiết khấu đề xuất nếu có (%).
    /// </summary>
    public short? DiscountPercent { get; private set; }

    /// <summary>
    /// Quy mô số lượng khách trong đoàn.
    /// </summary>
    public short? GroupSize { get; private set; }

    /// <summary>
    /// Khóa ngoại liên kết tới Tour du lịch mà khách đang quan tâm.
    /// </summary>
    public long? TravelId { get; private set; }

    /// <summary>
    /// Ghi chú tóm tắt nhu cầu hoặc lý do chuyển tiếp.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Mã tin nhắn gửi tới nhóm Telegram của Ban quản lý (nếu có).
    /// </summary>
    public int? AdminMessageId { get; private set; }

    /// <summary>
    /// Tên hoặc mã của chuyên viên đã tiếp nhận hoặc xử lý ticket.
    /// </summary>
    public string? ResolvedBy { get; private set; }

    /// <summary>
    /// Thời điểm ticket được xử lý hoàn tất.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Phương pháp phân giải Tour (explicit_argument, state_confirmed...).
    /// </summary>
    public string? TravelResolutionMethod { get; private set; }

    /// <summary>
    /// Độ tin cậy của thuật toán phân giải Tour (0.00 đến 1.00).
    /// </summary>
    public decimal? TravelResolutionConfidence { get; private set; }

    /// <summary>
    /// Dữ liệu JSON metadata kiểm toán đính kèm.
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>
    /// Thực thể phiên hội thoại liên quan.
    /// </summary>
    public Conversation? Conversation { get; private set; }

    /// <summary>
    /// Thực thể Tour du lịch liên quan.
    /// </summary>
    public Tour? Travel { get; private set; }

    /// <summary>
    /// Tạo mới một bản ghi chuyển tiếp tư vấn (AgentEscalation) với đầy đủ thông tin nghiệp vụ.
    /// </summary>
    /// <param name="conversationId">Mã phiên hội thoại.</param>
    /// <param name="chatId">Mã Chat ID trên kênh (Telegram, Zalo...).</param>
    /// <param name="type">Loại chuyển tiếp (deal_hot, human_support, discount_approval).</param>
    /// <param name="customerName">Tên khách hàng.</param>
    /// <param name="customerPhone">Số điện thoại liên lạc.</param>
    /// <param name="travelId">Mã Tour du lịch quan tâm.</param>
    /// <param name="groupSize">Quy mô đoàn khách.</param>
    /// <param name="discountPercent">Phần trăm giảm giá đề xuất (nếu có).</param>
    /// <param name="notes">Ghi chú tóm tắt nhu cầu.</param>
    /// <param name="resolutionMethod">Phương pháp nhận diện tour.</param>
    /// <param name="resolutionConfidence">Điểm tin cậy của việc nhận diện tour.</param>
    /// <param name="metadataJson">Dữ liệu kiểm toán JSON đi kèm.</param>
    /// <returns>Đối tượng AgentEscalation mới.</returns>
    public static AgentEscalation Create(
        long? conversationId,
        string? chatId,
        string type,
        string? customerName,
        string? customerPhone,
        long? travelId,
        short? groupSize,
        short? discountPercent,
        string? notes,
        string? resolutionMethod = "explicit_argument",
        decimal? resolutionConfidence = 1.0m,
        string? metadataJson = null)
    {
        return new AgentEscalation
        {
            ConversationId = conversationId,
            ChatId = chatId,
            Type = string.IsNullOrWhiteSpace(type) ? TypeHumanSupport : type,
            Status = StatusPending,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            TravelId = travelId,
            GroupSize = groupSize ?? 1,
            DiscountPercent = discountPercent,
            Notes = notes,
            TravelResolutionMethod = resolutionMethod,
            TravelResolutionConfidence = resolutionConfidence,
            MetadataJson = metadataJson,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hợp nhất thông tin bổ sung (Smart Merge) vào ticket pending đã có sẵn.
    /// </summary>
    /// <param name="phone">Số điện thoại cập nhật.</param>
    /// <param name="travelId">Mã tour du lịch cập nhật.</param>
    /// <param name="groupSize">Số lượng khách cập nhật.</param>
    /// <param name="additionalNotes">Ghi chú bổ sung.</param>
    public void MergeUpdate(string? phone, long? travelId, short? groupSize, string? additionalNotes)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            CustomerPhone = phone;
        }

        if (travelId.HasValue && travelId.Value > 0)
        {
            TravelId = travelId;
        }

        if (groupSize.HasValue && groupSize.Value > 1)
        {
            GroupSize = groupSize;
            Type = TypeDealHot;
        }

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? additionalNotes : $"{Notes} | {additionalNotes}";
        }

        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Đánh dấu chuyên viên bắt đầu liên hệ / tư vấn trực tiếp cho khách.
    /// </summary>
    /// <param name="resolvedBy">Tên chuyên viên tiếp nhận.</param>
    public void MarkAsContacting(string resolvedBy)
    {
        Status = StatusContacting;
        ResolvedBy = resolvedBy;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Đánh dấu duyệt ưu đãi / chốt deal thành công.
    /// </summary>
    /// <param name="resolvedBy">Tên người duyệt deal.</param>
    /// <param name="approvedDiscount">Phần trăm giảm giá chốt.</param>
    /// <param name="finalGroupSize">Số lượng khách chốt.</param>
    /// <param name="notes">Ghi chú chốt deal.</param>
    public void Approve(string resolvedBy, short? approvedDiscount = null, short? finalGroupSize = null, string? notes = null)
    {
        Status = StatusApproved;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
        SetUpdatedAtUtc();

        if (approvedDiscount.HasValue)
        {
            DiscountPercent = approvedDiscount;
        }

        if (finalGroupSize.HasValue)
        {
            GroupSize = finalGroupSize;
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes : $"{Notes} | {notes}";
        }
    }

    /// <summary>
    /// Đánh dấu từ chối đề xuất hoặc đóng ticket.
    /// </summary>
    /// <param name="resolvedBy">Tên người xử lý.</param>
    /// <param name="reason">Lý do đóng ticket.</param>
    public void Reject(string resolvedBy, string? reason = null)
    {
        Status = StatusRejected;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
        SetUpdatedAtUtc();

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes} | Lý do đóng: {reason}";
        }
    }

    /// <summary>
    /// Cập nhật mã tin nhắn gửi tới Telegram Admin.
    /// </summary>
    /// <param name="messageId">Mã định danh tin nhắn Telegram.</param>
    public void SetAdminMessageId(int messageId)
    {
        AdminMessageId = messageId;
        SetUpdatedAtUtc();
    }
}

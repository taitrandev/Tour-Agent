using TourAgent.Domain.Common;

namespace TourAgent.Domain.Contacts;

/// <summary>
/// Đại diện cho hồ sơ khách hàng trung tâm (Core CRM Contact 360°).
/// Độc lập hoàn toàn với kênh tiếp cận (Telegram, Web Chat, Zalo) và theo dõi vòng đời khách hàng.
/// </summary>
public sealed class CrmContact : Entity
{
    /// <summary>
    /// Giai đoạn khách hàng mới tiếp cận, chưa cung cấp thông tin liên lạc.
    /// </summary>
    public const string StageProspect = "prospect";

    /// <summary>
    /// Giai đoạn khách hàng có tương tác, hỏi han dịch vụ hoặc quan tâm sản phẩm.
    /// </summary>
    public const string StageWarm = "warm";

    /// <summary>
    /// Giai đoạn khách hàng tiềm năng cao (đã để lại SĐT, sẵn sàng nhận tư vấn chốt sale).
    /// </summary>
    public const string StageHot = "hot";

    /// <summary>
    /// Giai đoạn khách hàng đã thực hiện thanh toán, đặt tour hoặc mua dịch vụ thành công.
    /// </summary>
    public const string StageCustomer = "customer";

    /// <summary>
    /// Giai đoạn khách hàng từ chối, không có nhu cầu hoặc hủy giao dịch.
    /// </summary>
    public const string StageLost = "lost";

    /// <summary>
    /// Constructor rỗng phục vụ Entity Framework Core.
    /// </summary>
    private CrmContact()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng CrmContact mới với đầy đủ thuộc tính cơ bản.
    /// </summary>
    private CrmContact(
        long id,
        string? name,
        string? phone,
        string? email,
        string? telegramChatId,
        string? telegramUsername,
        string? zaloId,
        string channel,
        string stage,
        string? tagsJson,
        string? notes,
        DateTime? lastActiveAtUtc)
        : base(id)
    {
        Name = name;
        Phone = phone;
        Email = email;
        TelegramChatId = telegramChatId;
        TelegramUsername = telegramUsername;
        ZaloId = zaloId;
        Channel = channel;
        Stage = stage;
        TagsJson = tagsJson ?? "[]";
        Notes = notes;
        LastActiveAtUtc = lastActiveAtUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Họ và tên của khách hàng.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Số điện thoại liên lạc của khách hàng.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Địa chỉ email của khách hàng.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Chat ID trên nền tảng Telegram.
    /// </summary>
    public string? TelegramChatId { get; private set; }

    /// <summary>
    /// Tên người dùng (username) trên Telegram (không kèm ký tự @).
    /// </summary>
    public string? TelegramUsername { get; private set; }

    /// <summary>
    /// Định danh người dùng trên kênh Zalo.
    /// </summary>
    public string? ZaloId { get; private set; }

    /// <summary>
    /// Kênh tiếp cận ban đầu của khách hàng (telegram, web_widget, zalo, manual).
    /// </summary>
    public string Channel { get; private set; } = "manual";

    /// <summary>
    /// Phân hạng trạng thái vòng đời khách hàng (prospect, warm, hot, customer, lost).
    /// </summary>
    public string Stage { get; private set; } = StageProspect;

    /// <summary>
    /// Chuỗi JSON lưu trữ danh sách nhãn (tags) của khách hàng.
    /// </summary>
    public string? TagsJson { get; private set; }

    /// <summary>
    /// Ghi chú nội bộ về nhu cầu, lịch trình mong muốn hoặc sở thích của khách hàng.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Thời điểm khách hàng có tương tác hoặc hoạt động gần nhất (UTC).
    /// </summary>
    public DateTime? LastActiveAtUtc { get; private set; }

    /// <summary>
    /// Thời điểm xóa mềm hồ sơ khách hàng (nếu có, UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>
    /// Phương thức Factory tạo mới một hồ sơ khách hàng CRM.
    /// </summary>
    /// <param name="name">Họ tên khách hàng.</param>
    /// <param name="phone">Số điện thoại.</param>
    /// <param name="email">Email.</param>
    /// <param name="telegramChatId">Mã Telegram Chat ID.</param>
    /// <param name="telegramUsername">Tên tài khoản Telegram.</param>
    /// <param name="zaloId">Mã người dùng Zalo.</param>
    /// <param name="channel">Kênh tiếp cận.</param>
    /// <param name="stage">Phân hạng trạng thái.</param>
    /// <param name="tagsJson">Danh sách nhãn dạng JSON.</param>
    /// <param name="notes">Ghi chú nhu cầu.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa CrmContact mới tạo.</returns>
    public static Result<CrmContact> Create(
        string? name,
        string? phone,
        string? email = null,
        string? telegramChatId = null,
        string? telegramUsername = null,
        string? zaloId = null,
        string channel = "manual",
        string? stage = null,
        string? tagsJson = null,
        string? notes = null,
        long id = 0)
    {
        var normalizedChannel = string.IsNullOrWhiteSpace(channel) ? "manual" : channel.Trim().ToLowerInvariant();

        // Tự động nâng thành hot lead nếu khách có số điện thoại mà chưa có stage cụ thể
        var resolvedStage = string.IsNullOrWhiteSpace(stage)
            ? (!string.IsNullOrWhiteSpace(phone) ? StageHot : StageProspect)
            : stage.Trim().ToLowerInvariant();

        if (!IsValidStage(resolvedStage))
        {
            return Result.Failure<CrmContact>(Error.Validation(
                "CrmContact.InvalidStage",
                $"Giai đoạn khách hàng '{resolvedStage}' không hợp lệ. Chỉ chấp nhận: prospect, warm, hot, customer, lost."));
        }

        var contact = new CrmContact(
            id,
            name?.Trim(),
            phone?.Trim(),
            email?.Trim(),
            telegramChatId?.Trim(),
            telegramUsername?.Trim()?.TrimStart('@'),
            zaloId?.Trim(),
            normalizedChannel,
            resolvedStage,
            tagsJson,
            notes?.Trim(),
            DateTime.UtcNow);

        return Result.Success(contact);
    }

    /// <summary>
    /// Cập nhật các thông tin cơ bản của hồ sơ khách hàng.
    /// </summary>
    /// <param name="name">Họ tên mới.</param>
    /// <param name="phone">Số điện thoại mới.</param>
    /// <param name="email">Email mới.</param>
    /// <param name="stage">Phân hạng mới.</param>
    /// <param name="notes">Ghi chú nhu cầu mới.</param>
    /// <param name="tagsJson">Nhãn mới dạng JSON.</param>
    /// <returns>Result báo trạng thái cập nhật.</returns>
    public Result Update(
        string? name,
        string? phone,
        string? email,
        string? stage,
        string? notes,
        string? tagsJson = null)
    {
        if (!string.IsNullOrWhiteSpace(stage))
        {
            var normalizedStage = stage.Trim().ToLowerInvariant();
            if (!IsValidStage(normalizedStage))
            {
                return Result.Failure(Error.Validation(
                    "CrmContact.InvalidStage",
                    $"Giai đoạn khách hàng '{normalizedStage}' không hợp lệ."));
            }

            Stage = normalizedStage;
        }

        Name = name?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Notes = notes?.Trim();

        if (tagsJson != null)
        {
            TagsJson = tagsJson;
        }

        SetUpdatedAtUtc();
        return Result.Success();
    }

    /// <summary>
    /// Ghi nhận thời điểm tương tác gần nhất của khách hàng.
    /// </summary>
    public void RecordActivity()
    {
        LastActiveAtUtc = DateTime.UtcNow;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Gắn số điện thoại và tự động nâng phân hạng lên Hot Lead nếu khách đang ở giai đoạn Prospect hoặc Warm.
    /// </summary>
    /// <param name="phone">Số điện thoại đã được chuẩn hóa.</param>
    public void AttachPhone(string phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone = phone.Trim();
            if (Stage is StageProspect or StageWarm)
            {
                Stage = StageHot;
            }
            RecordActivity();
        }
    }

    /// <summary>
    /// Đánh dấu khách hàng là Hot Lead (có SĐT, sẵn sàng chuyển cho nhân viên chốt sale).
    /// </summary>
    public void MarkAsHot()
    {
        Stage = StageHot;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Cập nhật giai đoạn trạng thái vòng đời khách hàng (prospect, warm, hot, customer, lost).
    /// </summary>
    /// <param name="stage">Tên giai đoạn mới.</param>
    /// <returns>Result kết quả cập nhật.</returns>
    public Result UpdateStage(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return Result.Failure(Error.Validation("CrmContact.InvalidStage", "Giai đoạn khách hàng không được để trống."));
        }

        var normalizedStage = stage.Trim().ToLowerInvariant();
        if (!IsValidStage(normalizedStage))
        {
            return Result.Failure(Error.Validation("CrmContact.InvalidStage", $"Giai đoạn khách hàng '{normalizedStage}' không hợp lệ."));
        }

        Stage = normalizedStage;
        SetUpdatedAtUtc();
        return Result.Success();
    }

    /// <summary>
    /// Kiểm tra khách hàng có thuộc nhóm Hot Lead hay không.
    /// </summary>
    /// <returns>True nếu stage là hot, ngược lại False.</returns>
    public bool IsHot()
    {
        return Stage == StageHot;
    }

    /// <summary>
    /// Thực hiện xóa mềm hồ sơ khách hàng.
    /// </summary>
    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của chuỗi trạng thái stage.
    /// </summary>
    /// <param name="stage">Tên stage cần kiểm tra.</param>
    /// <returns>True nếu hợp lệ, ngược lại False.</returns>
    public static bool IsValidStage(string stage)
    {
        return stage is StageProspect or StageWarm or StageHot or StageCustomer or StageLost;
    }
}

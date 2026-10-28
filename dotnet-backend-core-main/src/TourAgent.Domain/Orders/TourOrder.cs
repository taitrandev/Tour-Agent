namespace TourAgent.Domain.Orders;

using TourAgent.Domain.Common;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Contacts;
using TourAgent.Domain.Tours;

/// <summary>
/// Thực thể TourOrder đại diện cho một Đơn Hàng / Thỏa Thuận Tour Du Lịch (Deal / Order Pipeline).
/// Quản lý chu trình: Cọc 50% ➔ Làm Visa qua Cty Du Lịch ➔ Thu 50% ➔ Điều phối HDV trước 3 ngày (T-3).
/// </summary>
public sealed class TourOrder : Entity<long>
{
    private TourOrder()
    {
    }

    /// <summary>
    /// Mã đơn hàng duy nhất dạng ORD-YYMM-XXXX.
    /// </summary>
    public string OrderCode { get; private set; } = string.Empty;

    /// <summary>
    /// Khóa ngoại liên kết tới thông tin khách hàng trong CRM.
    /// </summary>
    public long? ContactId { get; private set; }

    /// <summary>
    /// Khóa ngoại liên kết tới Tour du lịch.
    /// </summary>
    public long? TravelId { get; private set; }

    /// <summary>
    /// Khóa ngoại liên kết tới ngày khởi hành cụ thể của tour.
    /// </summary>
    public long? TravelDepartureId { get; private set; }

    /// <summary>
    /// Khóa ngoại liên kết tới phiên chat tư vấn của AI Agent.
    /// </summary>
    public long? ConversationId { get; private set; }

    /// <summary>
    /// Kênh tiếp nhận đơn hàng (telegram, zalo, web, manual).
    /// </summary>
    public string Channel { get; private set; } = "manual";

    /// <summary>
    /// Số lượng khách người lớn.
    /// </summary>
    public int PaxAdults { get; private set; } = 1;

    /// <summary>
    /// Số lượng khách trẻ em.
    /// </summary>
    public int PaxChildren { get; private set; } = 0;

    /// <summary>
    /// Tổng giá trị đơn hàng (VNĐ).
    /// </summary>
    public decimal TotalPrice { get; private set; }

    /// <summary>
    /// Tỷ lệ tiền đặt cọc quy định (mặc định 50%).
    /// </summary>
    public int DepositPercent { get; private set; } = 50;

    /// <summary>
    /// Số tiền đặt cọc đợt 1 (VNĐ).
    /// </summary>
    public decimal DepositAmount { get; private set; }

    /// <summary>
    /// Trạng thái đặt cọc đợt 1 (pending, paid).
    /// </summary>
    public string DepositStatus { get; private set; } = "pending";

    /// <summary>
    /// Thời điểm khách thanh toán tiền cọc.
    /// </summary>
    public DateTime? DepositPaidAt { get; private set; }

    /// <summary>
    /// Số tiền thanh toán đợt 2 còn lại (VNĐ).
    /// </summary>
    public decimal RemainingAmount { get; private set; }

    /// <summary>
    /// Trạng thái thanh toán đợt 2 (pending, paid).
    /// </summary>
    public string RemainingStatus { get; private set; } = "pending";

    /// <summary>
    /// Thời điểm khách thanh toán đợt 2.
    /// </summary>
    public DateTime? RemainingPaidAt { get; private set; }

    /// <summary>
    /// Thông tin tài khoản ngân hàng chuyển khoản.
    /// </summary>
    public string? PaymentAccountNote { get; private set; }

    /// <summary>
    /// Trạng thái tiến trình visa (not_required, awaiting_docs, sent_to_operator, processing, approved, rejected).
    /// </summary>
    public string VisaStatus { get; private set; } = "not_required";

    /// <summary>
    /// Ghi chú tiến trình visa từ công ty du lịch đối tác.
    /// </summary>
    public string? VisaNotes { get; private set; }

    /// <summary>
    /// Tên công ty du lịch đối tác phụ trách land tour / visa.
    /// </summary>
    public string? OperatorName { get; private set; }

    /// <summary>
    /// Mã booking tham chiếu của công ty đối tác.
    /// </summary>
    public string? OperatorBookingRef { get; private set; }

    /// <summary>
    /// Ngày khởi hành chính thức của tour.
    /// </summary>
    public DateOnly? DepartureDate { get; private set; }

    /// <summary>
    /// Họ tên Hướng Dẫn Viên phụ trách dẫn đoàn.
    /// </summary>
    public string? TourGuideName { get; private set; }

    /// <summary>
    /// Số điện thoại liên hệ Hướng Dẫn Viên.
    /// </summary>
    public string? TourGuidePhone { get; private set; }

    /// <summary>
    /// Ghi chú điểm tập trung và dặn dò của Hướng Dẫn Viên.
    /// </summary>
    public string? TourGuideNotes { get; private set; }

    /// <summary>
    /// Thời điểm đã gọi dặn dò khách trước khởi hành 3 ngày (T-3).
    /// </summary>
    public DateTime? PreDepartureNotifiedAt { get; private set; }

    /// <summary>
    /// Trạng thái của đơn hàng trong pipeline (lead_consulting, deposit_pending, visa_processing, final_payment_pending, ready_to_depart, completed, cancelled).
    /// </summary>
    public string Status { get; private set; } = "lead_consulting";

    /// <summary>
    /// Tóm tắt cuộc gọi / yêu cầu của khách từ AI Agent để nhân viên tư vấn nắm bắt trong 30s.
    /// </summary>
    public string? AiSummary { get; private set; }

    /// <summary>
    /// Ghi chú nội bộ cho đơn hàng.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Thời điểm tạo đơn hàng.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Thời điểm cập nhật đơn hàng gần nhất.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Thời điểm xóa mềm đơn hàng.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    // Navigation properties
    public CrmContact? Contact { get; private set; }
    public Tour? Travel { get; private set; }
    public TourDeparture? Departure { get; private set; }
    public Conversation? Conversation { get; private set; }

    /// <summary>
    /// Tạo mới một Đơn Hàng Tour Du Lịch với các ràng buộc nghiệp vụ hợp lệ.
    /// </summary>
    public static Result<TourOrder> Create(
        long? contactId,
        long? travelId,
        long? travelDepartureId,
        long? conversationId,
        string channel,
        int paxAdults,
        int paxChildren,
        decimal totalPrice,
        int depositPercent,
        DateOnly? departureDate,
        string? paymentAccountNote,
        string? aiSummary,
        string? notes)
    {
        if (paxAdults < 1)
        {
            return Result.Failure<TourOrder>(new Error("TourOrder.InvalidPax", "Số lượng người lớn phải từ 1 người trở lên."));
        }

        if (totalPrice < 0)
        {
            return Result.Failure<TourOrder>(new Error("TourOrder.InvalidPrice", "Tổng giá tour không được âm."));
        }

        var validDepositPercent = depositPercent is >= 10 and <= 100 ? depositPercent : 50;
        var depositAmount = Math.Round(totalPrice * validDepositPercent / 100m, 2);
        var remainingAmount = totalPrice - depositAmount;

        var order = new TourOrder
        {
            OrderCode = GenerateOrderCode(),
            ContactId = contactId,
            TravelId = travelId,
            TravelDepartureId = travelDepartureId,
            ConversationId = conversationId,
            Channel = string.IsNullOrWhiteSpace(channel) ? "manual" : channel.Trim().ToLowerInvariant(),
            PaxAdults = paxAdults,
            PaxChildren = Math.Max(0, paxChildren),
            TotalPrice = totalPrice,
            DepositPercent = validDepositPercent,
            DepositAmount = depositAmount,
            DepositStatus = "pending",
            RemainingAmount = remainingAmount,
            RemainingStatus = "pending",
            DepartureDate = departureDate,
            PaymentAccountNote = paymentAccountNote ?? "STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)",
            VisaStatus = travelId.HasValue ? "awaiting_docs" : "not_required",
            Status = "deposit_pending",
            AiSummary = aiSummary,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(order);
    }

    /// <summary>
    /// Tự động sinh mã đơn hàng độc nhất dạng ORD-YYMM-XXXX.
    /// </summary>
    public static string GenerateOrderCode()
    {
        var yearMonth = DateTime.UtcNow.ToString("yyMM");
        var randomSuffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"ORD-{yearMonth}-{randomSuffix}";
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản của đơn hàng (số lượng khách, tổng giá, ngày khởi hành, ghi chú).
    /// </summary>
    public void UpdateDetails(
        int paxAdults,
        int paxChildren,
        decimal totalPrice,
        DateOnly? departureDate,
        string? operatorName,
        string? operatorBookingRef,
        string? paymentAccountNote,
        string? status,
        string? notes)
    {
        if (paxAdults >= 1)
        {
            PaxAdults = paxAdults;
        }

        PaxChildren = Math.Max(0, paxChildren);

        if (totalPrice >= 0)
        {
            TotalPrice = totalPrice;
            DepositAmount = Math.Round(totalPrice * DepositPercent / 100m, 2);
            RemainingAmount = totalPrice - DepositAmount;
        }

        if (departureDate.HasValue)
        {
            DepartureDate = departureDate;
        }

        if (!string.IsNullOrWhiteSpace(operatorName))
        {
            OperatorName = operatorName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(operatorBookingRef))
        {
            OperatorBookingRef = operatorBookingRef.Trim();
        }

        if (!string.IsNullOrWhiteSpace(paymentAccountNote))
        {
            PaymentAccountNote = paymentAccountNote.Trim();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            Status = status.Trim();
        }

        if (notes != null)
        {
            Notes = notes;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Xác nhận khách hàng đã chuyển khoản tiền cọc đợt 1 (50%).
    /// </summary>
    public void MarkDepositPaid(string? accountNote = null)
    {
        DepositStatus = "paid";
        DepositPaidAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(accountNote))
        {
            PaymentAccountNote = accountNote.Trim();
        }

        // Nếu tour yêu cầu visa thì chuyển sang visa_processing, nếu không thì sang final_payment_pending
        if (VisaStatus != "not_required")
        {
            Status = "visa_processing";
            if (VisaStatus == "awaiting_docs" || string.IsNullOrWhiteSpace(VisaStatus))
            {
                VisaStatus = "awaiting_docs";
            }
        }
        else
        {
            Status = "final_payment_pending";
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Xác nhận khách hàng đã hoàn tất thanh toán 50% còn lại (100% giá trị đơn hàng).
    /// </summary>
    public void MarkRemainingPaid()
    {
        RemainingStatus = "paid";
        RemainingPaidAt = DateTime.UtcNow;
        Status = "ready_to_depart";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật trạng thái tiến trình làm hồ sơ Visa từ Đại sứ quán / Đối tác.
    /// </summary>
    public void UpdateVisaStatus(string status, string? notes = null, string? operatorName = null, string? operatorBookingRef = null)
    {
        VisaStatus = status;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            VisaNotes = notes.Trim();
        }

        if (!string.IsNullOrWhiteSpace(operatorName))
        {
            OperatorName = operatorName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(operatorBookingRef))
        {
            OperatorBookingRef = operatorBookingRef.Trim();
        }

        // Nếu visa đã được duyệt (Đậu), tự động chuyển trạng thái đơn hàng sang chờ thu nốt 50%
        if (status == "approved")
        {
            Status = "final_payment_pending";
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật thông tin Hướng Dẫn Viên phụ trách và đánh dấu đã dặn dò khách hàng trước ngày khởi hành 3 ngày (T-3).
    /// </summary>
    public void MarkT3Notified(string? guideName, string? guidePhone, string? guideNotes)
    {
        if (!string.IsNullOrWhiteSpace(guideName))
        {
            TourGuideName = guideName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(guidePhone))
        {
            TourGuidePhone = guidePhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(guideNotes))
        {
            TourGuideNotes = guideNotes.Trim();
        }

        PreDepartureNotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soạn mẫu tin nhắn thông báo chúc mừng đậu Visa và nhắc thu 50% còn lại gửi qua Zalo / Telegram.
    /// </summary>
    public string GetZaloVisaSuccessMessage(string customerName, string tourTitle)
    {
        var safeCustomer = string.IsNullOrWhiteSpace(customerName) ? "Quý khách" : customerName;
        var safeTour = string.IsNullOrWhiteSpace(tourTitle) ? "chương trình du lịch" : tourTitle;
        var dateFormatted = DepartureDate?.ToString("dd/MM/yyyy") ?? "theo lịch thông báo của đoàn";
        var remainingFormatted = $"{RemainingAmount:N0} VNĐ";

        string paymentBlock;
        if (RemainingStatus == "paid")
        {
            paymentBlock = "✅ Tình trạng thanh toán: Anh/chị đã hoàn tất 100% chi phí tour.\n" +
                           "Hiện tại bên em đang hoàn tất các thủ tục xuất vé máy bay và hồ sơ bảo hiểm du lịch quốc tế cho đoàn mình ạ!";
        }
        else
        {
            paymentBlock = $"💰 Số tiền đợt 2 cần thanh toán trước ngày bay: {remainingFormatted}\n" +
                           $"📌 Thông tin STK chuyển khoản: {PaymentAccountNote}\n\n" +
                           "Anh/chị vui lòng hoàn tất chuyển khoản đợt 2 để bên em tiến hành xuất vé máy bay và bảo hiểm du lịch quốc tế nhé ạ. Em cảm ơn anh/chị rất nhiều!";
        }

        return "🎉 TIN VUI TỪ PHÒNG VISA — CHÚC MỪNG ANH/CHỊ!\n\n" +
               $"Kính gửi {safeCustomer},\n" +
               $"Em xin vui mừng thông báo hồ sơ Visa của anh/chị tham gia tour {safeTour} đã chính thức CÓ KẾT QUẢ ĐẬU từ Đại sứ quán/Lãnh sự quán! 🇻🇳✈️\n\n" +
               $"📅 Ngày khởi hành dự kiến: {dateFormatted}\n" +
               $"{paymentBlock}";
    }

    /// <summary>
    /// Soạn mẫu tin nhắn dặn dò trước khởi hành 3 ngày (T-3) kèm thông tin Hướng Dẫn Viên gửi qua Zalo / Telegram.
    /// </summary>
    public string GetZaloT3DepartureMessage(string customerName, string tourTitle)
    {
        var safeCustomer = string.IsNullOrWhiteSpace(customerName) ? "Quý khách" : customerName;
        var safeTour = string.IsNullOrWhiteSpace(tourTitle) ? "chương trình du lịch" : tourTitle;
        var dateFormatted = DepartureDate?.ToString("dd/MM/yyyy") ?? "3 ngày nữa";
        var guide = TourGuideName ?? "Đang cập nhật";
        var phone = TourGuidePhone ?? "Đang cập nhật";
        var meeting = TourGuideNotes ?? "Tập trung tại Cột số 10 - Ga đi Quốc tế sân bay Tân Sơn Nhất / Nội Bài";

        return "✈️ THÔNG TIN CHUYẾN BAY & DẶN DÒ KHỞI HÀNH (T-3)\n\n" +
               $"Kính gửi {safeCustomer},\n" +
               $"Chuyến đi {safeTour} của anh/chị sẽ chính thức khởi hành vào ngày {dateFormatted}.\n\n" +
               $"👤 Hướng dẫn viên phụ trách đoàn: {guide}\n" +
               $"📞 Số điện thoại HDV: {phone}\n" +
               $"📍 Điểm hẹn & Thời gian tập trung: {meeting}\n\n" +
               "🧳 Một số lưu ý quan trọng:\n" +
               "- Mang theo Hộ chiếu gốc (còn hạn trên 6 tháng).\n" +
               "- Kiểm tra hành lý xách tay và ký gửi đúng quy định.\n" +
               "- Hướng dẫn viên sẽ chủ động gọi điện thoại cho anh/chị trước ngày bay để xác nhận điểm đón.\n\n" +
               "Chúc anh/chị có một kỳ nghỉ thật vui vẻ và trọn vẹn!";
    }

    /// <summary>
    /// Thực hiện xóa mềm đơn hàng khỏi hệ thống hoạt động.
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }
}

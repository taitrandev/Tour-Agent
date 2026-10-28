namespace TourAgent.Application.Features.Orders;

using System.Text.Json.Serialization;

/// <summary>
/// DTO tóm tắt thông tin khách hàng gắn với đơn hàng.
/// </summary>
public sealed record OrderContactDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("telegram_username")] string? TelegramUsername,
    [property: JsonPropertyName("channel")] string Channel);

/// <summary>
/// DTO tóm tắt thông tin Tour du lịch gắn với đơn hàng.
/// </summary>
public sealed record OrderTravelDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("price_from")] decimal PriceFrom);

/// <summary>
/// DTO đại diện cho một Đơn Hàng Tour Du Lịch trả về cho giao diện quản trị Admin và Sales.
/// </summary>
public sealed record TourOrderDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("order_code")] string OrderCode,
    [property: JsonPropertyName("contact_id")] long? ContactId,
    [property: JsonPropertyName("contact")] OrderContactDto? Contact,
    [property: JsonPropertyName("travel_id")] long? TravelId,
    [property: JsonPropertyName("travel")] OrderTravelDto? Travel,
    [property: JsonPropertyName("travel_departure_id")] long? TravelDepartureId,
    [property: JsonPropertyName("conversation_id")] long? ConversationId,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("pax_adults")] int PaxAdults,
    [property: JsonPropertyName("pax_children")] int PaxChildren,
    [property: JsonPropertyName("total_price")] decimal TotalPrice,
    [property: JsonPropertyName("deposit_percent")] int DepositPercent,
    [property: JsonPropertyName("deposit_amount")] decimal DepositAmount,
    [property: JsonPropertyName("deposit_status")] string DepositStatus,
    [property: JsonPropertyName("deposit_paid_at")] DateTime? DepositPaidAt,
    [property: JsonPropertyName("remaining_amount")] decimal RemainingAmount,
    [property: JsonPropertyName("remaining_status")] string RemainingStatus,
    [property: JsonPropertyName("remaining_paid_at")] DateTime? RemainingPaidAt,
    [property: JsonPropertyName("payment_account_note")] string? PaymentAccountNote,
    [property: JsonPropertyName("visa_status")] string VisaStatus,
    [property: JsonPropertyName("visa_notes")] string? VisaNotes,
    [property: JsonPropertyName("operator_name")] string? OperatorName,
    [property: JsonPropertyName("operator_booking_ref")] string? OperatorBookingRef,
    [property: JsonPropertyName("departure_date")] string? DepartureDate,
    [property: JsonPropertyName("tour_guide_name")] string? TourGuideName,
    [property: JsonPropertyName("tour_guide_phone")] string? TourGuidePhone,
    [property: JsonPropertyName("tour_guide_notes")] string? TourGuideNotes,
    [property: JsonPropertyName("pre_departure_notified_at")] DateTime? PreDepartureNotifiedAt,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("ai_summary")] string? AiSummary,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt);

/// <summary>
/// DTO chứa các chỉ số thống kê đường ống đơn hàng thời gian thực (Real-time Pipeline Metrics).
/// </summary>
public sealed record OrderMetricsDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("lead_consulting")] int LeadConsulting,
    [property: JsonPropertyName("deposit_pending")] int DepositPending,
    [property: JsonPropertyName("visa_processing")] int VisaProcessing,
    [property: JsonPropertyName("final_payment_pending")] int FinalPaymentPending,
    [property: JsonPropertyName("t3_departure")] int T3Departure);

/// <summary>
/// DTO phản hồi danh sách đơn hàng có phân trang và metrics tương thích chuẩn Laravel API.
/// </summary>
public sealed record OrdersListResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] IReadOnlyList<TourOrderDto> Data,
    [property: JsonPropertyName("pagination")] Contacts.PaginationMetaDto Pagination,
    [property: JsonPropertyName("metrics")] OrderMetricsDto Metrics);

/// <summary>
/// DTO phản hồi cho endpoint lấy mẫu tin nhắn Zalo gửi khách hàng.
/// </summary>
public sealed record ZaloTemplateResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message_content")] string MessageContent);

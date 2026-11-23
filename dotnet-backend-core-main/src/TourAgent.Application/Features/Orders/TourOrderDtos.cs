namespace TourAgent.Application.Features.Orders;

/// <summary>
/// DTO tóm tắt thông tin khách hàng gắn với đơn hàng.
/// </summary>
public sealed record OrderContactDto(
    long Id,
    string? Name,
    string? Phone,
    string? Email,
    string? TelegramUsername,
    string Channel);

/// <summary>
/// DTO tóm tắt thông tin Tour du lịch gắn với đơn hàng.
/// </summary>
public sealed record OrderTravelDto(
    long Id,
    string Title,
    string? Code,
    decimal PriceFrom);

/// <summary>
/// DTO đại diện cho một Đơn Hàng Tour Du Lịch trả về cho giao diện quản trị Admin và Sales.
/// </summary>
public sealed record TourOrderDto(
    long Id,
    string OrderCode,
    long? ContactId,
    OrderContactDto? Contact,
    long? TravelId,
    OrderTravelDto? Travel,
    long? TravelDepartureId,
    long? ConversationId,
    string Channel,
    int PaxAdults,
    int PaxChildren,
    decimal TotalPrice,
    int DepositPercent,
    decimal DepositAmount,
    string DepositStatus,
    DateTime? DepositPaidAt,
    decimal RemainingAmount,
    string RemainingStatus,
    DateTime? RemainingPaidAt,
    string? PaymentAccountNote,
    string VisaStatus,
    string? VisaNotes,
    string? OperatorName,
    string? OperatorBookingRef,
    string? DepartureDate,
    string? TourGuideName,
    string? TourGuidePhone,
    string? TourGuideNotes,
    DateTime? PreDepartureNotifiedAt,
    string Status,
    string? AiSummary,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// DTO chứa các chỉ số thống kê đường ống đơn hàng thời gian thực (Real-time Pipeline Metrics).
/// </summary>
public sealed record OrderMetricsDto(
    int Total,
    int LeadConsulting,
    int DepositPending,
    int VisaProcessing,
    int FinalPaymentPending,
    int T3Departure);

/// <summary>
/// DTO phản hồi danh sách đơn hàng có phân trang và metrics tương thích chuẩn camelCase API.
/// </summary>
public sealed record OrdersListResponseDto(
    bool Success,
    IReadOnlyList<TourOrderDto> Data,
    Contacts.PaginationMetaDto Pagination,
    OrderMetricsDto Metrics);

/// <summary>
/// DTO phản hồi cho endpoint lấy mẫu tin nhắn Zalo gửi khách hàng.
/// </summary>
public sealed record ZaloTemplateResponseDto(
    bool Success,
    string Type,
    string MessageContent);

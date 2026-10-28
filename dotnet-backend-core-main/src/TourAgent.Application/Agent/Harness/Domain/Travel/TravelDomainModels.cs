namespace TourAgent.Application.Agent.Harness.Domain.Travel;

using System.Text.Json.Serialization;

/// <summary>
/// Trạng thái của tour du lịch đang được quan tâm/chốt trong phiên hội thoại.
/// </summary>
public sealed class ActiveTravelState
{
    /// <summary>
    /// Mã định danh của Tour.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Mã code hiển thị của Tour (ví dụ: JP-TOK-01).
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề tên của Tour.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Giá khởi điểm của Tour (VNĐ).
    /// </summary>
    [JsonPropertyName("price_from")]
    public decimal PriceFrom { get; set; }

    /// <summary>
    /// Trạng thái trong máy trạng thái (candidate, proposed, confirmed, rejected).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "candidate";

    /// <summary>
    /// Thời điểm cập nhật trạng thái gần nhất.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tour ứng viên trong danh sách kết quả tìm kiếm gần nhất.
/// </summary>
public sealed class TravelCandidateState
{
    /// <summary>
    /// Mã định danh của Tour.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Mã code hiển thị của Tour.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề tên Tour.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Giá khởi điểm (VNĐ).
    /// </summary>
    [JsonPropertyName("price_from")]
    public decimal PriceFrom { get; set; }

    /// <summary>
    /// Quốc gia của Tour.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Đối tượng trạng thái nghiệp vụ Tour hoàn chỉnh lưu trong domain_state['travel'].
/// </summary>
public sealed class TravelDomainState
{
    /// <summary>
    /// Tour đang được xem xét hoặc chốt cọc (nếu có).
    /// </summary>
    [JsonPropertyName("active_travel")]
    public ActiveTravelState? ActiveTravel { get; set; }

    /// <summary>
    /// Danh sách các tour ứng viên được trả về từ lượt tìm kiếm gần nhất.
    /// </summary>
    [JsonPropertyName("candidates")]
    public List<TravelCandidateState> Candidates { get; set; } = [];

    /// <summary>
    /// Danh sách các tour ID đã bị khách từ chối.
    /// </summary>
    [JsonPropertyName("history")]
    public List<long> RejectedIds { get; set; } = [];
}

/// <summary>
/// DTO chứa thông tin Tour trích xuất qua TourGateway độc lập với ORM Entity.
/// </summary>
/// <param name="Id">Mã định danh Tour.</param>
/// <param name="Code">Mã code Tour.</param>
/// <param name="Title">Tiêu đề Tour.</param>
/// <param name="Country">Quốc gia du lịch.</param>
/// <param name="Destination">Điểm đến chi tiết.</param>
/// <param name="DepartureCity">Thành phố khởi hành.</param>
/// <param name="DurationDays">Số ngày.</param>
/// <param name="DurationNights">Số đêm.</param>
/// <param name="PriceFrom">Giá từ (VNĐ).</param>
/// <param name="Highlights">Điểm nổi bật.</param>
public sealed record TourDto(
    long Id,
    string Code,
    string Title,
    string Country,
    string Destination,
    string DepartureCity,
    int DurationDays,
    int DurationNights,
    decimal PriceFrom,
    string? Highlights
);

/// <summary>
/// Kết quả phân giải Tour 5 tầng (Kỹ thuật D2: Tour Reference Resolution).
/// </summary>
/// <param name="TravelId">Mã Tour xác định được (null nếu không xác định hoặc mập mờ).</param>
/// <param name="Confidence">Điểm tin cậy từ 0.00 đến 1.00.</param>
/// <param name="Method">Phương thức phân giải thành công.</param>
/// <param name="TourTitle">Tiêu đề tour (nếu xác định được).</param>
/// <param name="TourCode">Mã code tour (nếu xác định được).</param>
/// <param name="AuditMetadata">Dữ liệu hộp đen kiểm toán phục vụ quan sát và điều tra.</param>
public sealed record TourResolutionResult(
    long? TravelId,
    decimal Confidence,
    string Method,
    string? TourTitle,
    string? TourCode,
    IReadOnlyDictionary<string, object?> AuditMetadata
)
{
    /// <summary>
    /// Tạo kết quả không tìm thấy tour nào (Tier 5: None).
    /// </summary>
    public static TourResolutionResult None() =>
        new(null, 0.00m, "none", null, null, new Dictionary<string, object?>());

    /// <summary>
    /// Tạo kết quả trường hợp mập mờ có nhiều hơn 1 tour khớp (Tier 4c: Ambiguous - Cấm đoán bừa).
    /// </summary>
    public static TourResolutionResult Ambiguous(IReadOnlyList<long> candidateIds, string reason) =>
        new(null, 0.00m, "ambiguous", null, null, new Dictionary<string, object?>
        {
            ["candidate_ids"] = candidateIds,
            ["reason"] = reason
        });
}

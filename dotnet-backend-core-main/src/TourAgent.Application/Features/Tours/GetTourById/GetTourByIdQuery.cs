using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.GetTourById;

/// <summary>
/// Query yêu cầu lấy thông tin chi tiết đầy đủ của một Tour du lịch theo Id.
/// </summary>
/// <param name="Id">Mã định danh duy nhất của Tour.</param>
public sealed record GetTourByIdQuery(long Id) : IQuery<Result<TourDetailDto>>;

/// <summary>
/// DTO chứa thông tin chi tiết một đợt khởi hành của Tour.
/// </summary>
/// <param name="Id">Mã đợt khởi hành.</param>
/// <param name="DepartureDate">Ngày bay.</param>
/// <param name="PriceAdult">Giá người lớn.</param>
/// <param name="PriceChild">Giá trẻ em.</param>
/// <param name="SingleSupplement">Phụ thu phòng đơn.</param>
/// <param name="Status">Trạng thái (open, guaranteed, full, closed).</param>
/// <param name="Notes">Ghi chú đợt bay.</param>
public sealed record TourDepartureDto(
    long Id,
    string DepartureDate,
    decimal PriceAdult,
    decimal? PriceChild,
    decimal? SingleSupplement,
    string Status,
    string? Notes);

/// <summary>
/// DTO chứa toàn bộ thông tin chi tiết sản phẩm Tour phục vụ xem chi tiết & quản trị.
/// </summary>
public sealed record TourDetailDto(
    long Id,
    string Code,
    string Name,
    string Destination,
    string Country,
    string DepartureCity,
    int DurationDays,
    int DurationNights,
    decimal PriceFrom,
    string Currency,
    string? Highlights,
    string? Inclusions,
    string? Exclusions,
    string? VisaPolicy,
    string? CancellationPolicy,
    string? ChildPolicy,
    string? PaymentPolicy,
    string? Itinerary,
    bool IsActive,
    IReadOnlyList<TourDepartureDto> Departures);

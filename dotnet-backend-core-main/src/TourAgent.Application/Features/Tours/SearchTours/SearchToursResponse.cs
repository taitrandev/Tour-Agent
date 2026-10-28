using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.SearchTours;

/// <summary>
/// DTO chứa thông tin tóm tắt của một Tour trong danh sách kết quả tìm kiếm.
/// </summary>
/// <param name="Id">Mã định danh duy nhất của Tour.</param>
/// <param name="Code">Mã tour chuẩn hóa.</param>
/// <param name="Name">Tên hiển thị của tour.</param>
/// <param name="Destination">Điểm đến tham quan.</param>
/// <param name="Country">Quốc gia du lịch.</param>
/// <param name="DepartureCity">Nơi khởi hành.</param>
/// <param name="DurationDays">Số ngày hành trình.</param>
/// <param name="DurationNights">Số đêm hành trình.</param>
/// <param name="PriceFrom">Giá sàn khởi điểm (VNĐ).</param>
/// <param name="Currency">Đơn vị tiền tệ.</param>
/// <param name="Highlights">Các điểm nổi bật của tour.</param>
/// <param name="NextDepartureDate">Ngày khởi hành gần nhất còn mở bán.</param>
/// <param name="DeparturesCount">Tổng số đợt bay đang mở bán.</param>
public sealed record TourSummaryDto(
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
    DateOnly? NextDepartureDate,
    int DeparturesCount)
{
    /// <summary>
    /// Phương thức chuyển đổi thủ công (Manual Mapping) từ Entity Tour sang TourSummaryDto.
    /// Hoàn toàn không sử dụng AutoMapper, tối ưu hiệu năng và đảm bảo an toàn biên dịch.
    /// </summary>
    /// <param name="tour">Đối tượng Entity Tour.</param>
    /// <returns>Đối tượng TourSummaryDto tương ứng.</returns>
    public static TourSummaryDto FromEntity(Tour tour)
    {
        var openDepartures = tour.Departures
            .Where(d => d.Status == TourDepartureStatus.Open)
            .OrderBy(d => d.DepartureDate)
            .ToList();

        var nextDeparture = openDepartures.FirstOrDefault()?.DepartureDate;

        return new TourSummaryDto(
            tour.Id,
            tour.Code,
            tour.Name,
            tour.Destination,
            tour.Country,
            tour.DepartureCity,
            tour.DurationDays,
            tour.DurationNights,
            tour.PriceFrom,
            tour.Currency,
            tour.Highlights,
            nextDeparture,
            openDepartures.Count);
    }
}

/// <summary>
/// Kết quả phân trang của câu truy vấn tìm kiếm danh sách Tour.
/// </summary>
/// <param name="Items">Danh sách các tour thỏa mãn điều kiện.</param>
/// <param name="TotalCount">Tổng số lượng tour tìm thấy trong hệ thống.</param>
/// <param name="Page">Trang hiện tại.</param>
/// <param name="PageSize">Số lượng phần tử mỗi trang.</param>
public sealed record SearchToursResult(
    IReadOnlyList<TourSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

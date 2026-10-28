using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.SearchTours;

/// <summary>
/// Câu truy vấn tìm kiếm danh sách Tour du lịch theo bộ lọc tiêu chí.
/// </summary>
/// <param name="Destination">Điểm đến cần tìm (ví dụ: 'Tokyo', 'Phú Sĩ').</param>
/// <param name="Country">Quốc gia du lịch (ví dụ: 'Japan', 'Việt Nam').</param>
/// <param name="DepartureCity">Thành phố khởi hành (ví dụ: 'Hà Nội', 'TP.HCM').</param>
/// <param name="MaxPrice">Mức giá trần tối đa khách hàng mong muốn.</param>
/// <param name="Search">Từ khóa tìm kiếm chung theo tên hoặc mã tour.</param>
/// <param name="Page">Trang dữ liệu hiện tại (bắt đầu từ 1).</param>
/// <param name="PageSize">Số lượng tour trên một trang kết quả.</param>
public sealed record SearchToursQuery(
    string? Destination = null,
    string? Country = null,
    string? DepartureCity = null,
    decimal? MaxPrice = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10) : IQuery<Result<SearchToursResult>>;

using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.SearchTours;

/// <summary>
/// Bộ kiểm tra xác thực dữ liệu đầu vào cho câu truy vấn SearchToursQuery.
/// </summary>
public static class SearchToursValidator
{
    /// <summary>
    /// Kiểm tra tính hợp lệ của các tham số phân trang và lọc dữ liệu.
    /// </summary>
    /// <param name="query">Câu truy vấn cần kiểm tra.</param>
    /// <returns>Result thành công nếu dữ liệu hợp lệ, hoặc lỗi xác thực nếu tham số sai phạm.</returns>
    public static Result Validate(SearchToursQuery query)
    {
        if (query.Page < 1)
        {
            return Result.Failure(Error.Validation("SearchTours.InvalidPage", "Số trang (Page) phải từ 1 trở lên."));
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return Result.Failure(Error.Validation("SearchTours.InvalidPageSize", "Kích thước trang (PageSize) phải từ 1 đến 100."));
        }

        if (query.MaxPrice.HasValue && query.MaxPrice.Value <= 0)
        {
            return Result.Failure(Error.Validation("SearchTours.InvalidMaxPrice", "Mức giá trần tìm kiếm (MaxPrice) phải lớn hơn 0."));
        }

        return Result.Success();
    }
}

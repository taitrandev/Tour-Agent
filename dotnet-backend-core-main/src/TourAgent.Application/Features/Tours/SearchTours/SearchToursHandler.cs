using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.SearchTours;

/// <summary>
/// Xử lý logic nghiệp vụ cho câu truy vấn tìm kiếm danh sách Tour du lịch.
/// Lớp được đánh dấu sealed và inject trực tiếp IAppDbContext mà không cần qua tầng Repository riêng.
/// </summary>
public sealed class SearchToursHandler : IQueryHandler<SearchToursQuery, Result<SearchToursResult>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với đối tượng abstraction truy cập cơ sở dữ liệu.
    /// </summary>
    /// <param name="dbContext">Đối tượng IAppDbContext được inject từ DI container.</param>
    public SearchToursHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện tìm kiếm và phân trang danh sách tour dựa trên các tiêu chí lọc.
    /// </summary>
    /// <param name="query">Tham số tìm kiếm và phân trang.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa SearchToursResult nếu thành công, hoặc lỗi nếu tham số không hợp lệ.</returns>
    public async Task<Result<SearchToursResult>> HandleAsync(
        SearchToursQuery query,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra xác thực đầu vào
        var validationResult = SearchToursValidator.Validate(query);
        if (validationResult.IsFailure)
        {
            return Result.Failure<SearchToursResult>(validationResult.Error);
        }

        // 2. Xây dựng truy vấn cơ sở dữ liệu (chỉ lấy tour đang kích hoạt mở bán)
        var toursQuery = _dbContext.Tours
            .Include(t => t.Departures)
            .AsNoTracking()
            .Where(t => t.IsActive);

        // 3. Áp dụng các bộ lọc động
        if (!string.IsNullOrWhiteSpace(query.Destination))
        {
            var destinationKeyword = query.Destination.Trim().ToLower();
            toursQuery = toursQuery.Where(t => t.Destination.ToLower().Contains(destinationKeyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var countryKeyword = query.Country.Trim().ToLower();
            toursQuery = toursQuery.Where(t => t.Country.ToLower().Contains(countryKeyword));
        }

        if (!string.IsNullOrWhiteSpace(query.DepartureCity))
        {
            var cityKeyword = query.DepartureCity.Trim().ToLower();
            toursQuery = toursQuery.Where(t => t.DepartureCity.ToLower().Contains(cityKeyword));
        }

        if (query.MaxPrice.HasValue)
        {
            toursQuery = toursQuery.Where(t => t.PriceFrom <= query.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchKeyword = query.Search.Trim().ToLower();
            toursQuery = toursQuery.Where(t =>
                t.Name.ToLower().Contains(searchKeyword) ||
                t.Code.ToLower().Contains(searchKeyword) ||
                t.Destination.ToLower().Contains(searchKeyword));
        }

        // 4. Đếm tổng số lượng bản ghi thỏa mãn điều kiện
        var totalCount = await toursQuery.CountAsync(cancellationToken);

        // 5. Thực hiện phân trang và tải danh sách thực thể
        var tours = await toursQuery
            .OrderBy(t => t.PriceFrom)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // 6. Ánh xạ thủ công sang DTO (Manual Projection)
        var tourDtos = tours
            .Select(TourSummaryDto.FromEntity)
            .ToList();

        var searchResult = new SearchToursResult(
            tourDtos,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(searchResult);
    }
}

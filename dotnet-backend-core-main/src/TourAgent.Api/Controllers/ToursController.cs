using Microsoft.AspNetCore.Mvc;
using TourAgent.Api.Extensions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.SearchTours;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller quản lý và tra cứu thông tin Sản phẩm Tour du lịch.
/// </summary>
[ApiController]
[Route("api/v1/tours")]
[Produces("application/json")]
public class ToursController : ControllerBase
{
    private readonly IQueryHandler<SearchToursQuery, Result<SearchToursResult>> _searchToursHandler;

    /// <summary>
    /// Khởi tạo ToursController với các handler tương ứng được inject từ DI container.
    /// </summary>
    /// <param name="searchToursHandler">Handler xử lý nghiệp vụ tìm kiếm và phân trang tour.</param>
    public ToursController(IQueryHandler<SearchToursQuery, Result<SearchToursResult>> searchToursHandler)
    {
        _searchToursHandler = searchToursHandler;
    }

    /// <summary>
    /// Tra cứu và lọc danh sách tour du lịch theo các tiêu chí (Điểm đến, Quốc gia, Thành phố khởi hành, Giá trần, Từ khóa).
    /// </summary>
    /// <param name="query">Tham số tìm kiếm và phân trang từ query string.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách các tour thỏa mãn điều kiện lọc và thông tin phân trang.</returns>
    /// <response code="200">Tìm kiếm thành công, trả về danh sách tour.</response>
    /// <response code="400">Tham số tìm kiếm hoặc phân trang không hợp lệ.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SearchToursResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchTours(
        [FromQuery] SearchToursQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _searchToursHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult();
    }
}

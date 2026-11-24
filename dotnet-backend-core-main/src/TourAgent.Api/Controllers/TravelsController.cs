using Microsoft.AspNetCore.Mvc;
using TourAgent.Api.Extensions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.CreateTour;
using TourAgent.Application.Features.Tours.DeleteTour;
using TourAgent.Application.Features.Tours.Faqs.CreateTourFaq;
using TourAgent.Application.Features.Tours.Faqs.DeleteTourFaq;
using TourAgent.Application.Features.Tours.Faqs.GetTourFaqs;
using TourAgent.Application.Features.Tours.Faqs.UpdateTourFaqVisibility;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Application.Features.Tours.GetTourBySlug;
using TourAgent.Application.Features.Tours.Reviews.CreateTourReview;
using TourAgent.Application.Features.Tours.Reviews.DeleteTourReview;
using TourAgent.Application.Features.Tours.Reviews.GetTourReviews;
using TourAgent.Application.Features.Tours.Reviews.UpdateTourReviewVisibility;
using TourAgent.Application.Features.Tours.SearchTours;
using TourAgent.Application.Features.Tours.UpdateTour;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Controllers;

/// <summary>
/// DTO chứa thông tin thêm đánh giá khách hàng (Review) cho Tour.
/// </summary>
public sealed record CreateReviewRequest(
    string ReviewerName,
    string? TravelDate,
    int Rating,
    string Content,
    bool IsVisible);

/// <summary>
/// DTO cập nhật trạng thái hiển thị review.
/// </summary>
public sealed record UpdateReviewVisibilityRequest(bool IsVisible);

/// <summary>
/// DTO chứa thông tin thêm câu hỏi thường gặp (FAQ) cho Tour.
/// </summary>
public sealed record CreateFaqRequest(
    string Question,
    string Answer,
    int SortOrder,
    bool IsVisible);

/// <summary>
/// DTO cập nhật trạng thái hiển thị FAQ.
/// </summary>
public sealed record UpdateFaqVisibilityRequest(bool IsVisible);

/// <summary>
/// Controller quản lý danh mục Sản Phẩm Tour Du Lịch (Product Catalog &amp; CRUD).
/// Cung cấp đặc tả REST API cho các màn hình quản trị và tìm kiếm client.
/// </summary>
[ApiController]
[Produces("application/json")]
public class TravelsController : ControllerBase
{
    private readonly IQueryHandler<GetAllToursQuery, Result<IReadOnlyList<TourAdminItemDto>>> _getAllToursHandler;
    private readonly IQueryHandler<SearchToursQuery, Result<SearchToursResult>> _searchToursHandler;
    private readonly IQueryHandler<GetTourByIdQuery, Result<TourDetailDto>> _getTourByIdHandler;
    private readonly IQueryHandler<GetTourBySlugQuery, Result<TourDetailDto>> _getTourBySlugHandler;
    private readonly ICommandHandler<CreateTourCommand, Result<TourAdminItemDto>> _createTourHandler;
    private readonly ICommandHandler<UpdateTourCommand, Result<TourAdminItemDto>> _updateTourHandler;
    private readonly ICommandHandler<DeleteTourCommand, Result> _deleteTourHandler;
    private readonly IQueryHandler<GetTourReviewsQuery, Result<IReadOnlyList<TourReviewDto>>> _getTourReviewsHandler;
    private readonly ICommandHandler<CreateTourReviewCommand, Result<long>> _createTourReviewHandler;
    private readonly ICommandHandler<UpdateTourReviewVisibilityCommand, Result> _updateTourReviewVisibilityHandler;
    private readonly ICommandHandler<DeleteTourReviewCommand, Result> _deleteTourReviewHandler;
    private readonly IQueryHandler<GetTourFaqsQuery, Result<IReadOnlyList<TourFaqDto>>> _getTourFaqsHandler;
    private readonly ICommandHandler<CreateTourFaqCommand, Result<long>> _createTourFaqHandler;
    private readonly ICommandHandler<UpdateTourFaqVisibilityCommand, Result> _updateTourFaqVisibilityHandler;
    private readonly ICommandHandler<DeleteTourFaqCommand, Result> _deleteTourFaqHandler;

    /// <summary>
    /// Khởi tạo TravelsController với các Handler tương ứng theo chuẩn Clean Architecture.
    /// </summary>
    public TravelsController(
        IQueryHandler<GetAllToursQuery, Result<IReadOnlyList<TourAdminItemDto>>> getAllToursHandler,
        IQueryHandler<SearchToursQuery, Result<SearchToursResult>> searchToursHandler,
        IQueryHandler<GetTourByIdQuery, Result<TourDetailDto>> getTourByIdHandler,
        IQueryHandler<GetTourBySlugQuery, Result<TourDetailDto>> getTourBySlugHandler,
        ICommandHandler<CreateTourCommand, Result<TourAdminItemDto>> createTourHandler,
        ICommandHandler<UpdateTourCommand, Result<TourAdminItemDto>> updateTourHandler,
        ICommandHandler<DeleteTourCommand, Result> deleteTourHandler,
        IQueryHandler<GetTourReviewsQuery, Result<IReadOnlyList<TourReviewDto>>> getTourReviewsHandler,
        ICommandHandler<CreateTourReviewCommand, Result<long>> createTourReviewHandler,
        ICommandHandler<UpdateTourReviewVisibilityCommand, Result> updateTourReviewVisibilityHandler,
        ICommandHandler<DeleteTourReviewCommand, Result> deleteTourReviewHandler,
        IQueryHandler<GetTourFaqsQuery, Result<IReadOnlyList<TourFaqDto>>> getTourFaqsHandler,
        ICommandHandler<CreateTourFaqCommand, Result<long>> createTourFaqHandler,
        ICommandHandler<UpdateTourFaqVisibilityCommand, Result> updateTourFaqVisibilityHandler,
        ICommandHandler<DeleteTourFaqCommand, Result> deleteTourFaqHandler)
    {
        _getAllToursHandler = getAllToursHandler;
        _searchToursHandler = searchToursHandler;
        _getTourByIdHandler = getTourByIdHandler;
        _getTourBySlugHandler = getTourBySlugHandler;
        _createTourHandler = createTourHandler;
        _updateTourHandler = updateTourHandler;
        _deleteTourHandler = deleteTourHandler;
        _getTourReviewsHandler = getTourReviewsHandler;
        _createTourReviewHandler = createTourReviewHandler;
        _updateTourReviewVisibilityHandler = updateTourReviewVisibilityHandler;
        _deleteTourReviewHandler = deleteTourReviewHandler;
        _getTourFaqsHandler = getTourFaqsHandler;
        _createTourFaqHandler = createTourFaqHandler;
        _updateTourFaqVisibilityHandler = updateTourFaqVisibilityHandler;
        _deleteTourFaqHandler = deleteTourFaqHandler;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các Tour du lịch phục vụ bảng Product Catalog trên Admin và Landing Page.
    /// </summary>
    /// <param name="query">Đối tượng query chứa từ khóa tìm kiếm và trạng thái lọc.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    [HttpGet("api/v1/travels")]
    public async Task<IActionResult> GetAllTravels(
        [FromQuery] GetAllToursQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _getAllToursHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Tra cứu và lọc danh sách tour du lịch nâng cao theo tiêu chí (Điểm đến, Quốc gia, Thành phố khởi hành, Giá trần, Phân trang).
    /// </summary>
    /// <param name="query">Đối tượng query chứa các tiêu chí lọc tìm kiếm và phân trang.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    [HttpGet("api/v1/travels/search")]
    [ProducesResponseType(typeof(SearchToursResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTours(
        [FromQuery] SearchToursQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _searchToursHandler.HandleAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một Tour cụ thể theo Id (kèm lịch trình, chính sách, ngày khởi hành).
    /// </summary>
    [HttpGet("api/v1/travels/{id:long}")]
    public async Task<IActionResult> GetTravelById(
        long id,
        CancellationToken cancellationToken)
    {
        var query = new GetTourByIdQuery(id);
        var result = await _getTourByIdHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một Tour cụ thể theo URL Slug hoặc Mã Code duy nhất.
    /// </summary>
    /// <param name="slug">URL Slug hoặc Mã Code của Tour.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết Tour kèm ngày bay và lịch trình.</returns>
    [HttpGet("api/v1/travels/slug/{slug}")]
    public async Task<IActionResult> GetTravelBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var query = new GetTourBySlugQuery(slug);
        var result = await _getTourBySlugHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Tạo mới một Tour du lịch thủ công cùng các đợt khởi hành và lịch trình từng ngày.
    /// </summary>
    [HttpPost("api/v1/travels")]
    public async Task<IActionResult> CreateTravel(
        [FromBody] CreateTourCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _createTourHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Tạo Tour mới thành công!",
            data = result.Value
        });
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết một Tour du lịch và cập nhật các đợt khởi hành.
    /// </summary>
    [HttpPut("api/v1/travels/{id:long}")]
    public async Task<IActionResult> UpdateTravel(
        long id,
        [FromBody] UpdateTourCommand bodyCommand,
        CancellationToken cancellationToken)
    {
        var command = bodyCommand with { Id = id };
        var result = await _updateTourHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật Tour thành công!",
            data = result.Value
        });
    }

    /// <summary>
    /// Xóa một Tour du lịch khỏi hệ thống.
    /// </summary>
    [HttpDelete("api/v1/travels/{id:long}")]
    public async Task<IActionResult> DeleteTravel(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTourCommand(id);
        var result = await _deleteTourHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Đã xóa Tour thành công!"
        });
    }

    /// <summary>
    /// Lấy danh sách đánh giá của khách hàng (Reviews) cho một Tour cụ thể.
    /// </summary>
    [HttpGet("api/v1/travels/{travelId:long}/reviews")]
    public async Task<IActionResult> GetTravelReviews(
        long travelId,
        CancellationToken cancellationToken)
    {
        var query = new GetTourReviewsQuery(travelId);
        var result = await _getTourReviewsHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Thêm đánh giá mới cho một Tour du lịch.
    /// </summary>
    [HttpPost("api/v1/travels/{travelId:long}/reviews")]
    public async Task<IActionResult> CreateTravelReview(
        long travelId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTourReviewCommand(
            travelId,
            request.ReviewerName,
            request.TravelDate,
            request.Rating,
            request.Content,
            request.IsVisible);

        var result = await _createTourReviewHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Thêm đánh giá thành công!",
            data = new { id = result.Value }
        });
    }

    /// <summary>
    /// Ẩn / Hiện một đánh giá của khách hàng.
    /// </summary>
    [HttpPut("api/v1/travel-reviews/{id:long}")]
    public async Task<IActionResult> UpdateReviewVisibility(
        long id,
        [FromBody] UpdateReviewVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTourReviewVisibilityCommand(id, request.IsVisible);
        var result = await _updateTourReviewVisibilityHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = "Cập nhật trạng thái hiển thị thành công!" });
    }

    /// <summary>
    /// Xóa một đánh giá của khách hàng.
    /// </summary>
    [HttpDelete("api/v1/travel-reviews/{id:long}")]
    public async Task<IActionResult> DeleteReview(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTourReviewCommand(id);
        var result = await _deleteTourReviewHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = "Đã xóa đánh giá thành công!" });
    }

    /// <summary>
    /// Lấy danh sách câu hỏi thường gặp (FAQs) cho một Tour cụ thể.
    /// </summary>
    [HttpGet("api/v1/travels/{travelId:long}/faqs")]
    public async Task<IActionResult> GetTravelFaqs(
        long travelId,
        CancellationToken cancellationToken)
    {
        var query = new GetTourFaqsQuery(travelId);
        var result = await _getTourFaqsHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Thêm câu hỏi thường gặp (FAQ) mới cho một Tour du lịch.
    /// </summary>
    [HttpPost("api/v1/travels/{travelId:long}/faqs")]
    public async Task<IActionResult> CreateTravelFaq(
        long travelId,
        [FromBody] CreateFaqRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTourFaqCommand(
            travelId,
            request.Question,
            request.Answer,
            request.SortOrder,
            request.IsVisible);

        var result = await _createTourFaqHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Thêm FAQ thành công!",
            data = new { id = result.Value }
        });
    }

    /// <summary>
    /// Ẩn / Hiện một câu hỏi thường gặp.
    /// </summary>
    [HttpPut("api/v1/travel-faqs/{id:long}")]
    public async Task<IActionResult> UpdateFaqVisibility(
        long id,
        [FromBody] UpdateFaqVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTourFaqVisibilityCommand(id, request.IsVisible);
        var result = await _updateTourFaqVisibilityHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = "Cập nhật FAQ thành công!" });
    }

    /// <summary>
    /// Xóa một câu hỏi thường gặp.
    /// </summary>
    [HttpDelete("api/v1/travel-faqs/{id:long}")]
    public async Task<IActionResult> DeleteFaq(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTourFaqCommand(id);
        var result = await _deleteTourFaqHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = "Đã xóa FAQ thành công!" });
    }
}

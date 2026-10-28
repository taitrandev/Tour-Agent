using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.CreateTour;
using TourAgent.Application.Features.Tours.DeleteTour;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Application.Features.Tours.GetTourById;
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
/// Controller quản lý danh mục Sản Phẩm Tour Du Lịch (Product Catalog & CRUD)
/// Tương thích 100% với đặc tả REST API và các màn hình quản trị của PHP core.
/// </summary>
[ApiController]
[Produces("application/json")]
public class TravelsController : ControllerBase
{
    private readonly IQueryHandler<GetAllToursQuery, Result<IReadOnlyList<TourAdminItemDto>>> _getAllToursHandler;
    private readonly IQueryHandler<GetTourByIdQuery, Result<TourDetailDto>> _getTourByIdHandler;
    private readonly ICommandHandler<CreateTourCommand, Result<TourAdminItemDto>> _createTourHandler;
    private readonly ICommandHandler<UpdateTourCommand, Result<TourAdminItemDto>> _updateTourHandler;
    private readonly ICommandHandler<DeleteTourCommand, Result> _deleteTourHandler;
    private readonly string _connectionString;

    /// <summary>
    /// Khởi tạo TravelsController với các Handler tương ứng và chuỗi kết nối PostgreSQL.
    /// </summary>
    public TravelsController(
        IQueryHandler<GetAllToursQuery, Result<IReadOnlyList<TourAdminItemDto>>> getAllToursHandler,
        IQueryHandler<GetTourByIdQuery, Result<TourDetailDto>> getTourByIdHandler,
        ICommandHandler<CreateTourCommand, Result<TourAdminItemDto>> createTourHandler,
        ICommandHandler<UpdateTourCommand, Result<TourAdminItemDto>> updateTourHandler,
        ICommandHandler<DeleteTourCommand, Result> deleteTourHandler,
        IConfiguration configuration)
    {
        _getAllToursHandler = getAllToursHandler;
        _getTourByIdHandler = getTourByIdHandler;
        _createTourHandler = createTourHandler;
        _updateTourHandler = updateTourHandler;
        _deleteTourHandler = deleteTourHandler;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các Tour du lịch phục vụ bảng Product Catalog trên Admin và Landing Page.
    /// </summary>
    [HttpGet("api/v1/travels")]
    [HttpGet("api/v1/tours/catalog")]
    public async Task<IActionResult> GetAllTravels(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new GetAllToursQuery(search, status);
        var result = await _getAllToursHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
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
        var reviews = new List<object>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "SELECT id, travel_id, reviewer_name, reviewer_avatar_url, rating, content, travel_date, is_visible, created_at FROM travel_reviews WHERE travel_id = @travel_id ORDER BY id DESC";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@travel_id", travelId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                reviews.Add(new
                {
                    id = reader.GetInt64(0),
                    travel_id = reader.GetInt64(1),
                    reviewer_name = reader.GetString(2),
                    reviewer_avatar_url = reader.IsDBNull(3) ? null : reader.GetString(3),
                    rating = reader.GetInt16(4),
                    content = reader.GetString(5),
                    travel_date = reader.IsDBNull(6) ? null : reader.GetString(6),
                    is_visible = reader.GetBoolean(7),
                    created_at = reader.GetDateTime(8).ToString("o")
                });
            }
        }
        catch
        {
            // Trả về danh sách rỗng nếu bảng chưa tồn tại hoặc lỗi kết nối
        }

        return Ok(new { success = true, data = reviews });
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
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = @"INSERT INTO travel_reviews (travel_id, reviewer_name, rating, content, travel_date, is_visible, created_at, updated_at) 
                        VALUES (@travel_id, @reviewer_name, @rating, @content, @travel_date, @is_visible, NOW(), NOW()) RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@travel_id", travelId);
            cmd.Parameters.AddWithValue("@reviewer_name", request.ReviewerName);
            cmd.Parameters.AddWithValue("@rating", (short)request.Rating);
            cmd.Parameters.AddWithValue("@content", request.Content);
            cmd.Parameters.AddWithValue("@travel_date", (object?)request.TravelDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_visible", request.IsVisible);

            var newId = await cmd.ExecuteScalarAsync(cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message = "Thêm đánh giá thành công!",
                data = new { id = newId }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi lưu đánh giá: " + ex.Message });
        }
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
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "UPDATE travel_reviews SET is_visible = @is_visible, updated_at = NOW() WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@is_visible", request.IsVisible);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Ok(new { success = true, message = "Cập nhật trạng thái hiển thị thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa một đánh giá của khách hàng.
    /// </summary>
    [HttpDelete("api/v1/travel-reviews/{id:long}")]
    public async Task<IActionResult> DeleteReview(
        long id,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "DELETE FROM travel_reviews WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Ok(new { success = true, message = "Đã xóa đánh giá thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách câu hỏi thường gặp (FAQs) cho một Tour cụ thể.
    /// </summary>
    [HttpGet("api/v1/travels/{travelId:long}/faqs")]
    public async Task<IActionResult> GetTravelFaqs(
        long travelId,
        CancellationToken cancellationToken)
    {
        var faqs = new List<object>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "SELECT id, travel_id, question, answer, sort_order, is_visible, created_at FROM travel_faqs WHERE travel_id = @travel_id ORDER BY sort_order ASC, id ASC";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@travel_id", travelId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                faqs.Add(new
                {
                    id = reader.GetInt64(0),
                    travel_id = reader.GetInt64(1),
                    question = reader.GetString(2),
                    answer = reader.GetString(3),
                    sort_order = reader.GetInt32(4),
                    is_visible = reader.GetBoolean(5),
                    created_at = reader.GetDateTime(6).ToString("o")
                });
            }
        }
        catch
        {
            // Trả về mảng rỗng nếu bảng chưa tồn tại
        }

        return Ok(new { success = true, data = faqs });
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
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = @"INSERT INTO travel_faqs (travel_id, question, answer, sort_order, is_visible, created_at, updated_at) 
                        VALUES (@travel_id, @question, @answer, @sort_order, @is_visible, NOW(), NOW()) RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@travel_id", travelId);
            cmd.Parameters.AddWithValue("@question", request.Question);
            cmd.Parameters.AddWithValue("@answer", request.Answer);
            cmd.Parameters.AddWithValue("@sort_order", request.SortOrder);
            cmd.Parameters.AddWithValue("@is_visible", request.IsVisible);

            var newId = await cmd.ExecuteScalarAsync(cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message = "Thêm FAQ thành công!",
                data = new { id = newId }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi lưu FAQ: " + ex.Message });
        }
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
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "UPDATE travel_faqs SET is_visible = @is_visible, updated_at = NOW() WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@is_visible", request.IsVisible);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Ok(new { success = true, message = "Cập nhật FAQ thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa một câu hỏi thường gặp.
    /// </summary>
    [HttpDelete("api/v1/travel-faqs/{id:long}")]
    public async Task<IActionResult> DeleteFaq(
        long id,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = "DELETE FROM travel_faqs WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Ok(new { success = true, message = "Đã xóa FAQ thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
        }
    }
}

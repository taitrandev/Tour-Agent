using Microsoft.AspNetCore.Mvc;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Extensions;

/// <summary>
/// Các phương thức mở rộng hỗ trợ chuyển đổi từ Domain Result sang HTTP ActionResult.
/// Giúp loại bỏ các câu lệnh if-else lặp lại trong Controller và chuẩn hóa phản hồi lỗi.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Chuyển đổi một Result generic chứa giá trị thành ActionResult tương ứng.
    /// Nếu thành công: trả về Ok(200) kèm dữ liệu.
    /// Nếu thất bại: tự động ánh xạ Error thành mã HTTP lỗi phù hợp (400, 404, 409).
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của kết quả trả về khi thành công.</typeparam>
    /// <param name="result">Đối tượng Result cần chuyển đổi.</param>
    /// <returns>IActionResult phản hồi cho HTTP client.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToProblemDetails(result.Error);
    }

    /// <summary>
    /// Chuyển đổi một Result không có giá trị trả về thành ActionResult tương ứng.
    /// Nếu thành công: trả về NoContent(204).
    /// Nếu thất bại: tự động ánh xạ Error thành mã HTTP lỗi phù hợp.
    /// </summary>
    /// <param name="result">Đối tượng Result cần chuyển đổi.</param>
    /// <returns>IActionResult phản hồi cho HTTP client.</returns>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToProblemDetails(result.Error);
    }

    /// <summary>
    /// Ánh xạ đối tượng Error thành ProblemDetails kèm theo HTTP status code tương ứng.
    /// </summary>
    /// <param name="error">Đối tượng lỗi Domain cần ánh xạ.</param>
    /// <returns>ObjectResult chứa cấu trúc ProblemDetails.</returns>
    private static IActionResult ToProblemDetails(Error error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
            _ when error.Code.EndsWith(".Conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
            _ when error.Code.EndsWith(".Unauthorized", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status401Unauthorized,
            _ when error.Code.EndsWith(".Forbidden", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = error.Description,
            Extensions =
            {
                ["errorCode"] = error.Code
            }
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Lấy tiêu đề mặc định theo mã HTTP status code.
    /// </summary>
    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Resource Not Found",
        StatusCodes.Status409Conflict => "Conflict Detected",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        _ => "Bad Request / Validation Error"
    };
}

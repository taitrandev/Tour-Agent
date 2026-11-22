using System.Net;
using System.Text.Json;

namespace TourAgent.Api.Middlewares;

/// <summary>
/// Middleware toàn cục xử lý tập trung mọi ngoại lệ chưa được xử lý trong ứng dụng (Global Exception Handler Middleware).
/// Đảm bảo hệ thống luôn trả về JSON chuẩn camelCase có cấu trúc rõ ràng, chấm dứt hoàn toàn tình trạng văng Plain Text / HTML Stack Trace.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Khởi tạo GlobalExceptionMiddleware với RequestDelegate và Logger.
    /// </summary>
    /// <param name="next">Hàm ủy thác đại diện cho middleware tiếp theo trong pipeline.</param>
    /// <param name="logger">Dịch vụ ghi log của hệ thống.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Thực thi middleware trong chuỗi pipeline HTTP.
    /// </summary>
    /// <param name="context">Ngữ cảnh HTTP hiện thời.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ chưa xử lý tại đường dẫn {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Đóng gói ngoại lệ thành cấu trúc JSON chuẩn REST và ghi trực tiếp vào HTTP Response.
    /// </summary>
    /// <param name="context">Ngữ cảnh HTTP hiện thời.</param>
    /// <param name="exception">Ngoại lệ vừa xảy ra.</param>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        int statusCode = (int)HttpStatusCode.InternalServerError;
        string userFriendlyMessage;

        // Phân loại ngoại lệ đặc thù từ cổng kết nối mạng và các mô hình LLM Gateway (Claude, DeepSeek)
        if (exception is HttpRequestException httpEx)
        {
            var isOverloaded = httpEx.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase) ||
                               httpEx.Message.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase) ||
                               httpEx.StatusCode == HttpStatusCode.ServiceUnavailable;

            var isRateLimited = httpEx.Message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
                                httpEx.StatusCode == (HttpStatusCode)429;

            if (isOverloaded)
            {
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                userFriendlyMessage = "Mô hình AI hiện đang quá tải từ phía nhà cung cấp (503 Overloaded). Vui lòng gửi lại yêu cầu sau giây lát hoặc chuyển sang mô hình khác trong Cài đặt LLM.";
            }
            else if (isRateLimited)
            {
                statusCode = (int)HttpStatusCode.TooManyRequests;
                userFriendlyMessage = "Đã vượt hạn mức gọi API mô hình AI (429 Rate Limit). Vui lòng thử lại sau ít phút.";
            }
            else
            {
                statusCode = (int)HttpStatusCode.BadGateway;
                userFriendlyMessage = $"Sự cố kết nối cổng AI Gateway: {httpEx.Message}";
            }
        }
        else if (exception is TaskCanceledException or OperationCanceledException)
        {
            statusCode = (int)HttpStatusCode.RequestTimeout;
            userFriendlyMessage = "Yêu cầu xử lý đã quá thời gian chờ (Request Timeout).";
        }
        else
        {
            userFriendlyMessage = exception.Message;
        }

        context.Response.StatusCode = statusCode;

        var responsePayload = new
        {
            success = false,
            error = userFriendlyMessage,
            statusCode = statusCode
        };

        var json = JsonSerializer.Serialize(responsePayload, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}


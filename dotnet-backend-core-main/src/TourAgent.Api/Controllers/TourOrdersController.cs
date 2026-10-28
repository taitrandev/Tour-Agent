namespace TourAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Orders;
using TourAgent.Domain.Common;

/// <summary>
/// Controller quản lý Đơn Hàng Tour Du Lịch & Đường Ống Visa (Order & Visa Pipeline Desk).
/// Cung cấp đầy đủ các API xem, tìm kiếm, lọc theo pipeline status, tạo mới, cập nhật cọc 50%,
/// cập nhật tiến trình Visa đối tác, xác nhận thu nốt 50%, dặn dò HDV T-3 và xóa mềm.
/// Hỗ trợ cả 2 định dạng route: chuẩn admin (/api/v1/admin/orders) và chuẩn RESTful (/api/v1/orders).
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class TourOrdersController : ControllerBase
{
    private readonly IQueryHandler<GetOrdersQuery, Result<OrdersListResponseDto>> _getOrdersHandler;
    private readonly IQueryHandler<GetOrderByIdQuery, Result<TourOrderDto>> _getOrderByIdHandler;
    private readonly ICommandHandler<CreateTourOrderCommand, Result<TourOrderDto>> _createOrderHandler;
    private readonly ICommandHandler<UpdateTourOrderCommand, Result<TourOrderDto>> _updateOrderHandler;
    private readonly ICommandHandler<MarkDepositCommand, Result<TourOrderDto>> _markDepositHandler;
    private readonly ICommandHandler<UpdateVisaStatusCommand, Result<UpdateVisaResult>> _updateVisaHandler;
    private readonly ICommandHandler<MarkFinalPaymentCommand, Result<TourOrderDto>> _markFinalPaymentHandler;
    private readonly ICommandHandler<MarkT3NotifiedCommand, Result<MarkT3NotifiedResult>> _markT3NotifiedHandler;
    private readonly ICommandHandler<DeleteTourOrderCommand, Result> _deleteOrderHandler;
    private readonly IQueryHandler<GetZaloTemplateQuery, Result<ZaloTemplateResponseDto>> _getZaloTemplateHandler;

    /// <summary>
    /// Khởi tạo TourOrdersController với các handlers từ DI container.
    /// </summary>
    public TourOrdersController(
        IQueryHandler<GetOrdersQuery, Result<OrdersListResponseDto>> getOrdersHandler,
        IQueryHandler<GetOrderByIdQuery, Result<TourOrderDto>> getOrderByIdHandler,
        ICommandHandler<CreateTourOrderCommand, Result<TourOrderDto>> createOrderHandler,
        ICommandHandler<UpdateTourOrderCommand, Result<TourOrderDto>> updateOrderHandler,
        ICommandHandler<MarkDepositCommand, Result<TourOrderDto>> markDepositHandler,
        ICommandHandler<UpdateVisaStatusCommand, Result<UpdateVisaResult>> updateVisaHandler,
        ICommandHandler<MarkFinalPaymentCommand, Result<TourOrderDto>> markFinalPaymentHandler,
        ICommandHandler<MarkT3NotifiedCommand, Result<MarkT3NotifiedResult>> markT3NotifiedHandler,
        ICommandHandler<DeleteTourOrderCommand, Result> deleteOrderHandler,
        IQueryHandler<GetZaloTemplateQuery, Result<ZaloTemplateResponseDto>> getZaloTemplateHandler)
    {
        _getOrdersHandler = getOrdersHandler;
        _getOrderByIdHandler = getOrderByIdHandler;
        _createOrderHandler = createOrderHandler;
        _updateOrderHandler = updateOrderHandler;
        _markDepositHandler = markDepositHandler;
        _updateVisaHandler = updateVisaHandler;
        _markFinalPaymentHandler = markFinalPaymentHandler;
        _markT3NotifiedHandler = markT3NotifiedHandler;
        _deleteOrderHandler = deleteOrderHandler;
        _getZaloTemplateHandler = getZaloTemplateHandler;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng tour kèm bộ lọc theo Pipeline Status, Từ khóa tìm kiếm và Phân trang.
    /// </summary>
    /// <param name="status">Lọc theo trạng thái: lead_consulting, deposit_pending, visa_processing, final_payment_pending, t3_departure, ready_to_depart.</param>
    /// <param name="search">Tìm kiếm theo Mã đơn, Tên khách, Số điện thoại hoặc Tên tour.</param>
    /// <param name="page">Số trang hiện tại (mặc định 1).</param>
    /// <param name="per_page">Số lượng bản ghi mỗi trang (mặc định 50).</param>
    [HttpGet("api/v1/admin/orders")]
    [HttpGet("api/v1/orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrdersQuery(
            Status: status,
            Search: search,
            Page: page,
            PerPage: per_page);

        var result = await _getOrdersHandler.HandleAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Xem thông tin chi tiết một đơn hàng tour theo mã định danh ID.
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    [HttpGet("api/v1/admin/orders/{id:long}")]
    [HttpGet("api/v1/orders/{id:long}")]
    public async Task<IActionResult> GetOrderById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Tạo mới một đơn hàng tour thủ công (Manual Booking từ Sales).
    /// </summary>
    /// <param name="command">Thông tin khởi tạo đơn hàng.</param>
    [HttpPost("api/v1/admin/orders")]
    [HttpPost("api/v1/orders")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateTourOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _createOrderHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = result.Value.Id },
            new { success = true, message = "Tạo đơn hàng tour thành công.", data = result.Value });
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của đơn hàng tour (Số khách, tổng tiền, ngày bay, đối tác).
    /// </summary>
    /// <param name="id">ID đơn hàng cần cập nhật.</param>
    /// <param name="command">Dữ liệu cần cập nhật.</param>
    [HttpPut("api/v1/admin/orders/{id:long}")]
    [HttpPut("api/v1/orders/{id:long}")]
    public async Task<IActionResult> UpdateOrder(
        long id,
        [FromBody] UpdateTourOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var targetCommand = command with { Id = id };
        var result = await _updateOrderHandler.HandleAsync(targetCommand, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = "Cập nhật đơn hàng tour thành công.", data = result.Value });
    }

    /// <summary>
    /// Xác nhận khách hàng đã chuyển khoản đặt cọc 50%.
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    /// <param name="request">Thông tin tài khoản hoặc ghi chú đối soát.</param>
    [HttpPost("api/v1/admin/orders/{id:long}/deposit")]
    [HttpPatch("api/v1/admin/orders/{id:long}/deposit")]
    [HttpPost("api/v1/orders/{id:long}/deposit")]
    [HttpPatch("api/v1/orders/{id:long}/deposit")]
    public async Task<IActionResult> MarkDeposit(
        long id,
        [FromBody] MarkDepositRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkDepositCommand(id, request?.PaymentAccountNote);
        var result = await _markDepositHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = $"Đã xác nhận cọc 50% thành công cho đơn #{result.Value.OrderCode}!",
            data = result.Value
        });
    }

    /// <summary>
    /// Cập nhật trạng thái tiến trình làm hồ sơ Visa từ đối tác du lịch.
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    /// <param name="request">Thông tin trạng thái visa và đối tác.</param>
    [HttpPost("api/v1/admin/orders/{id:long}/visa")]
    [HttpPatch("api/v1/admin/orders/{id:long}/visa")]
    [HttpPost("api/v1/orders/{id:long}/visa")]
    [HttpPatch("api/v1/orders/{id:long}/visa")]
    public async Task<IActionResult> UpdateVisa(
        long id,
        [FromBody] UpdateVisaRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateVisaStatusCommand(
            Id: id,
            VisaStatus: request.VisaStatus,
            VisaNotes: request.VisaNotes,
            OperatorName: request.OperatorName,
            OperatorBookingRef: request.OperatorBookingRef);

        var result = await _updateVisaHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = $"Cập nhật tiến trình Visa thành công cho đơn #{result.Value.Order.OrderCode}!",
            data = result.Value.Order,
            zalo_message = result.Value.ZaloMessage
        });
    }

    /// <summary>
    /// Xác nhận khách hàng đã thanh toán 50% còn lại (hoàn tất 100% chi phí tour).
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    [HttpPost("api/v1/admin/orders/{id:long}/final-payment")]
    [HttpPatch("api/v1/admin/orders/{id:long}/final-payment")]
    [HttpPost("api/v1/orders/{id:long}/final-payment")]
    [HttpPatch("api/v1/orders/{id:long}/final-payment")]
    public async Task<IActionResult> MarkFinalPayment(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _markFinalPaymentHandler.HandleAsync(new MarkFinalPaymentCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = $"Đã xác nhận thu đủ 100% cho đơn #{result.Value.OrderCode}! Sẵn sàng khởi hành.",
            data = result.Value
        });
    }

    /// <summary>
    /// Lưu thông tin Hướng Dẫn Viên phụ trách và đánh dấu đã dặn dò khách trước khởi hành 3 ngày (T-3).
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    /// <param name="request">Thông tin HDV và điểm đón sân bay.</param>
    [HttpPost("api/v1/admin/orders/{id:long}/t3-notified")]
    [HttpPatch("api/v1/admin/orders/{id:long}/t3-notified")]
    [HttpPost("api/v1/orders/{id:long}/t3-notified")]
    [HttpPatch("api/v1/orders/{id:long}/t3-notified")]
    public async Task<IActionResult> MarkT3Notified(
        long id,
        [FromBody] MarkT3NotifiedRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkT3NotifiedCommand(
            Id: id,
            TourGuideName: request?.TourGuideName,
            TourGuidePhone: request?.TourGuidePhone,
            TourGuideNotes: request?.TourGuideNotes);

        var result = await _markT3NotifiedHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = $"Đã lưu thông tin HDV và đánh dấu dặn dò T-3 cho đơn #{result.Value.Order.OrderCode}!",
            data = result.Value.Order,
            zalo_message = result.Value.ZaloMessage
        });
    }

    /// <summary>
    /// Lấy mẫu tin nhắn Zalo soạn sẵn theo ngữ cảnh đơn hàng để sao chép nhanh.
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    /// <param name="type">Loại tin nhắn (visa_approved hoặc t3_departure).</param>
    [HttpGet("api/v1/admin/orders/{id:long}/zalo-template/{type}")]
    [HttpGet("api/v1/orders/{id:long}/zalo-template/{type}")]
    public async Task<IActionResult> GetZaloTemplate(
        long id,
        string type,
        CancellationToken cancellationToken = default)
    {
        var result = await _getZaloTemplateHandler.HandleAsync(new GetZaloTemplateQuery(id, type), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Xóa mềm đơn hàng tour khỏi danh sách hiển thị.
    /// </summary>
    /// <param name="id">ID đơn hàng.</param>
    [HttpDelete("api/v1/admin/orders/{id:long}")]
    [HttpDelete("api/v1/orders/{id:long}")]
    public async Task<IActionResult> DeleteOrder(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _deleteOrderHandler.HandleAsync(new DeleteTourOrderCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, message = $"Đã xóa đơn hàng #{id} thành công." });
    }
}

/// <summary>
/// Yêu cầu xác nhận cọc đợt 1.
/// </summary>
public sealed record MarkDepositRequest(string? PaymentAccountNote);

/// <summary>
/// Yêu cầu cập nhật tiến trình visa.
/// </summary>
public sealed record UpdateVisaRequest(
    string VisaStatus,
    string? VisaNotes,
    string? OperatorName,
    string? OperatorBookingRef);

/// <summary>
/// Yêu cầu lưu thông tin HDV và dặn dò T-3.
/// </summary>
public sealed record MarkT3NotifiedRequest(
    string? TourGuideName,
    string? TourGuidePhone,
    string? TourGuideNotes);

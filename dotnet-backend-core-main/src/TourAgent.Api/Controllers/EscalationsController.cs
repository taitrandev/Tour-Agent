namespace TourAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Escalations;
using TourAgent.Domain.Common;

/// <summary>
/// Controller quản lý Bàn Quyết Định (Human-in-the-Loop Escalations Desk).
/// Cung cấp các API truy vấn danh sách ticket, chuyển tiếp nhân viên, duyệt chiết khấu/deal nóng và đóng phiếu.
/// Hỗ trợ cả 2 định dạng route: chuẩn admin (/api/v1/admin/escalations) và chuẩn RESTful (/api/v1/escalations).
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class EscalationsController : ControllerBase
{
    private readonly IQueryHandler<GetEscalationsQuery, Result<EscalationsListResponseDto>> _getEscalationsHandler;
    private readonly ICommandHandler<UpdateEscalationStatusCommand, Result<EscalationTicketDto>> _updateStatusHandler;

    /// <summary>
    /// Khởi tạo EscalationsController với các handlers từ DI container.
    /// </summary>
    /// <param name="getEscalationsHandler">Handler truy vấn danh sách ticket.</param>
    /// <param name="updateStatusHandler">Handler cập nhật trạng thái ticket.</param>
    public EscalationsController(
        IQueryHandler<GetEscalationsQuery, Result<EscalationsListResponseDto>> getEscalationsHandler,
        ICommandHandler<UpdateEscalationStatusCommand, Result<EscalationTicketDto>> updateStatusHandler)
    {
        _getEscalationsHandler = getEscalationsHandler;
        _updateStatusHandler = updateStatusHandler;
    }

    /// <summary>
    /// Lấy danh sách các phiếu yêu cầu cần quyết định (Escalation Tickets) kèm bộ lọc theo trạng thái và tìm kiếm.
    /// </summary>
    /// <param name="status">Lọc theo trạng thái: pending, contacting, approved, closed, rejected.</param>
    /// <param name="search">Tìm kiếm theo Tên khách hàng, SĐT hoặc Mã vé.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách tickets kèm số lượng tổng và theo từng trạng thái.</returns>
    [HttpGet("api/v1/admin/escalations")]
    [HttpGet("api/v1/escalations")]
    [ProducesResponseType(typeof(EscalationsListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEscalations(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEscalationsQuery(status, search);
        var result = await _getEscalationsHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Chuyển trạng thái ticket sang 'contacting' (Chuyên viên bắt đầu gọi điện tư vấn).
    /// </summary>
    /// <param name="id">Mã định danh phiếu yêu cầu.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin ticket sau khi cập nhật.</returns>
    [HttpPost("api/v1/admin/escalations/{id:long}/contacting")]
    [HttpPost("api/v1/escalations/{id:long}/contacting")]
    [ProducesResponseType(typeof(EscalationTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkContacting(
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEscalationStatusCommand(id, "contacting", "Admin");
        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Duyệt ưu đãi / chốt deal thành công ('approved').
    /// </summary>
    /// <param name="id">Mã định danh phiếu yêu cầu.</param>
    /// <param name="request">Thông tin ghi chú duyệt deal.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin ticket sau khi duyệt.</returns>
    [HttpPost("api/v1/admin/escalations/{id:long}/approve")]
    [HttpPost("api/v1/escalations/{id:long}/approve")]
    [ProducesResponseType(typeof(EscalationTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveDeal(
        [FromRoute] long id,
        [FromBody] ApproveDealRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEscalationStatusCommand(id, "approved", "Admin", request?.Notes);
        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đóng phiếu / từ chối đề xuất ('closed' hoặc 'rejected').
    /// </summary>
    /// <param name="id">Mã định danh phiếu yêu cầu.</param>
    /// <param name="request">Lý do đóng ticket.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin ticket sau khi đóng.</returns>
    [HttpPost("api/v1/admin/escalations/{id:long}/reject")]
    [HttpPost("api/v1/admin/escalations/{id:long}/close")]
    [HttpPost("api/v1/escalations/{id:long}/close")]
    [ProducesResponseType(typeof(EscalationTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CloseTicket(
        [FromRoute] long id,
        [FromBody] CloseTicketRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEscalationStatusCommand(id, "closed", "Admin", request?.Reason);
        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cập nhật trạng thái tổng quát cho ticket.
    /// </summary>
    /// <param name="id">Mã định danh phiếu yêu cầu.</param>
    /// <param name="request">Dữ liệu trạng thái cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin ticket sau khi cập nhật.</returns>
    [HttpPut("api/v1/admin/escalations/{id:long}/status")]
    [HttpPut("api/v1/escalations/{id:long}/status")]
    [ProducesResponseType(typeof(EscalationTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] long id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEscalationStatusCommand(id, request.Status, "Admin", request.Notes);
        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }
}

/// <summary>
/// Request payload khi duyệt deal.
/// </summary>
public sealed record ApproveDealRequest(string? Notes);

/// <summary>
/// Request payload khi đóng phiếu.
/// </summary>
public sealed record CloseTicketRequest(string? Reason);

/// <summary>
/// Request payload khi cập nhật trạng thái tùy chỉnh.
/// </summary>
public sealed record UpdateStatusRequest(string Status, string? Notes);

using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Contacts;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller quản trị danh bạ khách hàng tập trung (CRM Contacts Desk & 360° Profile).
/// Cung cấp đầy đủ các API xem, tìm kiếm, lọc theo phân hạng stage, tạo mới, chỉnh sửa và xóa mềm.
/// Hỗ trợ cả 2 định dạng đường dẫn: chuẩn quản trị (/api/v1/admin/contacts) và chuẩn RESTful (/api/v1/contacts).
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class ContactsController : ControllerBase
{
    private readonly IQueryHandler<GetContactsQuery, Result<ContactListResponse>> _getContactsHandler;
    private readonly IQueryHandler<GetContactByIdQuery, Result<CrmContactDetailDto>> _getContactByIdHandler;
    private readonly ICommandHandler<CreateContactCommand, Result<CrmContactDto>> _createContactHandler;
    private readonly ICommandHandler<UpdateContactCommand, Result<CrmContactDto>> _updateContactHandler;
    private readonly ICommandHandler<DeleteContactCommand, Result> _deleteContactHandler;

    /// <summary>
    /// Khởi tạo ContactsController với các handlers tương ứng từ DI container.
    /// </summary>
    /// <param name="getContactsHandler">Handler truy vấn danh sách khách hàng.</param>
    /// <param name="getContactByIdHandler">Handler truy vấn chi tiết khách hàng và phiên chat.</param>
    /// <param name="createContactHandler">Handler tạo mới khách hàng.</param>
    /// <param name="updateContactHandler">Handler cập nhật thông tin khách hàng.</param>
    /// <param name="deleteContactHandler">Handler xóa mềm khách hàng.</param>
    public ContactsController(
        IQueryHandler<GetContactsQuery, Result<ContactListResponse>> getContactsHandler,
        IQueryHandler<GetContactByIdQuery, Result<CrmContactDetailDto>> getContactByIdHandler,
        ICommandHandler<CreateContactCommand, Result<CrmContactDto>> createContactHandler,
        ICommandHandler<UpdateContactCommand, Result<CrmContactDto>> updateContactHandler,
        ICommandHandler<DeleteContactCommand, Result> deleteContactHandler)
    {
        _getContactsHandler = getContactsHandler;
        _getContactByIdHandler = getContactByIdHandler;
        _createContactHandler = createContactHandler;
        _updateContactHandler = updateContactHandler;
        _deleteContactHandler = deleteContactHandler;
    }

    /// <summary>
    /// Lấy danh sách khách hàng kèm bộ lọc theo Stage, Kênh tiếp cận, Từ khóa tìm kiếm và Phân trang.
    /// </summary>
    /// <param name="stage">Lọc theo phân hạng: prospect, warm, hot, customer, lost.</param>
    /// <param name="channel">Lọc theo kênh: telegram, web_widget, zalo, manual.</param>
    /// <param name="search">Tìm kiếm theo Tên, Số điện thoại hoặc Username.</param>
    /// <param name="page">Trang hiện tại (mặc định 1).</param>
    /// <param name="per_page">Số lượng bản ghi mỗi trang (mặc định 20).</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách khách hàng và thông tin phân trang chuẩn định dạng.</returns>
    [HttpGet("api/v1/admin/contacts")]
    [HttpGet("api/v1/contacts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContactsAsync(
        [FromQuery] string? stage,
        [FromQuery] string? channel,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContactsQuery(stage, channel, search, page, per_page);
        var result = await _getContactsHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy chi tiết thông tin 360° của một khách hàng kèm toàn bộ lịch sử các phiên chat.
    /// </summary>
    /// <param name="id">Mã định danh khách hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Hồ sơ khách hàng 360° cùng danh sách hội thoại.</returns>
    [HttpGet("api/v1/admin/contacts/{id:long}")]
    [HttpGet("api/v1/contacts/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContactByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContactByIdQuery(id);
        var result = await _getContactByIdHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            data = result.Value
        });
    }

    /// <summary>
    /// Tạo mới một hồ sơ khách hàng thủ công.
    /// </summary>
    /// <param name="requestBody">Dữ liệu tạo khách hàng mới.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Hồ sơ khách hàng mới tạo kèm mã trạng thái 201 Created.</returns>
    [HttpPost("api/v1/admin/contacts")]
    [HttpPost("api/v1/contacts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContactAsync(
        [FromBody] CreateContactBody requestBody,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateContactCommand(
            Name: requestBody.Name,
            Phone: requestBody.Phone,
            Email: requestBody.Email,
            Channel: requestBody.Channel,
            Stage: requestBody.Stage,
            Notes: requestBody.Notes,
            Tags: requestBody.Tags);

        var result = await _createContactHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Tạo hồ sơ khách hàng thành công.",
            data = result.Value
        });
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng (Tên, SĐT, Email, Stage, Ghi chú, Nhãn).
    /// </summary>
    /// <param name="id">Mã định danh khách hàng cần cập nhật.</param>
    /// <param name="requestBody">Dữ liệu thông tin cập nhật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Dữ liệu khách hàng đã được cập nhật thành công.</returns>
    [HttpPut("api/v1/admin/contacts/{id:long}")]
    [HttpPut("api/v1/contacts/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContactAsync(
        long id,
        [FromBody] UpdateContactBody requestBody,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateContactCommand(
            Id: id,
            Name: requestBody.Name,
            Phone: requestBody.Phone,
            Email: requestBody.Email,
            Stage: requestBody.Stage,
            Notes: requestBody.Notes,
            Tags: requestBody.Tags);

        var result = await _updateContactHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { success = false, message = result.Error.Description });
            }

            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật thông tin khách hàng thành công.",
            data = result.Value
        });
    }

    /// <summary>
    /// Xóa mềm một hồ sơ khách hàng khỏi hệ thống.
    /// </summary>
    /// <param name="id">Mã định danh khách hàng cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Thông báo xác nhận xóa thành công.</returns>
    [HttpDelete("api/v1/admin/contacts/{id:long}")]
    [HttpDelete("api/v1/contacts/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContactAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteContactCommand(id);
        var result = await _deleteContactHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Đã xóa hồ sơ khách hàng thành công."
        });
    }
}

/// <summary>
/// DTO chứa nội dung body gửi lên khi thực hiện cập nhật khách hàng.
/// </summary>
public sealed record UpdateContactBody(
    string? Name,
    string? Phone,
    string? Email,
    string? Stage,
    string? Notes,
    IReadOnlyList<string>? Tags = null);

/// <summary>
/// DTO chứa nội dung body gửi lên khi thực hiện tạo mới khách hàng.
/// </summary>
public sealed record CreateContactBody(
    string? Name,
    string? Phone,
    string? Email,
    string? Channel,
    string? Stage,
    string? Notes,
    IReadOnlyList<string>? Tags = null);

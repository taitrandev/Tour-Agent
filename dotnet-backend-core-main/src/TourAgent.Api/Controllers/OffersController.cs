using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Offers;
using TourAgent.Application.Features.Offers.CreateOffer;
using TourAgent.Application.Features.Offers.DeleteOffer;
using TourAgent.Application.Features.Offers.GetAllOffers;
using TourAgent.Application.Features.Offers.GetOfferById;
using TourAgent.Application.Features.Offers.UpdateOffer;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller quản lý các gói Grand Slam Offers và Bảng xếp chồng giá trị (Value Stack).
/// Tương thích 100% với REST API và giao diện quản trị của PHP core.
/// </summary>
[ApiController]
[Route("api/v1/offers")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly IQueryHandler<GetAllOffersQuery, Result<IReadOnlyList<OfferDto>>> _getAllOffersHandler;
    private readonly IQueryHandler<GetOfferByIdQuery, Result<OfferDto>> _getOfferByIdHandler;
    private readonly ICommandHandler<CreateOfferCommand, Result<OfferDto>> _createOfferHandler;
    private readonly ICommandHandler<UpdateOfferCommand, Result<OfferDto>> _updateOfferHandler;
    private readonly ICommandHandler<DeleteOfferCommand, Result> _deleteOfferHandler;

    /// <summary>
    /// Khởi tạo OffersController với các Query/Command Handlers tương ứng từ DI container.
    /// </summary>
    public OffersController(
        IQueryHandler<GetAllOffersQuery, Result<IReadOnlyList<OfferDto>>> getAllOffersHandler,
        IQueryHandler<GetOfferByIdQuery, Result<OfferDto>> getOfferByIdHandler,
        ICommandHandler<CreateOfferCommand, Result<OfferDto>> createOfferHandler,
        ICommandHandler<UpdateOfferCommand, Result<OfferDto>> updateOfferHandler,
        ICommandHandler<DeleteOfferCommand, Result> deleteOfferHandler)
    {
        _getAllOffersHandler = getAllOffersHandler;
        _getOfferByIdHandler = getOfferByIdHandler;
        _createOfferHandler = createOfferHandler;
        _updateOfferHandler = updateOfferHandler;
        _deleteOfferHandler = deleteOfferHandler;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các Grand Slam Offer (kèm Value Stack Items và Tour liên kết).
    /// </summary>
    /// <param name="search">Từ khóa tìm kiếm theo tiêu đề, khẩu hiệu hoặc cơ chế.</param>
    /// <param name="status">Lọc theo trạng thái: active, draft, archived.</param>
    /// <param name="is_featured">Lọc theo cờ nổi bật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách các gói Offer dưới dạng JSON.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllOffers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] bool? is_featured,
        CancellationToken cancellationToken)
    {
        var query = new GetAllOffersQuery(search, status, is_featured);
        var result = await _getAllOffersHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Lấy thông tin chi tiết một gói Offer theo Id.
    /// </summary>
    /// <param name="id">Mã định danh của Offer.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Dữ liệu chi tiết của Offer kèm Value Stack.</returns>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOfferById(
        long id,
        CancellationToken cancellationToken)
    {
        var query = new GetOfferByIdQuery(id);
        var result = await _getOfferByIdHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// Tạo mới một gói Grand Slam Offer cùng với các hạng mục trong Bảng xếp chồng giá trị.
    /// </summary>
    /// <param name="command">Thông tin khởi tạo Offer từ Client.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>OfferDto vừa được tạo thành công kèm mã HTTP 201.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOffer(
        [FromBody] CreateOfferCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _createOfferHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Tạo Offer mới thành công!",
            data = result.Value
        });
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết gói Offer và đồng bộ danh sách Value Stack.
    /// </summary>
    /// <param name="id">Mã định danh gói Offer trong URL.</param>
    /// <param name="bodyCommand">Dữ liệu cập nhật từ Client.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>OfferDto sau khi được cập nhật.</returns>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateOffer(
        long id,
        [FromBody] UpdateOfferCommand bodyCommand,
        CancellationToken cancellationToken)
    {
        var command = bodyCommand with { Id = id };
        var result = await _updateOfferHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật Offer & Value Stack thành công!",
            data = result.Value
        });
    }

    /// <summary>
    /// Xóa một gói Grand Slam Offer khỏi hệ thống.
    /// </summary>
    /// <param name="id">Mã định danh gói Offer.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Thông báo xác nhận xóa thành công.</returns>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteOffer(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteOfferCommand(id);
        var result = await _deleteOfferHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            message = "Đã xóa Offer thành công!"
        });
    }
}

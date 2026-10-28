namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Truy vấn nội dung mẫu tin nhắn Zalo gửi cho khách hàng theo ngữ cảnh đơn hàng.
/// </summary>
/// <param name="Id">ID đơn hàng.</param>
/// <param name="Type">Loại mẫu tin nhắn (visa_approved hoặc t3_departure).</param>
public sealed record GetZaloTemplateQuery(
    long Id,
    string Type) : IQuery<Result<ZaloTemplateResponseDto>>;

/// <summary>
/// Xử lý lấy và định dạng mẫu tin nhắn Zalo cho đơn hàng.
/// </summary>
public sealed class GetZaloTemplateQueryHandler : IQueryHandler<GetZaloTemplateQuery, Result<ZaloTemplateResponseDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public GetZaloTemplateQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện trích xuất nội dung tin nhắn Zalo phù hợp với yêu cầu.
    /// </summary>
    /// <param name="query">Tham số truy vấn mẫu tin nhắn.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result<ZaloTemplateResponseDto>> HandleAsync(
        GetZaloTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .AsNoTracking()
            .Include(o => o.Contact)
            .Include(o => o.Travel)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure<ZaloTemplateResponseDto>(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng #{query.Id}."));
        }

        var customerName = order.Contact?.Name ?? "Quý khách";
        var tourTitle = order.Travel?.Name ?? "chương trình du lịch";

        var message = query.Type.ToLowerInvariant() switch
        {
            "visa_approved" => order.GetZaloVisaSuccessMessage(customerName, tourTitle),
            "t3_departure" => order.GetZaloT3DepartureMessage(customerName, tourTitle),
            _ => "Mẫu tin nhắn không hợp lệ."
        };

        var response = new ZaloTemplateResponseDto(
            Success: true,
            Type: query.Type,
            MessageContent: message);

        return Result.Success(response);
    }
}

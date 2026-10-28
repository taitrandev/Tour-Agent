namespace TourAgent.Application.Features.Orders;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Lệnh xóa mềm đơn hàng tour khỏi hệ thống hoạt động.
/// </summary>
/// <param name="Id">ID đơn hàng cần xóa.</param>
public sealed record DeleteTourOrderCommand(long Id) : ICommand<Result>;

/// <summary>
/// Xử lý xóa mềm đơn hàng tour.
/// </summary>
public sealed class DeleteTourOrderCommandHandler : ICommandHandler<DeleteTourOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    public DeleteTourOrderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện xóa mềm đơn hàng và lưu thay đổi vào cơ sở dữ liệu.
    /// </summary>
    /// <param name="command">Lệnh chứa ID đơn hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public async Task<Result> HandleAsync(
        DeleteTourOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.TourOrders
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order == null)
        {
            return Result.Failure(new Error("TourOrder.NotFound", $"Không tìm thấy đơn hàng #{command.Id}."));
        }

        order.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

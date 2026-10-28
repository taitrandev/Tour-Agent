using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// Lệnh xóa mềm một hồ sơ khách hàng CRM theo ID.
/// </summary>
/// <param name="Id">Mã định danh của khách hàng cần xóa.</param>
public sealed record DeleteContactCommand(long Id) : ICommand<Result>;

/// <summary>
/// Xử lý lệnh xóa mềm hồ sơ khách hàng CRM.
/// </summary>
public sealed class DeleteContactCommandHandler : ICommandHandler<DeleteContactCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Database Context kết nối PostgreSQL.</param>
    public DeleteContactCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Tìm khách hàng và thực hiện xóa mềm (Soft Delete).
    /// </summary>
    /// <param name="command">Lệnh chứa ID khách hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result báo trạng thái xóa thành công hoặc lỗi không tìm thấy.</returns>
    public async Task<Result> HandleAsync(
        DeleteContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (contact == null)
        {
            return Result.Failure(Error.NotFound(
                "CrmContact.NotFound",
                $"Không tìm thấy hồ sơ khách hàng với ID #{command.Id}."));
        }

        contact.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

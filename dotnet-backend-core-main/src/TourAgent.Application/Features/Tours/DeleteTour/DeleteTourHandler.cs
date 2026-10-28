using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.DeleteTour;

/// <summary>
/// Handler xử lý xóa một tour du lịch khỏi cơ sở dữ liệu.
/// </summary>
public sealed class DeleteTourHandler : ICommandHandler<DeleteTourCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo DeleteTourHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public DeleteTourHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện xóa tour và các quan hệ phụ thuộc.
    /// </summary>
    /// <param name="command">Command chứa Id của tour cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        DeleteTourCommand command,
        CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours
            .Include(t => t.Departures)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (tour is null)
        {
            return Result.Failure(TourErrors.NotFound(command.Id));
        }

        _dbContext.Tours.Remove(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

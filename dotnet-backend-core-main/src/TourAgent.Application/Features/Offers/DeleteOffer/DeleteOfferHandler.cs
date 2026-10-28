using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.DeleteOffer;

/// <summary>
/// Handler xử lý xóa gói Grand Slam Offer khỏi hệ thống.
/// </summary>
public sealed class DeleteOfferHandler : ICommandHandler<DeleteOfferCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo DeleteOfferHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public DeleteOfferHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện xóa Offer và các hạng mục Value Stack liên quan.
    /// </summary>
    /// <param name="command">Chứa Id gói Offer cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        DeleteOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var offer = await _dbContext.Offers
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (offer is null)
        {
            return Result.Failure(
                Error.NotFound("Offer.NotFound", $"Không tìm thấy Offer có Id {command.Id}."));
        }

        // Xóa các items trước
        _dbContext.OfferItems.RemoveRange(offer.Items);
        _dbContext.Offers.Remove(offer);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

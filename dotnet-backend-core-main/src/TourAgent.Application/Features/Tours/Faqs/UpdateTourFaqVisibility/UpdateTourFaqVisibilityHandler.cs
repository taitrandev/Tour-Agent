using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Faqs.UpdateTourFaqVisibility;

/// <summary>
/// Command yêu cầu cập nhật trạng thái hiển thị của câu hỏi thường gặp.
/// </summary>
/// <param name="Id">Mã định danh của câu hỏi thường gặp.</param>
/// <param name="IsVisible">Trạng thái hiển thị mới.</param>
public sealed record UpdateTourFaqVisibilityCommand(long Id, bool IsVisible) : ICommand<Result>;

/// <summary>
/// Handler xử lý cập nhật trạng thái ẩn/hiện của câu hỏi thường gặp.
/// </summary>
public sealed class UpdateTourFaqVisibilityHandler : ICommandHandler<UpdateTourFaqVisibilityCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public UpdateTourFaqVisibilityHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Cập nhật trường IsVisible của câu hỏi thường gặp trong CSDL.
    /// </summary>
    /// <param name="command">Lệnh cập nhật trạng thái hiển thị.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        UpdateTourFaqVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var faq = await _dbContext.TourFaqs
            .FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (faq is null)
        {
            return Result.Failure(TourErrors.FaqNotFound(command.Id));
        }

        faq.UpdateVisibility(command.IsVisible);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


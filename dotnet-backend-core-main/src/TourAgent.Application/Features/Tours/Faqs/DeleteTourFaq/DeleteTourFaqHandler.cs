using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Faqs.DeleteTourFaq;

/// <summary>
/// Command yêu cầu xóa một câu hỏi thường gặp.
/// </summary>
/// <param name="Id">Mã định danh của FAQ cần xóa.</param>
public sealed record DeleteTourFaqCommand(long Id) : ICommand<Result>;

/// <summary>
/// Handler xử lý xóa câu hỏi thường gặp khỏi CSDL.
/// </summary>
public sealed class DeleteTourFaqHandler : ICommandHandler<DeleteTourFaqCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public DeleteTourFaqHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xóa câu hỏi FAQ theo Id khỏi CSDL.
    /// </summary>
    /// <param name="command">Lệnh chứa Id cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Result thành công hoặc lỗi NotFound.</returns>
    public async Task<Result> HandleAsync(
        DeleteTourFaqCommand command,
        CancellationToken cancellationToken = default)
    {
        var faq = await _dbContext.TourFaqs
            .FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (faq is null)
        {
            return Result.Failure(TourErrors.FaqNotFound(command.Id));
        }

        _dbContext.TourFaqs.Remove(faq);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


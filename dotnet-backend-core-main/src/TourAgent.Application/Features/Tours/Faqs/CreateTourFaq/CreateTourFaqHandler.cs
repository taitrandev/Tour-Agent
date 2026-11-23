using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.Faqs.CreateTourFaq;

/// <summary>
/// Command yêu cầu tạo mới một câu hỏi thường gặp cho Tour.
/// </summary>
public sealed record CreateTourFaqCommand(
    long TourId,
    string Question,
    string Answer,
    int SortOrder,
    bool IsVisible) : ICommand<Result<long>>;

/// <summary>
/// Handler xử lý tạo mới câu hỏi thường gặp cho Tour.
/// </summary>
public sealed class CreateTourFaqHandler : ICommandHandler<CreateTourFaqCommand, Result<long>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public CreateTourFaqHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện kiểm tra tính hợp lệ và thêm câu hỏi thường gặp vào CSDL.
    /// </summary>
    /// <param name="command">Lệnh tạo câu hỏi thường gặp.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Id của câu hỏi FAQ vừa tạo nếu thành công, ngược lại trả về Failure.</returns>
    public async Task<Result<long>> HandleAsync(
        CreateTourFaqCommand command,
        CancellationToken cancellationToken = default)
    {
        var faqResult = TourFaq.Create(
            command.TourId,
            command.Question,
            command.Answer,
            command.SortOrder,
            command.IsVisible);

        if (faqResult.IsFailure)
        {
            return Result.Failure<long>(faqResult.Error);
        }

        var faq = faqResult.Value;
        _dbContext.TourFaqs.Add(faq);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(faq.Id);
    }
}

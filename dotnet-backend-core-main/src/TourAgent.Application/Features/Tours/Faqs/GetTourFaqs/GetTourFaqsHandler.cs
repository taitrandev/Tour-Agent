using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.Faqs.GetTourFaqs;

/// <summary>
/// DTO chứa thông tin một câu hỏi thường gặp (FAQ) cho Tour.
/// </summary>
public sealed record TourFaqDto(
    long Id,
    long TravelId,
    string Question,
    string Answer,
    int SortOrder,
    bool IsVisible,
    string CreatedAt);

/// <summary>
/// Query yêu cầu lấy danh sách câu hỏi thường gặp cho một Tour du lịch.
/// </summary>
/// <param name="TourId">Mã định danh của Tour.</param>
public sealed record GetTourFaqsQuery(long TourId) : IQuery<Result<IReadOnlyList<TourFaqDto>>>;

/// <summary>
/// Handler xử lý câu truy vấn danh sách câu hỏi thường gặp của Tour.
/// </summary>
public sealed class GetTourFaqsHandler : IQueryHandler<GetTourFaqsQuery, Result<IReadOnlyList<TourFaqDto>>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng trừu tượng truy cập CSDL.</param>
    public GetTourFaqsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện truy vấn danh sách câu hỏi thường gặp sắp xếp theo SortOrder tăng dần rồi đến Id tăng dần.
    /// </summary>
    /// <param name="request">Câu truy vấn chứa TourId.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách các FAQ dưới dạng TourFaqDto.</returns>
    public async Task<Result<IReadOnlyList<TourFaqDto>>> HandleAsync(
        GetTourFaqsQuery request,
        CancellationToken cancellationToken = default)
    {
        var faqs = await _dbContext.TourFaqs
            .AsNoTracking()
            .Where(f => f.TourId == request.TourId)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .Select(f => new TourFaqDto(
                f.Id,
                f.TourId,
                f.Question,
                f.Answer,
                f.SortOrder,
                f.IsVisible,
                f.CreatedAtUtc.ToString("o")))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TourFaqDto>>(faqs);
    }
}

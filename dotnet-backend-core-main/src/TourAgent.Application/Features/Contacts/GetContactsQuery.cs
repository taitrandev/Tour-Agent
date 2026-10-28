using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// Truy vấn danh sách khách hàng CRM có lọc theo stage, kênh, từ khóa tìm kiếm và phân trang.
/// </summary>
/// <param name="Stage">Lọc theo giai đoạn (prospect, warm, hot, customer, lost).</param>
/// <param name="Channel">Lọc theo kênh (telegram, web_widget, zalo, manual).</param>
/// <param name="Search">Từ khóa tìm kiếm theo tên, số điện thoại hoặc @username.</param>
/// <param name="Page">Số trang hiện tại (bắt đầu từ 1).</param>
/// <param name="PerPage">Số lượng bản ghi trên một trang (mặc định 20).</param>
public sealed record GetContactsQuery(
    string? Stage = null,
    string? Channel = null,
    string? Search = null,
    int Page = 1,
    int PerPage = 20) : IQuery<Result<ContactListResponse>>;

/// <summary>
/// Xử lý truy vấn danh sách khách hàng CRM kèm số lượng phiên chat và phân trang.
/// </summary>
public sealed class GetContactsQueryHandler : IQueryHandler<GetContactsQuery, Result<ContactListResponse>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Database Context kết nối PostgreSQL.</param>
    public GetContactsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thực hiện truy vấn danh sách khách hàng theo các điều kiện lọc và phân trang.
    /// </summary>
    /// <param name="query">Đối tượng chứa các tham số lọc.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa phản hồi danh sách khách hàng cùng thông tin phân trang.</returns>
    public async Task<Result<ContactListResponse>> HandleAsync(
        GetContactsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var perPage = query.PerPage <= 0 ? 20 : query.PerPage;

        var contactsQuery = _dbContext.Contacts.AsNoTracking();

        // 1. Lọc theo Stage nếu có
        if (!string.IsNullOrWhiteSpace(query.Stage))
        {
            var normalizedStage = query.Stage.Trim().ToLowerInvariant();
            contactsQuery = contactsQuery.Where(c => c.Stage == normalizedStage);
        }

        // 2. Lọc theo Kênh tiếp cận nếu có
        if (!string.IsNullOrWhiteSpace(query.Channel))
        {
            var normalizedChannel = query.Channel.Trim().ToLowerInvariant();
            contactsQuery = contactsQuery.Where(c => c.Channel == normalizedChannel);
        }

        // 3. Tìm kiếm theo tên, số điện thoại hoặc username
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            contactsQuery = contactsQuery.Where(c =>
                (c.Name != null && c.Name.ToLower().Contains(keyword)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(keyword)) ||
                (c.TelegramUsername != null && c.TelegramUsername.ToLower().Contains(keyword)));
        }

        // 4. Tính toán tổng số bản ghi
        var totalRecords = await contactsQuery.CountAsync(cancellationToken);
        var lastPage = (int)Math.Ceiling(totalRecords / (double)perPage);
        if (lastPage < 1) lastPage = 1;

        // 5. Lấy danh sách phân trang
        var pagedContacts = await contactsQuery
            .OrderByDescending(c => c.LastActiveAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync(cancellationToken);

        // 6. Tính số lượng phiên hội thoại cho từng khách hàng
        var contactIds = pagedContacts.Select(c => c.Id).ToList();
        var convCounts = await _dbContext.Conversations
            .AsNoTracking()
            .Where(cv => cv.ContactId.HasValue && contactIds.Contains(cv.ContactId.Value))
            .GroupBy(cv => cv.ContactId!.Value)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContactId, x => x.Count, cancellationToken);

        // 7. Chuyển đổi sang DTO
        var contactDtos = pagedContacts.Select(c =>
        {
            IReadOnlyList<string>? tags = null;
            if (!string.IsNullOrWhiteSpace(c.TagsJson))
            {
                try
                {
                    tags = JsonSerializer.Deserialize<List<string>>(c.TagsJson);
                }
                catch
                {
                    tags = [];
                }
            }

            var count = convCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0;

            return new CrmContactDto(
                Id: c.Id,
                Name: c.Name,
                Phone: c.Phone,
                Email: c.Email,
                TelegramChatId: c.TelegramChatId,
                TelegramUsername: c.TelegramUsername,
                ZaloId: c.ZaloId,
                Channel: c.Channel,
                Stage: c.Stage,
                Tags: tags,
                Notes: c.Notes,
                LastActiveAt: c.LastActiveAtUtc,
                ConversationsCount: count,
                CreatedAt: c.CreatedAtUtc,
                UpdatedAt: c.UpdatedAtUtc);
        }).ToList();

        var response = new ContactListResponse(
            Success: true,
            Data: contactDtos,
            Pagination: new PaginationMetaDto(
                CurrentPage: page,
                LastPage: lastPage,
                PerPage: perPage,
                Total: totalRecords));

        return Result.Success(response);
    }
}

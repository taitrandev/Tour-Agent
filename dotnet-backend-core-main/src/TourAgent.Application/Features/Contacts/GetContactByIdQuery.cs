using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// Truy vấn chi tiết một hồ sơ khách hàng CRM theo ID kèm toàn bộ lịch sử các phiên chat.
/// </summary>
/// <param name="Id">Mã định danh của khách hàng.</param>
public sealed record GetContactByIdQuery(long Id) : IQuery<Result<CrmContactDetailDto>>;

/// <summary>
/// Xử lý truy vấn chi tiết hồ sơ khách hàng CRM kèm danh sách phiên hội thoại.
/// </summary>
public sealed class GetContactByIdQueryHandler : IQueryHandler<GetContactByIdQuery, Result<CrmContactDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Database Context kết nối PostgreSQL.</param>
    public GetContactByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Tìm kiếm và trả về thông tin chi tiết khách hàng và lịch sử hội thoại liên kết.
    /// </summary>
    /// <param name="query">Đối tượng chứa ID khách hàng.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa CrmContactDetailDto hoặc lỗi không tìm thấy.</returns>
    public async Task<Result<CrmContactDetailDto>> HandleAsync(
        GetContactByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        if (contact == null)
        {
            return Result.Failure<CrmContactDetailDto>(Error.NotFound(
                "CrmContact.NotFound",
                $"Không tìm thấy hồ sơ khách hàng với ID #{query.Id}."));
        }

        // Lấy danh sách các phiên hội thoại của khách hàng kèm số lượng tin nhắn
        var conversations = await _dbContext.Conversations
            .AsNoTracking()
            .Where(cv => cv.ContactId == query.Id)
            .OrderByDescending(cv => cv.LastMessageAtUtc)
            .ThenByDescending(cv => cv.Id)
            .Select(cv => new ContactConversationDto(
                cv.Id,
                cv.Channel,
                cv.ChannelUserId,
                cv.Status.ToString(),
                cv.LastMessageAtUtc,
                cv.Messages.Count))
            .ToListAsync(cancellationToken);

        IReadOnlyList<string>? tags = null;
        if (!string.IsNullOrWhiteSpace(contact.TagsJson))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(contact.TagsJson);
            }
            catch
            {
                tags = [];
            }
        }

        var detailDto = new CrmContactDetailDto(
            Id: contact.Id,
            Name: contact.Name,
            Phone: contact.Phone,
            Email: contact.Email,
            TelegramChatId: contact.TelegramChatId,
            TelegramUsername: contact.TelegramUsername,
            ZaloId: contact.ZaloId,
            Channel: contact.Channel,
            Stage: contact.Stage,
            Tags: tags,
            Notes: contact.Notes,
            LastActiveAt: contact.LastActiveAtUtc,
            CreatedAt: contact.CreatedAtUtc,
            UpdatedAt: contact.UpdatedAtUtc,
            Conversations: conversations);

        return Result.Success(detailDto);
    }
}

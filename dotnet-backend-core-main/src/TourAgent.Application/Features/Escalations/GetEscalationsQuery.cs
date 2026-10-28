namespace TourAgent.Application.Features.Escalations;

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

/// <summary>
/// Câu truy vấn lấy danh sách các phiếu chuyển giao chuyên viên (Escalations Desk) kèm bộ lọc và thống kê.
/// </summary>
/// <param name="Status">Lọc theo trạng thái phiếu (pending, contacting, approved, closed, rejected) hoặc null để lấy tất cả.</param>
/// <param name="Search">Từ khóa tìm kiếm theo tên khách, số điện thoại, mã vé hoặc ghi chú.</param>
public sealed record GetEscalationsQuery(
    string? Status = null,
    string? Search = null
) : IQuery<Result<EscalationsListResponseDto>>;

/// <summary>
/// Handler xử lý câu truy vấn lấy danh sách Escalations từ cơ sở dữ liệu.
/// </summary>
public sealed class GetEscalationsQueryHandler : IQueryHandler<GetEscalationsQuery, Result<EscalationsListResponseDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu.</param>
    public GetEscalationsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<EscalationsListResponseDto>> HandleAsync(
        GetEscalationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.Escalations
            .AsNoTracking()
            .Include(e => e.Conversation)
            .Include(e => e.Travel)
            .AsQueryable();

        // 1. Áp dụng bộ lọc tìm kiếm
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = $"%{query.Search.Trim().ToLowerInvariant()}%";
            queryable = queryable.Where(e =>
                (e.CustomerName != null && EF.Functions.Like(e.CustomerName.ToLower(), searchLower)) ||
                (e.CustomerPhone != null && EF.Functions.Like(e.CustomerPhone.ToLower(), searchLower)) ||
                (e.Notes != null && EF.Functions.Like(e.Notes.ToLower(), searchLower)) ||
                EF.Functions.Like(e.Id.ToString(), searchLower));
        }

        // 2. Thống kê theo trạng thái trên toàn bộ tập dữ liệu (trước khi lọc theo status)
        var allTickets = await queryable.OrderByDescending(e => e.Id).ToListAsync(cancellationToken);

        var pendingCount = allTickets.Count(t => t.Status == "pending");
        var contactingCount = allTickets.Count(t => t.Status == "contacting");
        var approvedCount = allTickets.Count(t => t.Status == "approved");

        // 3. Lọc theo trạng thái nếu có chỉ định
        var filteredList = allTickets;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var targetStatus = query.Status.Trim().ToLowerInvariant();
            filteredList = allTickets.Where(t => t.Status.ToLowerInvariant() == targetStatus).ToList();
        }

        // 4. Ánh xạ sang danh sách DTO
        var items = filteredList.Select(ticket =>
        {
            var channel = ticket.Conversation?.Channel ?? (ticket.ChatId != null ? "telegram" : "web_widget");
            var tourTitle = ticket.Travel?.Name ?? (ticket.TravelId.HasValue ? $"Tour #{ticket.TravelId}" : "Chưa chọn tour cụ thể");
            var timeAgo = CalculateTimeAgo(ticket.CreatedAtUtc);

            return new EscalationTicketDto(
                Id: ticket.Id,
                Code: $"#ESC-{ticket.Id}",
                CustomerName: !string.IsNullOrWhiteSpace(ticket.CustomerName) ? ticket.CustomerName : "Khách quan tâm tour",
                CustomerPhone: ticket.CustomerPhone ?? "Chưa có SĐT",
                Channel: channel.Contains("telegram", StringComparison.OrdinalIgnoreCase) ? "telegram" : "web_widget",
                TourTitle: tourTitle,
                DiscountPercent: ticket.DiscountPercent ?? 0,
                GroupSize: ticket.GroupSize ?? 1,
                Notes: ticket.Notes ?? "Yêu cầu tư vấn trực tiếp từ AI Agent",
                Status: ticket.Status,
                TimeAgo: timeAgo,
                CreatedAt: ticket.CreatedAtUtc,
                ConversationId: ticket.ConversationId,
                TravelId: ticket.TravelId
            );
        }).ToList();

        var response = new EscalationsListResponseDto(
            Items: items,
            TotalCount: allTickets.Count,
            PendingCount: pendingCount,
            ContactingCount: contactingCount,
            ApprovedCount: approvedCount
        );

        return Result.Success(response);
    }

    /// <summary>
    /// Tính toán chuỗi thời gian tương đối thân thiện (ví dụ: '5 phút trước', '2 giờ trước').
    /// </summary>
    /// <param name="dateTimeUtc">Thời điểm UTC.</param>
    /// <returns>Chuỗi mô tả khoảng cách thời gian.</returns>
    private static string CalculateTimeAgo(DateTime? dateTimeUtc)
    {
        if (!dateTimeUtc.HasValue)
        {
            return "Vừa xong";
        }

        var diff = DateTime.UtcNow - dateTimeUtc.Value;
        if (diff.TotalMinutes < 1)
        {
            return "Vừa xong";
        }
        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes} phút trước";
        }
        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours} giờ trước";
        }
        return $"{(int)diff.TotalDays} ngày trước";
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// Lệnh cập nhật thông tin hồ sơ khách hàng CRM theo ID.
/// </summary>
public sealed record UpdateContactCommand(
    [property: JsonIgnore] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("stage")] string? Stage,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags = null) : ICommand<Result<CrmContactDto>>;

/// <summary>
/// Xử lý lệnh cập nhật thông tin hồ sơ khách hàng CRM.
/// </summary>
public sealed class UpdateContactCommandHandler : ICommandHandler<UpdateContactCommand, Result<CrmContactDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Database Context kết nối PostgreSQL.</param>
    public UpdateContactCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng, lưu vào CSDL và trả về DTO mới nhất.
    /// </summary>
    /// <param name="command">Dữ liệu cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa DTO đã cập nhật hoặc lỗi không tìm thấy / validation.</returns>
    public async Task<Result<CrmContactDto>> HandleAsync(
        UpdateContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (contact == null)
        {
            return Result.Failure<CrmContactDto>(Error.NotFound(
                "CrmContact.NotFound",
                $"Không tìm thấy hồ sơ khách hàng với ID #{command.Id}."));
        }

        string? tagsJson = null;
        if (command.Tags != null)
        {
            tagsJson = JsonSerializer.Serialize(command.Tags);
        }

        var updateResult = contact.Update(
            name: command.Name,
            phone: command.Phone,
            email: command.Email,
            stage: command.Stage,
            notes: command.Notes,
            tagsJson: tagsJson);

        if (updateResult.IsFailure)
        {
            return Result.Failure<CrmContactDto>(updateResult.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var convCount = await _dbContext.Conversations
            .CountAsync(cv => cv.ContactId == contact.Id, cancellationToken);

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

        var dto = new CrmContactDto(
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
            ConversationsCount: convCount,
            CreatedAt: contact.CreatedAtUtc,
            UpdatedAt: contact.UpdatedAtUtc);

        return Result.Success(dto);
    }
}

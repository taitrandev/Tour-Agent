using System.Text.Json;
using System.Text.Json.Serialization;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Contacts;

namespace TourAgent.Application.Features.Contacts;

/// <summary>
/// Lệnh tạo mới một hồ sơ khách hàng CRM thủ công hoặc từ kênh tích hợp.
/// </summary>
public sealed record CreateContactCommand(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("channel")] string? Channel = "manual",
    [property: JsonPropertyName("stage")] string? Stage = null,
    [property: JsonPropertyName("notes")] string? Notes = null,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags = null) : ICommand<Result<CrmContactDto>>;

/// <summary>
/// Xử lý lệnh tạo mới hồ sơ khách hàng CRM.
/// </summary>
public sealed class CreateContactCommandHandler : ICommandHandler<CreateContactCommand, Result<CrmContactDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo Handler với DbContext.
    /// </summary>
    /// <param name="dbContext">Database Context kết nối PostgreSQL.</param>
    public CreateContactCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Tạo mới bản ghi khách hàng, lưu vào CSDL và trả về DTO.
    /// </summary>
    /// <param name="command">Dữ liệu thông tin khách hàng cần tạo.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa DTO khách hàng mới tạo hoặc lỗi hợp lệ dữ liệu.</returns>
    public async Task<Result<CrmContactDto>> HandleAsync(
        CreateContactCommand command,
        CancellationToken cancellationToken = default)
    {
        string? tagsJson = null;
        if (command.Tags != null && command.Tags.Count > 0)
        {
            tagsJson = JsonSerializer.Serialize(command.Tags);
        }

        var contactResult = CrmContact.Create(
            name: command.Name,
            phone: command.Phone,
            email: command.Email,
            channel: command.Channel ?? "manual",
            stage: command.Stage,
            tagsJson: tagsJson,
            notes: command.Notes);

        if (contactResult.IsFailure)
        {
            return Result.Failure<CrmContactDto>(contactResult.Error);
        }

        var contact = contactResult.Value;
        await _dbContext.Contacts.AddAsync(contact, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
            Tags: command.Tags,
            Notes: contact.Notes,
            LastActiveAt: contact.LastActiveAtUtc,
            ConversationsCount: 0,
            CreatedAt: contact.CreatedAtUtc,
            UpdatedAt: contact.UpdatedAtUtc);

        return Result.Success(dto);
    }
}

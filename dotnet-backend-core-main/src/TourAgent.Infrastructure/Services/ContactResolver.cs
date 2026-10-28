using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Contacts;
using TourAgent.Domain.Conversations;

namespace TourAgent.Infrastructure.Services;

/// <summary>
/// Dịch vụ nhận diện, trích xuất và đồng bộ hồ sơ khách hàng CRM từ các kênh tin nhắn.
/// Tự động bóc tách số điện thoại Việt Nam từ văn bản tin nhắn để nâng hạng Hot Lead tức thì.
/// </summary>
public sealed class ContactResolver : IContactResolver
{
    private static readonly Regex VietnamPhoneRegex = new(
        @"(?:\+?84|0)(3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SpaceOrPunctuationBetweenDigitsRegex = new(
        @"(\d)[\s\.\-](\d)",
        RegexOptions.Compiled);

    private readonly IAppDbContext _dbContext;
    private readonly ILogger<ContactResolver> _logger;

    /// <summary>
    /// Khởi tạo ContactResolver với DbContext và Logger.
    /// </summary>
    /// <param name="dbContext">Database context kết nối PostgreSQL.</param>
    /// <param name="logger">Logger ghi nhận nhật ký.</param>
    public ContactResolver(IAppDbContext dbContext, ILogger<ContactResolver> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Nhận diện hoặc tạo mới CrmContact, bóc tách SĐT và gắn liên kết với Conversation.
    /// </summary>
    /// <param name="channel">Kênh giao tiếp (telegram, web_widget, zalo).</param>
    /// <param name="channelUserId">Mã định danh người dùng trên kênh.</param>
    /// <param name="text">Nội dung tin nhắn khách hàng gửi.</param>
    /// <param name="senderName">Tên hiển thị của khách hàng.</param>
    /// <param name="username">Username tài khoản (ví dụ trên Telegram không kèm @).</param>
    /// <param name="conversation">Phiên hội thoại cần liên kết.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Hồ sơ CrmContact đã được đồng bộ.</returns>
    public async Task<CrmContact> ResolveAndSyncContactAsync(
        string channel,
        string channelUserId,
        string? text,
        string? senderName,
        string? username,
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        var normalizedChannel = channel.Trim().ToLowerInvariant();

        // 1. Tìm kiếm Contact hiện có theo kênh tương ứng
        CrmContact? contact = null;

        if (normalizedChannel == "telegram")
        {
            contact = await _dbContext.Contacts
                .FirstOrDefaultAsync(c => c.TelegramChatId == channelUserId, cancellationToken);
        }
        else if (normalizedChannel == "zalo")
        {
            contact = await _dbContext.Contacts
                .FirstOrDefaultAsync(c => c.ZaloId == channelUserId, cancellationToken);
        }

        // 2. Nếu chưa có, tạo mới hồ sơ Contact ban đầu
        if (contact is null)
        {
            var createResult = CrmContact.Create(
                name: senderName,
                phone: null,
                email: null,
                telegramChatId: normalizedChannel == "telegram" ? channelUserId : null,
                telegramUsername: username,
                zaloId: normalizedChannel == "zalo" ? channelUserId : null,
                channel: normalizedChannel,
                stage: CrmContact.StageProspect);

            if (createResult.IsSuccess)
            {
                contact = createResult.Value;
                _dbContext.Contacts.Add(contact);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Đã tự động tạo CrmContact #{ContactId} cho khách hàng {SenderName} từ kênh {Channel}", contact.Id, senderName, channel);
            }
            else
            {
                _logger.LogError("Lỗi khi tạo CrmContact: {Error}", createResult.Error.Description);
                throw new InvalidOperationException($"Không thể tạo CrmContact: {createResult.Error.Description}");
            }
        }
        else
        {
            // Cập nhật tên hoặc username nếu ban đầu chưa có
            bool hasChanges = false;
            if (string.IsNullOrWhiteSpace(contact.Name) && !string.IsNullOrWhiteSpace(senderName))
            {
                contact.Update(senderName, contact.Phone, contact.Email, contact.Stage, contact.Notes, contact.TagsJson);
                hasChanges = true;
            }

            contact.RecordActivity();
            hasChanges = true;

            if (hasChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 3. Tự động bóc tách số điện thoại Việt Nam từ văn bản tin nhắn
        if (!string.IsNullOrWhiteSpace(text))
        {
            var detectedPhone = DetectPhoneNumber(text);
            if (detectedPhone != null)
            {
                _logger.LogInformation("Phát hiện số điện thoại {Phone} trong tin nhắn của khách #{ContactId}. Tự động thăng hạng HOT LEAD!", detectedPhone, contact.Id);
                contact.AttachPhone(detectedPhone);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 4. Gắn liên kết giữa Conversation và CrmContact nếu chưa gắn
        if (conversation.ContactId != contact.Id)
        {
            conversation.LinkContact(contact.Id);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã liên kết Conversation #{ConvId} với CrmContact #{ContactId}", conversation.Id, contact.Id);
        }

        return contact;
    }

    /// <summary>
    /// Quét và bóc tách số điện thoại Việt Nam 10 chữ số từ văn bản tin nhắn thô.
    /// Xử lý các trường hợp viết cách nhau bằng dấu chấm, dấu gạch hoặc dấu cách (ví dụ: 0909.349.909, 090 934 9909).
    /// </summary>
    /// <param name="text">Văn bản tin nhắn của khách hàng.</param>
    /// <returns>Số điện thoại 10 chữ số đã chuẩn hóa hoặc null nếu không tìm thấy.</returns>
    public static string? DetectPhoneNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Xóa các ký tự ngăn cách giữa các chữ số
        var sanitized = SpaceOrPunctuationBetweenDigitsRegex.Replace(text, "$1$2");
        sanitized = SpaceOrPunctuationBetweenDigitsRegex.Replace(sanitized, "$1$2");

        var match = VietnamPhoneRegex.Match(sanitized);
        if (!match.Success) return null;

        var raw = match.Value;
        var digits = Regex.Replace(raw, @"\D", "");

        if (digits.StartsWith("84") && digits.Length == 11)
        {
            digits = "0" + digits.Substring(2);
        }

        if (digits.Length == 10 && digits.StartsWith("0"))
        {
            return digits;
        }

        return null;
    }
}

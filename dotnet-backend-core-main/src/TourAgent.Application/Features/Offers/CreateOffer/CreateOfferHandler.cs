using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Offers;

namespace TourAgent.Application.Features.Offers.CreateOffer;

/// <summary>
/// Handler xử lý tạo mới gói Grand Slam Offer và các hạng mục Value Stack.
/// </summary>
public sealed class CreateOfferHandler : ICommandHandler<CreateOfferCommand, Result<OfferDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo CreateOfferHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public CreateOfferHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý tạo mới Offer và lưu trữ giao dịch toàn vẹn.
    /// </summary>
    /// <param name="command">Thông tin khởi tạo Offer.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>OfferDto đã được khởi tạo hoàn chỉnh.</returns>
    public async Task<Result<OfferDto>> HandleAsync(
        CreateOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Result.Failure<OfferDto>(
                Error.Validation("Offer.TitleRequired", "Tiêu đề Offer không được để trống."));
        }

        // Tạo slug duy nhất
        var baseSlug = !string.IsNullOrWhiteSpace(command.Slug)
            ? Slugify(command.Slug)
            : Slugify(command.Title);

        var uniqueSlug = await GenerateUniqueSlugAsync(baseSlug, null, cancellationToken);

        // Parse status
        var status = OfferStatus.Active;
        if (!string.IsNullOrWhiteSpace(command.Status) &&
            Enum.TryParse<OfferStatus>(command.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        // Chuẩn hóa JSON Guarantees và PaymentTerms
        var guaranteesJson = command.Guarantees != null
            ? JsonSerializer.Serialize(command.Guarantees)
            : null;

        var paymentTermsJson = command.PaymentTerms != null
            ? JsonSerializer.Serialize(command.PaymentTerms)
            : null;

        var offer = Offer.Create(
            title: command.Title.Trim(),
            slug: uniqueSlug,
            originalPrice: command.OriginalPrice,
            offerPrice: command.OfferPrice,
            travelId: command.TravelId,
            tagline: command.Tagline,
            targetAudience: command.TargetAudience,
            heroMechanism: command.HeroMechanism,
            corePromise: command.CorePromise,
            depositPercentage: command.DepositPercentage,
            durationDays: command.DurationDays,
            durationNights: command.DurationNights,
            maxSlotsPerWeek: command.MaxSlotsPerWeek,
            availableSlots: command.AvailableSlots,
            urgencyHours: command.UrgencyHours,
            guarantees: guaranteesJson,
            paymentTerms: paymentTermsJson,
            salesScript: command.SalesScript,
            status: status,
            isFeatured: command.IsFeatured);

        _dbContext.Offers.Add(offer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Thêm các hạng mục Value Stack
        if (command.Items != null && command.Items.Count > 0)
        {
            var sortIndex = 1;
            foreach (var itemDto in command.Items)
            {
                if (string.IsNullOrWhiteSpace(itemDto.Name)) continue;

                var itemType = string.Equals(itemDto.Type, "bonus", StringComparison.OrdinalIgnoreCase)
                    ? OfferItemType.Bonus
                    : OfferItemType.Core;

                var isHighlighted = itemDto.IsHighlighted || itemType == OfferItemType.Bonus;

                var item = OfferItem.Create(
                    offerId: offer.Id,
                    name: itemDto.Name.Trim(),
                    description: itemDto.Description,
                    type: itemType,
                    originalValue: itemDto.OriginalValue,
                    sortOrder: itemDto.SortOrder > 0 ? itemDto.SortOrder : sortIndex++,
                    isHighlighted: isHighlighted);

                _dbContext.OfferItems.Add(item);
                offer.AddItem(item);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Tải thông tin tour liên kết nếu có
        if (offer.TravelId.HasValue)
        {
            await _dbContext.Tours
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == offer.TravelId.Value, cancellationToken);
        }

        return Result.Success(offer.ToDto());
    }

    /// <summary>
    /// Sinh chuỗi slug duy nhất trong bảng offers.
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(
        string baseSlug,
        long? ignoreId,
        CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var counter = 2;

        while (await _dbContext.Offers
                   .AnyAsync(o => o.Slug == slug && (!ignoreId.HasValue || o.Id != ignoreId.Value), cancellationToken))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    /// <summary>
    /// Chuyển đổi chuỗi tiếng Việt hoặc văn bản thành URL slug thân thiện.
    /// </summary>
    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "offer";

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(clean) ? "offer" : clean;
    }
}

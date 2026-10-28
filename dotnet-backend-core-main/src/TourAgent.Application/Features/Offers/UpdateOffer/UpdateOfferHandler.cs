using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Domain.Common;
using TourAgent.Domain.Offers;

namespace TourAgent.Application.Features.Offers.UpdateOffer;

/// <summary>
/// Handler xử lý cập nhật thông tin gói Offer và đồng bộ danh sách hạng mục Value Stack.
/// </summary>
public sealed class UpdateOfferHandler : ICommandHandler<UpdateOfferCommand, Result<OfferDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo UpdateOfferHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public UpdateOfferHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý cập nhật Offer và lưu lại database.
    /// </summary>
    /// <param name="command">Dữ liệu cập nhật gói Offer.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>OfferDto sau khi được cập nhật.</returns>
    public async Task<Result<OfferDto>> HandleAsync(
        UpdateOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var offer = await _dbContext.Offers
            .Include(o => o.Items)
            .Include(o => o.Travel)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (offer is null)
        {
            return Result.Failure<OfferDto>(
                Error.NotFound("Offer.NotFound", $"Không tìm thấy Offer có Id {command.Id}."));
        }

        // Cập nhật slug nếu có thay đổi
        var baseSlug = !string.IsNullOrWhiteSpace(command.Slug)
            ? Slugify(command.Slug)
            : Slugify(command.Title);

        var uniqueSlug = await GenerateUniqueSlugAsync(baseSlug, offer.Id, cancellationToken);

        // Parse status
        var status = offer.Status;
        if (!string.IsNullOrWhiteSpace(command.Status) &&
            Enum.TryParse<OfferStatus>(command.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var guaranteesJson = command.Guarantees != null
            ? JsonSerializer.Serialize(command.Guarantees)
            : offer.Guarantees;

        var paymentTermsJson = command.PaymentTerms != null
            ? JsonSerializer.Serialize(command.PaymentTerms)
            : offer.PaymentTerms;

        offer.Update(
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

        // Cập nhật Items trong Value Stack
        if (command.Items != null)
        {
            // Xóa các item cũ khỏi DbSet
            var oldItems = await _dbContext.OfferItems
                .Where(i => i.OfferId == offer.Id)
                .ToListAsync(cancellationToken);

            _dbContext.OfferItems.RemoveRange(oldItems);
            offer.ClearItems();

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
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Nạp lại Travel navigation nếu có
        if (offer.TravelId.HasValue && (offer.Travel == null || offer.Travel.Id != offer.TravelId.Value))
        {
            await _dbContext.Tours
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == offer.TravelId.Value, cancellationToken);
        }

        return Result.Success(offer.ToDto());
    }

    /// <summary>
    /// Sinh chuỗi slug duy nhất trong bảng offers, bỏ qua Id hiện tại.
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(
        string baseSlug,
        long ignoreId,
        CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var counter = 2;

        while (await _dbContext.Offers
                   .AnyAsync(o => o.Slug == slug && o.Id != ignoreId, cancellationToken))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    /// <summary>
    /// Chuyển đổi chuỗi tiếng Việt thành URL slug.
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

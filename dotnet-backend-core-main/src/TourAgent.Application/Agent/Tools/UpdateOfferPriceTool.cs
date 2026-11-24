using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI Agent cập nhật tiêu đề hoặc giá ưu đãi (offerPrice) cho gói Grand Slam Offer.
/// Hỗ trợ Cơ chế B (Disambiguation Pattern): tự động phát hiện danh sách trùng khớp và yêu cầu người dùng xác nhận khi có nhiều hơn 1 kết quả.
/// </summary>
public sealed class UpdateOfferPriceTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "offerId": {
                "type": "integer",
                "description": "Mã ID chính xác của gói Grand Slam Offer cần cập nhật nếu đã biết (ví dụ: 1, 2...)."
            },
            "searchKeyword": {
                "type": "string",
                "description": "Tiêu đề hoặc từ khóa nhận diện gói Offer nếu chưa biết ID chính xác (ví dụ: 'Nghỉ Dưỡng', 'Phú Quốc')."
            },
            "newTitle": {
                "type": "string",
                "description": "Tiêu đề mới cho gói Offer (nếu muốn thay đổi tiêu đề)."
            },
            "newOfferPrice": {
                "type": "number",
                "description": "Mức giá ưu đãi mới cho gói Offer tính theo VNĐ (ví dụ: 3500000, 4200000)."
            }
        },
        "required": []
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ cập nhật Offer với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public UpdateOfferPriceTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "update_offer_price";

    /// <inheritdoc />
    public string Description => "Cập nhật tiêu đề hoặc mức giá ưu đãi (offerPrice) của gói Grand Slam Offer. Cung cấp offerId hoặc searchKeyword để tìm kiếm, kèm theo newTitle hoặc newOfferPrice.";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public bool IsAdminOnly => true;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        long? offerId = null;
        string? searchKeyword = null;
        string? newTitle = null;
        decimal? newOfferPrice = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("offerId", out var idProp) && idProp.TryGetInt64(out var parsedId))
                {
                    offerId = parsedId;
                }

                if (root.TryGetProperty("searchKeyword", out var kwProp) && kwProp.ValueKind == JsonValueKind.String)
                {
                    searchKeyword = kwProp.GetString();
                }

                if (root.TryGetProperty("newTitle", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    newTitle = titleProp.GetString();
                }

                if (root.TryGetProperty("newOfferPrice", out var priceProp) && priceProp.TryGetDecimal(out var parsedPrice))
                {
                    newOfferPrice = parsedPrice;
                }
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Dữ liệu tham số JSON không hợp lệ: {ex.Message}"
                });
            }
        }

        // Kiểm tra phải có ít nhất một thông tin cần cập nhật
        if (string.IsNullOrWhiteSpace(newTitle) && (!newOfferPrice.HasValue || newOfferPrice.Value <= 0))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp ít nhất 'newTitle' hoặc 'newOfferPrice' (> 0) để thực hiện cập nhật."
            });
        }

        TourAgent.Domain.Offers.Offer? targetOffer = null;

        // 1. Trường hợp chỉ định rõ offerId
        if (offerId.HasValue && offerId.Value > 0)
        {
            targetOffer = await _dbContext.Offers
                .FirstOrDefaultAsync(o => o.Id == offerId.Value && o.DeletedAt == null, cancellationToken);

            if (targetOffer is null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Không tìm thấy gói Offer nào có mã ID = {offerId.Value}."
                });
            }
        }
        // 2. Trường hợp tìm kiếm theo từ khóa (Áp dụng Cơ chế B: Disambiguation Pattern)
        else if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kwNormalized = searchKeyword.Trim().ToLower();
            var matches = await _dbContext.Offers
                .Where(o => o.DeletedAt == null && (o.Title.ToLower().Contains(kwNormalized) || o.Slug.ToLower().Contains(kwNormalized)))
                .OrderBy(o => o.Id)
                .Take(10)
                .ToListAsync(cancellationToken);

            if (matches.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Không tìm thấy gói Offer nào khớp với từ khóa '{searchKeyword}'."
                });
            }

            if (matches.Count > 1)
            {
                // Cơ chế B: Phát hiện nhiều hơn 1 bản ghi trùng khớp -> Yêu cầu người dùng làm rõ
                var candidates = matches.Select(o => new
                {
                    id = o.Id,
                    title = o.Title,
                    offerPrice = o.OfferPrice,
                    originalPrice = o.OriginalPrice,
                    availableSlots = o.AvailableSlots
                }).ToList();

                return JsonSerializer.Serialize(new
                {
                    success = false,
                    disambiguationNeeded = true,
                    message = $"Tìm thấy {matches.Count} gói Offer khớp với từ khóa '{searchKeyword}'. Vui lòng hỏi lại người dùng để chọn chính xác mã ID gói Offer cần cập nhật.",
                    candidates
                });
            }

            targetOffer = matches[0];
        }
        else
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp ít nhất 'offerId' hoặc 'searchKeyword' để định danh gói Offer cần cập nhật."
            });
        }

        var oldTitle = targetOffer.Title;
        var oldPrice = targetOffer.OfferPrice;

        // Kiểm tra ràng buộc nghiệp vụ: giá ưu đãi không được lớn hơn hoặc bằng giá gốc
        if (newOfferPrice.HasValue && newOfferPrice.Value >= targetOffer.OriginalPrice)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Mức giá ưu đãi mới ({newOfferPrice.Value:N0} VNĐ) không được lớn hơn hoặc bằng giá gốc niêm yết ({targetOffer.OriginalPrice:N0} VNĐ)."
            });
        }

        // Cập nhật tiêu đề nếu có
        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            targetOffer.UpdateTitle(newTitle);
        }

        // Cập nhật giá ưu đãi nếu có
        if (newOfferPrice.HasValue && newOfferPrice.Value > 0)
        {
            targetOffer.UpdatePrice(newOfferPrice.Value);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Đã cập nhật thành công gói Offer [ID: {targetOffer.Id}].",
            updatedOffer = new
            {
                id = targetOffer.Id,
                oldTitle = oldTitle,
                newTitle = targetOffer.Title,
                oldPrice = oldPrice,
                newPrice = targetOffer.OfferPrice,
                originalPrice = targetOffer.OriginalPrice,
                depositAmount = targetOffer.DepositAmount
            }
        });
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI Agent cập nhật tên (tiêu đề) của tour du lịch / sản phẩm (Product/Travel).
/// Hỗ trợ Cơ chế B (Disambiguation Pattern): tự động phát hiện danh sách trùng khớp và yêu cầu xác nhận khi có nhiều hơn 1 kết quả.
/// </summary>
public sealed class UpdateTravelTitleTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "travelId": {
                "type": "integer",
                "description": "Mã ID chính xác của tour du lịch cần cập nhật nếu đã biết (ví dụ: 1, 2...)."
            },
            "searchKeyword": {
                "type": "string",
                "description": "Tên hoặc từ khóa nhận diện tour nếu chưa biết ID chính xác (ví dụ: 'Nhật Bản', 'Hàn Quốc')."
            },
            "newTitle": {
                "type": "string",
                "description": "Tên mới (tiêu đề mới) muốn đặt cho tour du lịch."
            }
        },
        "required": ["newTitle"]
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ cập nhật tên tour với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public UpdateTravelTitleTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "update_travel_title";

    /// <inheritdoc />
    public string Description => "Cập nhật tên (tiêu đề) cho một tour du lịch/sản phẩm (Product/Travel). Yêu cầu truyền travelId hoặc searchKeyword để tìm kiếm tour và newTitle mới.";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        long? travelId = null;
        string? searchKeyword = null;
        string? newTitle = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("travelId", out var idProp) && idProp.TryGetInt64(out var parsedId))
                {
                    travelId = parsedId;
                }

                if (root.TryGetProperty("searchKeyword", out var kwProp) && kwProp.ValueKind == JsonValueKind.String)
                {
                    searchKeyword = kwProp.GetString();
                }

                if (root.TryGetProperty("newTitle", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    newTitle = titleProp.GetString();
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

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Tên mới (newTitle) không được để trống."
            });
        }

        TourAgent.Domain.Tours.Tour? targetTour = null;

        // 1. Trường hợp người dùng hoặc Agent chỉ định rõ travelId
        if (travelId.HasValue && travelId.Value > 0)
        {
            targetTour = await _dbContext.Tours
                .FirstOrDefaultAsync(t => t.Id == travelId.Value, cancellationToken);

            if (targetTour is null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Không tìm thấy tour du lịch nào có mã ID = {travelId.Value}."
                });
            }
        }
        // 2. Trường hợp tìm kiếm theo từ khóa (Áp dụng Cơ chế B: Disambiguation Pattern)
        else if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kwNormalized = searchKeyword.Trim().ToLower();
            var matches = await _dbContext.Tours
                .Where(t => t.IsActive && (t.Name.ToLower().Contains(kwNormalized) || t.Code.ToLower().Contains(kwNormalized)))
                .OrderBy(t => t.Id)
                .Take(10)
                .ToListAsync(cancellationToken);

            if (matches.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Không tìm thấy tour du lịch nào khớp với từ khóa '{searchKeyword}'."
                });
            }

            if (matches.Count > 1)
            {
                // Cơ chế B: Phát hiện nhiều hơn 1 bản ghi trùng khớp -> Yêu cầu người dùng làm rõ
                var candidates = matches.Select(t => new
                {
                    id = t.Id,
                    code = t.Code,
                    name = t.Name,
                    destination = t.Destination,
                    priceFrom = t.PriceFrom
                }).ToList();

                return JsonSerializer.Serialize(new
                {
                    success = false,
                    disambiguationNeeded = true,
                    message = $"Tìm thấy {matches.Count} tour khớp với từ khóa '{searchKeyword}'. Vui lòng hỏi lại người dùng để chọn chính xác mã ID tour cần cập nhật.",
                    candidates
                });
            }

            targetTour = matches[0];
        }
        else
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp ít nhất 'travelId' hoặc 'searchKeyword' để định danh tour du lịch cần cập nhật."
            });
        }

        var oldTitle = targetTour.Name;
        var updateResult = targetTour.UpdateName(newTitle);
        if (updateResult.IsFailure)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = updateResult.Error.Description
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Đã cập nhật tên tour [ID: {targetTour.Id}] từ '{oldTitle}' thành '{targetTour.Name}'.",
            updatedTour = new
            {
                id = targetTour.Id,
                code = targetTour.Code,
                oldName = oldTitle,
                newName = targetTour.Name
            }
        });
    }
}

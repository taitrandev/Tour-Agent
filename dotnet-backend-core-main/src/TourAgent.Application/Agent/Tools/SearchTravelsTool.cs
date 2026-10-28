using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI tra cứu danh sách tour du lịch hiện có trong hệ thống
/// theo các tiêu chí: quốc gia, điểm đến, nơi khởi hành, ngân sách trần hoặc từ khóa tìm kiếm.
/// </summary>
public sealed class SearchTravelsTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "country": {
                "type": "string",
                "description": "Tên quốc gia cần đến (ví dụ: Nhật Bản, Trung Quốc, Hàn Quốc, Singapore, Thái Lan)."
            },
            "destination": {
                "type": "string",
                "description": "Tên địa danh/điểm tham quan cụ thể (ví dụ: Núi Phú Sĩ, Kyoto, Thượng Hải, Trương Gia Giới, Vạn Lý Trường Thành)."
            },
            "departure_city": {
                "type": "string",
                "description": "Thành phố khởi hành (ví dụ: TP.HCM, Hà Nội, Đà Nẵng)."
            },
            "tag": {
                "type": "string",
                "description": "Thẻ phân loại nhu cầu: gia-dinh, le-tet, cao-cap, tiet-kiem, kham-pha, nghi-duong."
            },
            "max_price": {
                "type": "number",
                "description": "Mức ngân sách tối đa mong muốn (VNĐ)."
            },
            "search": {
                "type": "string",
                "description": "Từ khóa tìm kiếm tự do trong tên tour hoặc lộ trình."
            }
        },
        "required": []
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ tìm kiếm tour với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public SearchTravelsTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "search_travels";

    /// <inheritdoc />
    public string Description => "Tra cứu số lượng và danh sách tất cả các tour du lịch hiện có trong hệ thống, hoặc tìm kiếm tour phù hợp theo quốc gia, điểm tham quan, thành phố khởi hành, mức giá trần hoặc thẻ phân loại nhu cầu.";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        string? country = null;
        string? destination = null;
        string? departureCity = null;
        decimal? maxPrice = null;
        string? search = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("country", out var countryProp) && countryProp.ValueKind == JsonValueKind.String)
                {
                    country = countryProp.GetString();
                }

                if (root.TryGetProperty("destination", out var destProp) && destProp.ValueKind == JsonValueKind.String)
                {
                    destination = destProp.GetString();
                }

                if (root.TryGetProperty("departure_city", out var depProp) && depProp.ValueKind == JsonValueKind.String)
                {
                    departureCity = depProp.GetString();
                }

                if (root.TryGetProperty("max_price", out var priceProp) && priceProp.TryGetDecimal(out var parsedPrice))
                {
                    maxPrice = parsedPrice;
                }

                if (root.TryGetProperty("search", out var searchProp) && searchProp.ValueKind == JsonValueKind.String)
                {
                    search = searchProp.GetString();
                }
            }
            catch (JsonException)
            {
                // Khi JSON đối số không hợp lệ, tiếp tục tìm kiếm mặc định
            }
        }

        var query = _dbContext.Tours
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countryNormalized = country.Trim().ToLower();
            query = query.Where(t => t.Country.ToLower().Contains(countryNormalized));
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            var destNormalized = destination.Trim().ToLower();
            query = query.Where(t => t.Destination.ToLower().Contains(destNormalized));
        }

        if (!string.IsNullOrWhiteSpace(departureCity))
        {
            var cityNormalized = departureCity.Trim().ToLower();
            query = query.Where(t => t.DepartureCity.ToLower().Contains(cityNormalized));
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(t => t.PriceFrom <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchNormalized = search.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchNormalized) ||
                t.Code.ToLower().Contains(searchNormalized) ||
                t.Destination.ToLower().Contains(searchNormalized));
        }

        var tours = await query
            .OrderBy(t => t.PriceFrom)
            .Take(10)
            .Select(t => new
            {
                id = t.Id,
                code = t.Code,
                name = t.Name,
                country = t.Country,
                destination = t.Destination,
                departure_city = t.DepartureCity,
                duration_days = t.DurationDays,
                duration_nights = t.DurationNights,
                price_from = t.PriceFrom,
                highlights = t.Highlights
            })
            .ToListAsync(cancellationToken);

        var response = new
        {
            total_found = tours.Count,
            travels = tours,
            message = tours.Count > 0
                ? $"Tìm thấy {tours.Count} tour du lịch phù hợp."
                : "Không tìm thấy tour nào phù hợp với yêu cầu tìm kiếm."
        };

        return JsonSerializer.Serialize(response);
    }
}

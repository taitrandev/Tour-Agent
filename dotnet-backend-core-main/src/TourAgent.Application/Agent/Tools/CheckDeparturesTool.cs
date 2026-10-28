using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI tra cứu các ngày khởi hành thực tế, tình trạng chỗ và bảng giá theo từng đợt bay của một tour.
/// </summary>
public sealed class CheckDeparturesTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "travel_id": {
                "type": "integer",
                "description": "ID của tour cần kiểm tra ngày khởi hành (lấy từ kết quả search_travels)."
            },
            "month": {
                "type": "string",
                "description": "Tháng khởi hành dự kiến theo định dạng YYYY-MM (ví dụ: '2026-10', '2026-11', '2026-12')."
            }
        },
        "required": ["travel_id"]
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ kiểm tra ngày khởi hành.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public CheckDeparturesTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "check_departures";

    /// <inheritdoc />
    public string Description => "Tra cứu danh sách các ngày khởi hành thực tế, giá người lớn, giá trẻ em, phụ thu phòng đơn và tình trạng mở bán của một tour cụ thể.";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        long travelId = 0;
        string? month = null;

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("travel_id", out var idProp) && idProp.TryGetInt64(out var parsedId))
                {
                    travelId = parsedId;
                }

                if (root.TryGetProperty("month", out var monthProp) && monthProp.ValueKind == JsonValueKind.String)
                {
                    month = monthProp.GetString();
                }
            }
            catch (JsonException)
            {
                // Khi JSON lỗi, travelId vẫn là 0
            }
        }

        if (travelId <= 0)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp mã travel_id hợp lệ để kiểm tra lịch khởi hành."
            });
        }

        var tour = await _dbContext.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == travelId, cancellationToken);

        if (tour is null)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Không tìm thấy tour du lịch với ID #{travelId}."
            });
        }

        var query = _dbContext.TourDepartures
            .AsNoTracking()
            .Where(d => d.TourId == travelId);

        if (!string.IsNullOrWhiteSpace(month) && month.Length >= 7 && month.Contains('-'))
        {
            var parts = month.Split('-');
            if (int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var monthNumber))
            {
                query = query.Where(d => d.DepartureDate.Year == year && d.DepartureDate.Month == monthNumber);
            }
        }

        var departures = await query
            .OrderBy(d => d.DepartureDate)
            .Select(d => new
            {
                departure_date = d.DepartureDate.ToString("yyyy-MM-dd"),
                price_adult = d.PriceAdult,
                price_child = d.PriceChild,
                single_supplement = d.SingleSupplement,
                total_slots = d.TotalSlots,
                available_slots = d.AvailableSlots,
                status = d.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        var response = new
        {
            success = true,
            travel_id = tour.Id,
            travel_title = tour.Name,
            total_departures = departures.Count,
            departures,
            message = departures.Count > 0
                ? $"Có {departures.Count} đợt khởi hành thực tế phù hợp."
                : "Hiện chưa có lịch khởi hành cụ thể cho thời gian này."
        };

        return JsonSerializer.Serialize(response);
    }
}

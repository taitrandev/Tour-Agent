using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI lấy thông tin lịch trình chi tiết và điểm tham quan của tour
/// theo từng ngày hoặc toàn bộ chuyến đi.
/// </summary>
public sealed class GetTravelItineraryTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "travel_id": {
                "type": "integer",
                "description": "ID của tour cần xem lịch trình (bắt buộc)."
            },
            "day": {
                "type": "integer",
                "description": "Số thứ tự ngày cụ thể muốn xem chi tiết (ví dụ: 1, 2, 3...). Để trống nếu muốn xem toàn bộ các ngày."
            }
        },
        "required": ["travel_id"]
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ lấy lịch trình tour với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public GetTravelItineraryTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "get_travel_itinerary";

    /// <inheritdoc />
    public string Description => "Lấy thông tin lịch trình chi tiết và danh sách các điểm tham quan của tour (có thể lấy toàn bộ các ngày hoặc một ngày cụ thể).";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        long travelId = 0;
        int? dayFilter = null;

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

                if (root.TryGetProperty("day", out var dayProp) && dayProp.TryGetInt32(out var parsedDay))
                {
                    dayFilter = parsedDay;
                }
            }
            catch (JsonException)
            {
                // Giữ travelId = 0 nếu parse lỗi
            }
        }

        if (travelId <= 0)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp mã travel_id hợp lệ để xem lịch trình."
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

        if (string.IsNullOrWhiteSpace(tour.Itinerary))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Tour '{tour.Name}' hiện chưa có thông tin lịch trình chi tiết."
            });
        }

        try
        {
            using var itineraryDoc = JsonDocument.Parse(tour.Itinerary);
            var root = itineraryDoc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var daysList = new List<JsonElement>();
                foreach (var item in root.EnumerateArray())
                {
                    if (dayFilter.HasValue)
                    {
                        if (item.TryGetProperty("day", out var dProp) && dProp.GetInt32() == dayFilter.Value)
                        {
                            return JsonSerializer.Serialize(new
                            {
                                success = true,
                                travel_id = tour.Id,
                                travel_title = tour.Name,
                                day = item,
                                message = $"Lịch trình chi tiết Ngày {dayFilter.Value} của tour {tour.Name}."
                            });
                        }
                    }
                    else
                    {
                        daysList.Add(item.Clone());
                    }
                }

                if (dayFilter.HasValue)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Tour này không có lịch trình Ngày {dayFilter.Value} (Tổng thời lượng: {tour.DurationDays} ngày)."
                    });
                }

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    travel_id = tour.Id,
                    travel_title = tour.Name,
                    itinerary = daysList,
                    total_days = daysList.Count
                });
            }
        }
        catch (JsonException)
        {
            // Fallback nếu chuỗi itinerary là text thông thường
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            travel_id = tour.Id,
            travel_title = tour.Name,
            raw_itinerary = tour.Itinerary
        });
    }
}

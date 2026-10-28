using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Application.Agent.Tools;

/// <summary>
/// Công cụ cho phép AI tra cứu các điều khoản và chính sách của tour theo chủ đề:
/// giá bao gồm (inclusions), giá không bao gồm (exclusions), chính sách trẻ em (child_policy),
/// quy định thanh toán (payment_policy), hủy đổi & hoàn tiền (cancellation_policy), thủ tục visa (visa_policy), lưu ý (notes).
/// </summary>
public sealed class GetTravelPoliciesTool : IAgentTool
{
    private readonly IAppDbContext _dbContext;

    private static readonly JsonDocument StaticSchema = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "travel_id": {
                "type": "integer",
                "description": "ID của tour cần xem chính sách (bắt buộc)."
            },
            "topic": {
                "type": "string",
                "description": "Chủ đề chính sách cần tra cứu: inclusions, exclusions, child_policy, payment_policy, cancellation_policy, visa_policy, notes, all.",
                "enum": ["all", "inclusions", "exclusions", "child_policy", "payment_policy", "cancellation_policy", "visa_policy", "notes"]
            }
        },
        "required": ["travel_id"]
    }
    """);

    /// <summary>
    /// Khởi tạo công cụ tra cứu chính sách tour với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu ứng dụng.</param>
    public GetTravelPoliciesTool(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public string Name => "get_travel_policies";

    /// <inheritdoc />
    public string Description => "Tra cứu các điều khoản chính sách của tour theo từng chủ đề: giá bao gồm (inclusions), giá không bao gồm (exclusions), chính sách trẻ em (child_policy), thanh toán (payment_policy), hủy tour & hoàn tiền rớt visa (cancellation_policy), thủ tục visa (visa_policy), hoặc lưu ý chung (notes).";

    /// <inheritdoc />
    public JsonDocument ParametersSchema => StaticSchema;

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        long travelId = 0;
        string topic = "all";

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

                if (root.TryGetProperty("topic", out var topicProp) && topicProp.ValueKind == JsonValueKind.String)
                {
                    topic = topicProp.GetString() ?? "all";
                }
            }
            catch (JsonException)
            {
                // Giữ mặc định nếu parse lỗi
            }
        }

        if (travelId <= 0)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Vui lòng cung cấp mã travel_id hợp lệ để xem chính sách tour."
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

        var policyData = new Dictionary<string, object?>();

        if (topic is "all" or "inclusions")
        {
            policyData["inclusions"] = ParseJsonOrRaw(tour.Inclusions);
        }

        if (topic is "all" or "exclusions")
        {
            policyData["exclusions"] = ParseJsonOrRaw(tour.Exclusions);
        }

        if (topic is "all" or "child_policy")
        {
            policyData["child_policy"] = tour.ChildPolicy;
        }

        if (topic is "all" or "payment_policy")
        {
            policyData["payment_policy"] = tour.PaymentPolicy;
        }

        if (topic is "all" or "cancellation_policy")
        {
            policyData["cancellation_policy"] = tour.CancellationPolicy;
        }

        if (topic is "all" or "visa_policy")
        {
            policyData["visa_policy"] = tour.VisaPolicy;
        }

        if (topic is "all" or "notes")
        {
            policyData["notes"] = tour.Highlights;
        }

        var response = new
        {
            success = true,
            travel_id = tour.Id,
            travel_title = tour.Name,
            topic,
            policies = policyData
        };

        return JsonSerializer.Serialize(response);
    }

    /// <summary>
    /// Chuyển đổi chuỗi JSON thô thành đối tượng JsonElement nếu hợp lệ, ngược lại giữ nguyên chuỗi.
    /// </summary>
    /// <param name="rawJson">Chuỗi JSON hoặc văn bản thuần.</param>
    /// <returns>JsonElement hoặc chuỗi nguyên bản.</returns>
    private static object? ParseJsonOrRaw(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return rawJson;
        }
    }
}

namespace TourAgent.Application.Agent.Harness.Guardrails;

using System.Text.Json;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Kết quả của quá trình hậu kiểm kết quả công cụ (Pha 3: Verify & Correct).
/// </summary>
/// <param name="IsVerified">Cờ báo kết quả có thỏa mãn điều kiện hậu kiểm hay không.</param>
/// <param name="ProcessedResultJson">Chuỗi JSON kết quả đã được làm sạch và bổ sung hướng dẫn tự phục hồi.</param>
/// <param name="CorrectionNotes">Ghi chú điều chỉnh nếu có sự sai lệch cần lưu ý.</param>
public sealed record ToolVerificationResult(
    bool IsVerified,
    string ProcessedResultJson,
    string? CorrectionNotes = null
);

/// <summary>
/// Giao diện bộ hậu kiểm kết quả thực thi công cụ (Pha 3: Verify & Correct).
/// </summary>
public interface IPostConditionVerifier
{
    /// <summary>
    /// Kiểm tra tính đúng đắn và chuẩn hóa kết quả do Tool trả về trước khi đưa vào ngữ cảnh LLM.
    /// </summary>
    /// <param name="tool">Công cụ vừa thực thi.</param>
    /// <param name="rawResultJson">Kết quả thô do Tool trả về.</param>
    /// <param name="state">Trạng thái hiện tại của Agent.</param>
    /// <returns>Đối tượng ToolVerificationResult.</returns>
    ToolVerificationResult Verify(IAgentTool tool, string rawResultJson, AgentState state);
}

/// <summary>
/// Cài đặt IPostConditionVerifier mặc định, kiểm tra lỗi nghiệp vụ, bắt lỗi ngoại lệ
/// và tự động bổ sung gợi ý giải thích thân thiện cho LLM.
/// </summary>
public sealed class DefaultPostConditionVerifier : IPostConditionVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc />
    public ToolVerificationResult Verify(IAgentTool tool, string rawResultJson, AgentState state)
    {
        if (string.IsNullOrWhiteSpace(rawResultJson))
        {
            var fallback = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Công cụ '{tool.Name}' trả về kết quả rỗng.",
                recovery_hint = "Hãy thông báo nhẹ nhàng với khách rằng hệ thống đang cập nhật dữ liệu và đề xuất hỗ trợ hướng khác.",
                _guardrail_status = "empty_output"
            }, JsonOptions);
            return new ToolVerificationResult(false, fallback, "Kết quả trả về rỗng.");
        }

        try
        {
            using var doc = JsonDocument.Parse(rawResultJson);
            var root = doc.RootElement;

            // Kiểm tra xem tool có trả về cờ success = false hoặc có thuộc tính error không
            bool hasError = false;
            string? errorMessage = null;

            if (root.TryGetProperty("success", out var successProp) &&
                successProp.ValueKind == JsonValueKind.False)
            {
                hasError = true;
            }

            if (root.TryGetProperty("error", out var errorProp) &&
                errorProp.ValueKind == JsonValueKind.String)
            {
                hasError = true;
                errorMessage = errorProp.GetString();
            }

            if (hasError)
            {
                var correctedObj = new Dictionary<string, object?>
                {
                    ["success"] = false,
                    ["error"] = errorMessage ?? "Thao tác không hoàn tất theo yêu cầu.",
                    ["recovery_hint"] = "Xin lỗi khách hàng lịch sự về sự cố này và hướng dẫn khách cung cấp thêm thông tin hoặc liên hệ chuyên viên.",
                    ["_guardrail_status"] = "corrected"
                };

                return new ToolVerificationResult(false, JsonSerializer.Serialize(correctedObj, JsonOptions), "Tool báo lỗi nghiệp vụ.");
            }

            return new ToolVerificationResult(true, rawResultJson);
        }
        catch (JsonException)
        {
            var fallback = JsonSerializer.Serialize(new
            {
                success = true,
                raw_text = rawResultJson,
                _guardrail_status = "raw_fallback"
            }, JsonOptions);
            return new ToolVerificationResult(true, fallback);
        }
    }
}

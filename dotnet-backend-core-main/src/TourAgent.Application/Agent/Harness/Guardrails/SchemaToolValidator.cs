namespace TourAgent.Application.Agent.Harness.Guardrails;

using System.Text.Json;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Cài đặt IToolValidator thực hiện tiền kiểm tra tham số dựa trên ParametersSchema của IAgentTool.
/// Ngăn chặn các tham số rỗng, sai định dạng JSON hoặc thiếu các thuộc tính bắt buộc (required fields).
/// </summary>
public sealed class SchemaToolValidator : IToolValidator
{
    /// <inheritdoc />
    public ToolValidationResult Validate(IAgentTool tool, string argumentsJson, AgentState state)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            argumentsJson = "{}";
        }

        JsonDocument argumentsDoc;
        try
        {
            argumentsDoc = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException ex)
        {
            return ToolValidationResult.Failure(
                new[] { $"Tham số JSON không hợp lệ: {ex.Message}" },
                $"Hãy sinh lại chuỗi JSON hợp lệ theo đúng schema của công cụ '{tool.Name}'.");
        }

        using (argumentsDoc)
        {
            var rootArgs = argumentsDoc.RootElement;
            if (rootArgs.ValueKind != JsonValueKind.Object)
            {
                return ToolValidationResult.Failure(
                    new[] { "Tham số truyền vào bắt buộc phải là một JSON Object." },
                    $"Hãy truyền tham số dạng object {{ ... }} cho công cụ '{tool.Name}'.");
            }

            var schemaDoc = tool.ParametersSchema;
            if (schemaDoc is null)
            {
                return ToolValidationResult.Success();
            }

            var schemaRoot = schemaDoc.RootElement;
            var errors = new List<string>();

            // 1. Kiểm tra các trường bắt buộc (required properties)
            if (schemaRoot.TryGetProperty("required", out var requiredProp) &&
                requiredProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var reqItem in requiredProp.EnumerateArray())
                {
                    var reqKey = reqItem.GetString();
                    if (!string.IsNullOrWhiteSpace(reqKey))
                    {
                        if (!rootArgs.TryGetProperty(reqKey, out var valElement) ||
                            valElement.ValueKind == JsonValueKind.Null ||
                            valElement.ValueKind == JsonValueKind.Undefined ||
                            (valElement.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(valElement.GetString())))
                        {
                            errors.Add($"Thiếu tham số bắt buộc '{reqKey}'.");
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                var hint = $"Công cụ '{tool.Name}' yêu cầu phải cung cấp đủ các tham số: {string.Join(", ", errors)}. Hãy hỏi lại khách hàng hoặc trích xuất từ ngữ cảnh trước khi gọi lại.";
                return ToolValidationResult.Failure(errors, hint);
            }

            return ToolValidationResult.Success();
        }
    }
}

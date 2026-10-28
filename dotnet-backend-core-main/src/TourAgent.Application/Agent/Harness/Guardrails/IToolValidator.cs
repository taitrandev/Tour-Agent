namespace TourAgent.Application.Agent.Harness.Guardrails;

using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.State;

/// <summary>
/// Kết quả của quá trình tiền kiểm tham số công cụ (Pha 1: Constrain).
/// </summary>
/// <param name="IsValid">Cờ báo tham số có hợp lệ theo Schema và ràng buộc nghiệp vụ hay không.</param>
/// <param name="Errors">Danh sách các lỗi vi phạm phát hiện được.</param>
/// <param name="RecoveryHint">Gợi ý phục hồi gửi kèm để LLM tự sửa sai ở lượt tiếp theo.</param>
public sealed record ToolValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    string? RecoveryHint = null
)
{
    /// <summary>
    /// Tạo kết quả tiền kiểm thành công.
    /// </summary>
    public static ToolValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Tạo kết quả tiền kiểm thất bại kèm danh sách lỗi và gợi ý.
    /// </summary>
    public static ToolValidationResult Failure(IReadOnlyList<string> errors, string? hint = null) =>
        new(false, errors, hint);
}

/// <summary>
/// Giao diện bộ tiền kiểm tham số đầu vào của Tool (Pha 1: Constrain).
/// </summary>
public interface IToolValidator
{
    /// <summary>
    /// Kiểm tra tính hợp lệ của tham số JSON trước khi thực thi Tool.
    /// </summary>
    /// <param name="tool">Đối tượng công cụ cần chạy.</param>
    /// <param name="argumentsJson">Chuỗi JSON tham số do LLM sinh ra.</param>
    /// <param name="state">Trạng thái hiện tại của Agent.</param>
    /// <returns>Đối tượng ToolValidationResult.</returns>
    ToolValidationResult Validate(IAgentTool tool, string argumentsJson, AgentState state);
}

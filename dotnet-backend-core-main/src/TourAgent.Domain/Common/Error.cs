namespace TourAgent.Domain.Common;

/// <summary>
/// Đại diện cho lỗi nghiệp vụ trong hệ thống Domain.
/// Thay thế việc throw Exception cho các trường hợp nghiệp vụ thông thường.
/// </summary>
/// <param name="Code">Mã lỗi định danh duy nhất (ví dụ: 'Tour.InvalidPrice').</param>
/// <param name="Description">Mô tả chi tiết nguyên nhân lỗi.</param>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// Đại diện cho trạng thái không có lỗi.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Lỗi khi giá trị cung cấp bị null.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "Giá trị cung cấp không được phép null.");

    /// <summary>
    /// Lỗi khi không tìm thấy tài nguyên.
    /// </summary>
    public static Error NotFound(string entityName, object id) =>
        new($"{entityName}.NotFound", $"Không tìm thấy {entityName} với mã định danh: '{id}'.");

    /// <summary>
    /// Lỗi xác thực dữ liệu đầu vào không hợp lệ.
    /// </summary>
    public static Error Validation(string code, string description) =>
        new(code, description);

    /// <summary>
    /// Lỗi xung đột dữ liệu (ví dụ trùng lặp mã duy nhất).
    /// </summary>
    public static Error Conflict(string code, string description) =>
        new(code, description);
}

namespace TourAgent.Domain.Settings;

/// <summary>
/// Đại diện cho một mục cấu hình hệ thống (LLM, Telegram, Webhook, v.v.) lưu trong cơ sở dữ liệu.
/// Thiết kế dạng Key-Value để mở rộng linh hoạt mà không cần sửa đổi schema mỗi khi bổ sung tham số.
/// </summary>
public sealed class SystemSetting
{
    /// <summary>
    /// Khóa định danh cấu hình (Ví dụ: 'llm_settings', 'telegram_settings').
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Giá trị cấu hình được lưu trữ dưới dạng chuỗi JSON có cấu trúc.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Thời điểm cập nhật cấu hình lần cuối (UTC).
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Constructor mặc định phục vụ EF Core.
    /// </summary>
    private SystemSetting()
    {
    }

    /// <summary>
    /// Khởi tạo một mục cấu hình hệ thống mới.
    /// </summary>
    /// <param name="key">Khóa định danh cấu hình.</param>
    /// <param name="value">Nội dung JSON của cấu hình.</param>
    public SystemSetting(string key, string value)
    {
        Key = !string.IsNullOrWhiteSpace(key) ? key.Trim() : throw new ArgumentException("Key không được để trống.", nameof(key));
        Value = value ?? string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật giá trị cấu hình mới và ghi nhận thời điểm sửa đổi.
    /// </summary>
    /// <param name="newValue">Chuỗi JSON cấu hình mới.</param>
    public void UpdateValue(string newValue)
    {
        Value = newValue ?? string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}


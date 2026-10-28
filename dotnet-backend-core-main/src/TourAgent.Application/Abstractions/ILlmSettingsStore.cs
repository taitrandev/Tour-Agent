namespace TourAgent.Application.Abstractions;

/// <summary>
/// Đại diện cho cấu hình cổng LLM runtime.
/// </summary>
public sealed class LlmSettings
{
    /// <summary>
    /// Tên driver AI đang sử dụng (mặc định 'deepseek').
    /// </summary>
    public string Driver { get; set; } = "deepseek";

    /// <summary>
    /// Khóa API DeepSeek hoặc nhà cung cấp LLM tương thích OpenAI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Tên mô hình AI được sử dụng.
    /// </summary>
    public string Model { get; set; } = "deepseek-chat";

    /// <summary>
    /// Đường dẫn cơ sở của cổng API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>
    /// Nhiệt độ sáng tạo của mô hình AI (0.0 đến 1.0).
    /// </summary>
    public float Temperature { get; set; } = 0.2f;
}

/// <summary>
/// Giao diện quản lý lưu trữ và nạp cấu hình cổng LLM runtime.
/// </summary>
public interface ILlmSettingsStore
{
    /// <summary>
    /// Lấy bản sao cấu hình LLM hiện tại.
    /// </summary>
    /// <returns>Đối tượng LlmSettings hiện hành.</returns>
    LlmSettings GetSettings();

    /// <summary>
    /// Lấy chuỗi API Key đã được che giấu một phần vì lý do bảo mật (ví dụ: sk-9••••••••e86a).
    /// </summary>
    /// <returns>Chuỗi khóa API đã che một phần.</returns>
    string GetMaskedApiKey();

    /// <summary>
    /// Cập nhật các tham số cấu hình LLM và ghi đè bền vững xuống file JSON.
    /// </summary>
    /// <param name="apiKey">API Key mới (nếu truyền vào thì ghi đè, nếu rỗng thì giữ nguyên key cũ).</param>
    /// <param name="model">Tên Model suy luận AI.</param>
    /// <param name="baseUrl">Địa chỉ Base URL của Gateway.</param>
    /// <param name="temperature">Nhiệt độ sáng tạo.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    Task UpdateSettingsAsync(
        string? apiKey,
        string model,
        string? baseUrl,
        float? temperature,
        CancellationToken cancellationToken = default);
}

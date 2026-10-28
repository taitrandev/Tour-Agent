using System.Text.Json;

namespace TourAgent.Application.Abstractions;

/// <summary>
/// Đại diện cho kết quả phản hồi chuẩn từ Telegram Bot API.
/// </summary>
/// <param name="Ok">Trạng thái thành công hay thất bại.</param>
/// <param name="Description">Mô tả lỗi hoặc chi tiết từ máy chủ Telegram.</param>
/// <param name="Result">Dữ liệu kết quả dạng JSON trả về nếu có.</param>
public sealed record TelegramOperationResult(bool Ok, string? Description = null, JsonElement? Result = null);

/// <summary>
/// Cổng giao tiếp trừu tượng gửi thông điệp, hình ảnh và quản trị kết nối tới Telegram Bot API.
/// Giúp tách rời hoàn toàn tầng nghiệp vụ khỏi thư viện hoặc HTTP client của Telegram.
/// </summary>
public interface ITelegramAdapter
{
    /// <summary>
    /// Gửi tin nhắn văn bản đến người dùng hoặc nhóm chat Telegram.
    /// </summary>
    /// <param name="chatId">Mã định danh phòng chat Telegram.</param>
    /// <param name="text">Nội dung tin nhắn cần gửi.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>True nếu gửi thành công, ngược lại False.</returns>
    Task<bool> SendTextMessageAsync(
        string chatId,
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi ảnh kèm chú thích tới phòng chat Telegram.
    /// </summary>
    /// <param name="chatId">Mã định danh phòng chat Telegram.</param>
    /// <param name="photoUrl">Đường link URL ảnh.</param>
    /// <param name="caption">Nội dung chú thích ảnh.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>True nếu gửi thành công, ngược lại False.</returns>
    Task<bool> SendPhotoAsync(
        string chatId,
        string photoUrl,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin tài khoản Bot (@username, bot ID, first_name) từ Telegram API.
    /// </summary>
    /// <param name="overrideToken">Token ghi đè tạm thời nếu muốn kiểm tra tính hợp lệ trước khi lưu.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả phản hồi chứa JsonElement của bot.</returns>
    Task<TelegramOperationResult> GetMeAsync(
        string? overrideToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin trạng thái Webhook hiện tại từ Telegram API.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả phản hồi chứa trạng thái webhook (URL, pending count, last error).</returns>
    Task<TelegramOperationResult> GetWebhookInfoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đăng ký Webhook URL với Telegram Bot API.
    /// </summary>
    /// <param name="webhookUrl">Đường dẫn URL HTTPS công khai.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả phản hồi thành công hoặc lỗi.</returns>
    Task<TelegramOperationResult> SetWebhookAsync(
        string webhookUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy bỏ Webhook hiện tại trên Telegram Bot API.
    /// </summary>
    /// <param name="dropPendingUpdates">Nếu true sẽ xóa toàn bộ tin nhắn tồn đọng.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả phản hồi thành công hoặc lỗi.</returns>
    Task<TelegramOperationResult> DeleteWebhookAsync(
        bool dropPendingUpdates = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi tin nhắn có kèm bàn phím nút bấm Inline Keyboard (phục vụ duyệt deal Human-in-the-Loop).
    /// </summary>
    /// <param name="chatId">Mã định danh phòng chat Telegram.</param>
    /// <param name="text">Nội dung văn bản tin nhắn.</param>
    /// <param name="inlineKeyboard">Cấu trúc nút bấm dạng ma trận 2 chiều.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả phản hồi từ Telegram.</returns>
    Task<TelegramOperationResult> SendMessageWithInlineKeyboardAsync(
        string chatId,
        string text,
        object inlineKeyboard,
        CancellationToken cancellationToken = default);
}

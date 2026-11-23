using TourAgent.Domain.Common;

namespace TourAgent.Domain.Tours;

/// <summary>
/// Danh mục các lỗi nghiệp vụ liên quan đến Tour và Lịch khởi hành.
/// </summary>
public static class TourErrors
{
    /// <summary>
    /// Lỗi khi mã tour bị bỏ trống hoặc không đúng định dạng.
    /// </summary>
    public static readonly Error EmptyCode =
        Error.Validation("Tour.EmptyCode", "Mã tour (Code) không được để trống.");

    /// <summary>
    /// Lỗi khi tên tour bị để trống.
    /// </summary>
    public static readonly Error EmptyName =
        Error.Validation("Tour.EmptyName", "Tên tour (Name) không được để trống.");

    /// <summary>
    /// Lỗi khi điểm đến của tour bị để trống.
    /// </summary>
    public static readonly Error EmptyDestination =
        Error.Validation("Tour.EmptyDestination", "Điểm đến của tour (Destination) không được để trống.");

    /// <summary>
    /// Lỗi khi số ngày đi tour nhỏ hơn 1.
    /// </summary>
    public static readonly Error InvalidDurationDays =
        Error.Validation("Tour.InvalidDurationDays", "Thời gian tour phải kéo dài ít nhất 1 ngày.");

    /// <summary>
    /// Lỗi khi giá khởi điểm của tour nhỏ hơn hoặc bằng 0.
    /// </summary>
    public static readonly Error InvalidPriceFrom =
        Error.Validation("Tour.InvalidPriceFrom", "Giá sàn khởi điểm (PriceFrom) của tour phải lớn hơn 0.");

    /// <summary>
    /// Lỗi khi không tìm thấy tour theo yêu cầu.
    /// </summary>
    public static Error NotFound(long id) =>
        Error.NotFound("Tour", id);

    /// <summary>
    /// Lỗi khi giá người lớn của đợt bay nhỏ hơn hoặc bằng 0.
    /// </summary>
    public static readonly Error InvalidAdultPrice =
        Error.Validation("TourDeparture.InvalidAdultPrice", "Giá tour cho người lớn phải lớn hơn 0.");

    /// <summary>
    /// Lỗi khi số chỗ trống vượt quá tổng số chỗ mở bán hoặc là số âm.
    /// </summary>
    public static readonly Error InvalidSlots =
        Error.Validation("TourDeparture.InvalidSlots", "Số lượng chỗ còn trống phải từ 0 và không vượt quá tổng số chỗ mở bán.");

    /// <summary>
    /// Lỗi khi tên người đánh giá bị để trống.
    /// </summary>
    public static readonly Error ReviewerNameEmpty =
        Error.Validation("TourReview.ReviewerNameEmpty", "Tên người đánh giá không được để trống.");

    /// <summary>
    /// Lỗi khi nội dung đánh giá bị để trống.
    /// </summary>
    public static readonly Error ReviewContentEmpty =
        Error.Validation("TourReview.ReviewContentEmpty", "Nội dung đánh giá không được để trống.");

    /// <summary>
    /// Lỗi khi điểm số đánh giá không nằm trong khoảng 1 đến 5 sao.
    /// </summary>
    public static readonly Error InvalidRating =
        Error.Validation("TourReview.InvalidRating", "Điểm đánh giá phải từ 1 đến 5 sao.");

    /// <summary>
    /// Lỗi khi không tìm thấy đánh giá.
    /// </summary>
    public static Error ReviewNotFound(long id) =>
        Error.NotFound("TourReview", id);

    /// <summary>
    /// Lỗi khi câu hỏi thường gặp bị để trống.
    /// </summary>
    public static readonly Error FaqQuestionEmpty =
        Error.Validation("TourFaq.FaqQuestionEmpty", "Câu hỏi thường gặp không được để trống.");

    /// <summary>
    /// Lỗi khi câu trả lời FAQ bị để trống.
    /// </summary>
    public static readonly Error FaqAnswerEmpty =
        Error.Validation("TourFaq.FaqAnswerEmpty", "Câu trả lời FAQ không được để trống.");

    /// <summary>
    /// Lỗi khi không tìm thấy câu hỏi thường gặp.
    /// </summary>
    public static Error FaqNotFound(long id) =>
        Error.NotFound("TourFaq", id);
}

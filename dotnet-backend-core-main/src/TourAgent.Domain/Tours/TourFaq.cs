using TourAgent.Domain.Common;

namespace TourAgent.Domain.Tours;

/// <summary>
/// Đại diện cho một câu hỏi thường gặp (FAQ) được giải đáp cho một Tour du lịch cụ thể.
/// </summary>
public sealed class TourFaq : Entity
{
    /// <summary>
    /// Constructor rỗng bắt buộc phục vụ ORM EF Core.
    /// </summary>
    private TourFaq()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng TourFaq với đầy đủ dữ liệu nghiệp vụ.
    /// </summary>
    private TourFaq(
        long id,
        long tourId,
        string question,
        string answer,
        int sortOrder,
        bool isVisible)
        : base(id)
    {
        TourId = tourId;
        Question = question;
        Answer = answer;
        SortOrder = sortOrder;
        IsVisible = isVisible;
    }

    /// <summary>
    /// Mã định danh của Tour sở hữu FAQ này (Foreign Key tới travels.id).
    /// </summary>
    public long TourId { get; private set; }

    /// <summary>
    /// Nội dung câu hỏi thường gặp của khách hàng.
    /// </summary>
    public string Question { get; private set; } = string.Empty;

    /// <summary>
    /// Nội dung câu trả lời, giải đáp chi tiết từ công ty tour.
    /// </summary>
    public string Answer { get; private set; } = string.Empty;

    /// <summary>
    /// Thứ tự ưu tiên sắp xếp hiển thị (số nhỏ hiển thị trước).
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Trạng thái hiển thị công khai trên giao diện người dùng.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Factory method tạo mới một câu hỏi FAQ với đầy đủ kiểm tra bất biến nghiệp vụ.
    /// </summary>
    /// <param name="tourId">Mã định danh của Tour.</param>
    /// <param name="question">Nội dung câu hỏi.</param>
    /// <param name="answer">Nội dung câu trả lời.</param>
    /// <param name="sortOrder">Thứ tự sắp xếp hiển thị.</param>
    /// <param name="isVisible">Trạng thái hiển thị ban đầu.</param>
    /// <param name="id">Mã định danh (nếu có).</param>
    /// <returns>Result chứa TourFaq nếu hợp lệ, ngược lại trả về Failure.</returns>
    public static Result<TourFaq> Create(
        long tourId,
        string question,
        string answer,
        int sortOrder = 0,
        bool isVisible = true,
        long id = 0)
    {
        if (tourId <= 0)
        {
            return Result.Failure<TourFaq>(TourErrors.NotFound(tourId));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            return Result.Failure<TourFaq>(TourErrors.FaqQuestionEmpty);
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            return Result.Failure<TourFaq>(TourErrors.FaqAnswerEmpty);
        }

        return Result.Success(new TourFaq(
            id,
            tourId,
            question.Trim(),
            answer.Trim(),
            sortOrder,
            isVisible));
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị công khai của FAQ.
    /// </summary>
    /// <param name="isVisible">Trạng thái hiển thị mới.</param>
    public void UpdateVisibility(bool isVisible)
    {
        IsVisible = isVisible;
        SetUpdatedAtUtc();
    }
}

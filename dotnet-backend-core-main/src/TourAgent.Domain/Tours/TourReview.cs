using TourAgent.Domain.Common;

namespace TourAgent.Domain.Tours;

/// <summary>
/// Đại diện cho một đánh giá thực tế của khách hàng dành cho một Tour du lịch.
/// </summary>
public sealed class TourReview : Entity
{
    /// <summary>
    /// Constructor rỗng bắt buộc phục vụ ORM EF Core.
    /// </summary>
    private TourReview()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng TourReview với đầy đủ dữ liệu nghiệp vụ.
    /// </summary>
    private TourReview(
        long id,
        long tourId,
        string reviewerName,
        string? reviewerAvatarUrl,
        short rating,
        string content,
        string? travelDate,
        bool isVisible)
        : base(id)
    {
        TourId = tourId;
        ReviewerName = reviewerName;
        ReviewerAvatarUrl = reviewerAvatarUrl;
        Rating = rating;
        Content = content;
        TravelDate = travelDate;
        IsVisible = isVisible;
    }

    /// <summary>
    /// Mã định danh của Tour được đánh giá (Foreign Key tới travels.id).
    /// </summary>
    public long TourId { get; private set; }

    /// <summary>
    /// Tên hoặc biệt danh của khách hàng đánh giá.
    /// </summary>
    public string ReviewerName { get; private set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh đại diện của khách hàng (nếu có).
    /// </summary>
    public string? ReviewerAvatarUrl { get; private set; }

    /// <summary>
    /// Điểm số đánh giá từ 1 đến 5 sao.
    /// </summary>
    public short Rating { get; private set; }

    /// <summary>
    /// Nội dung chia sẻ cảm nghĩ, trải nghiệm tour.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Ngày hoặc mùa khách đã tham gia tour (ví dụ: "Tháng 10/2025").
    /// </summary>
    public string? TravelDate { get; private set; }

    /// <summary>
    /// Trạng thái hiển thị công khai trên giao diện người dùng.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Factory method tạo mới một đánh giá Tour với đầy đủ kiểm tra bất biến nghiệp vụ.
    /// </summary>
    /// <param name="tourId">Mã định danh của Tour.</param>
    /// <param name="reviewerName">Tên khách hàng đánh giá.</param>
    /// <param name="reviewerAvatarUrl">URL ảnh đại diện.</param>
    /// <param name="rating">Điểm số từ 1 đến 5.</param>
    /// <param name="content">Nội dung đánh giá.</param>
    /// <param name="travelDate">Thời điểm trải nghiệm tour.</param>
    /// <param name="isVisible">Trạng thái hiển thị ban đầu.</param>
    /// <param name="id">Mã định danh (nếu có).</param>
    /// <returns>Result chứa TourReview nếu hợp lệ, ngược lại trả về Failure.</returns>
    public static Result<TourReview> Create(
        long tourId,
        string reviewerName,
        string? reviewerAvatarUrl,
        short rating,
        string content,
        string? travelDate,
        bool isVisible = true,
        long id = 0)
    {
        if (tourId <= 0)
        {
            return Result.Failure<TourReview>(TourErrors.NotFound(tourId));
        }

        if (string.IsNullOrWhiteSpace(reviewerName))
        {
            return Result.Failure<TourReview>(TourErrors.ReviewerNameEmpty);
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure<TourReview>(TourErrors.InvalidRating);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<TourReview>(TourErrors.ReviewContentEmpty);
        }

        return Result.Success(new TourReview(
            id,
            tourId,
            reviewerName.Trim(),
            string.IsNullOrWhiteSpace(reviewerAvatarUrl) ? null : reviewerAvatarUrl.Trim(),
            rating,
            content.Trim(),
            string.IsNullOrWhiteSpace(travelDate) ? null : travelDate.Trim(),
            isVisible));
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị công khai của đánh giá.
    /// </summary>
    /// <param name="isVisible">Trạng thái hiển thị mới.</param>
    public void UpdateVisibility(bool isVisible)
    {
        IsVisible = isVisible;
        SetUpdatedAtUtc();
    }
}

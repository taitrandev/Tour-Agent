namespace TourAgent.Application.Agent.Harness.Domain.Travel;

/// <summary>
/// Cổng giao tiếp một cửa truy xuất dữ liệu Tour (Kỹ thuật D1: Tour Gateway).
/// Cách ly tuyệt đối tầng nghiệp vụ Tour và AI Agent khỏi ORM Entity Framework Core.
/// </summary>
public interface ITourGateway
{
    /// <summary>
    /// Tìm kiếm thông tin Tour theo ID định danh duy nhất.
    /// </summary>
    /// <param name="id">Mã ID của Tour.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng TourDto nếu tìm thấy, ngược lại là null.</returns>
    Task<TourDto?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm thông tin Tour theo mã code (ví dụ: JP-TOK-01).
    /// </summary>
    /// <param name="code">Mã code Tour.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng TourDto nếu tìm thấy, ngược lại là null.</returns>
    Task<TourDto?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm danh sách các Tour thỏa mãn tiêu chí lọc.
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm trong tên hoặc điểm đến.</param>
    /// <param name="country">Quốc gia du lịch.</param>
    /// <param name="departureCity">Thành phố khởi hành.</param>
    /// <param name="maxPrice">Mức giá trần (VNĐ).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách TourDto phù hợp.</returns>
    Task<IReadOnlyList<TourDto>> SearchToursAsync(
        string? keyword = null,
        string? country = null,
        string? departureCity = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách các Tour đang ở trạng thái phát hành (Published).
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách tất cả các Tour đang mở bán.</returns>
    Task<IReadOnlyList<TourDto>> GetAllPublishedToursAsync(CancellationToken cancellationToken = default);
}

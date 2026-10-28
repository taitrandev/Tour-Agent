using TourAgent.Domain.Common;

namespace TourAgent.Domain.Tours;

/// <summary>
/// Đại diện cho một đợt khởi hành cụ thể của một Tour du lịch.
/// Quản lý ngày bay, bảng giá chi tiết và số lượng chỗ ngồi còn trống.
/// </summary>
public sealed class TourDeparture : Entity
{
    /// <summary>
    /// Constructor rỗng bắt buộc phục vụ ORM EF Core.
    /// </summary>
    private TourDeparture()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng TourDeparture với đầy đủ dữ liệu nghiệp vụ.
    /// </summary>
    private TourDeparture(
        long id,
        long tourId,
        DateOnly departureDate,
        decimal priceAdult,
        decimal priceChild,
        decimal singleSupplement,
        int totalSlots,
        int availableSlots,
        TourDepartureStatus status)
        : base(id)
    {
        TourId = tourId;
        DepartureDate = departureDate;
        PriceAdult = priceAdult;
        PriceChild = priceChild;
        SingleSupplement = singleSupplement;
        TotalSlots = totalSlots;
        AvailableSlots = availableSlots;
        Status = status;
    }

    /// <summary>
    /// Mã định danh của Tour sở hữu đợt khởi hành này.
    /// </summary>
    public long TourId { get; private set; }

    /// <summary>
    /// Ngày khởi hành cụ thể của đợt bay.
    /// </summary>
    public DateOnly DepartureDate { get; private set; }

    /// <summary>
    /// Giá tour trọn gói cho 1 người lớn (VNĐ).
    /// </summary>
    public decimal PriceAdult { get; private set; }

    /// <summary>
    /// Giá tour trọn gói cho 1 trẻ em (VNĐ).
    /// </summary>
    public decimal PriceChild { get; private set; }

    /// <summary>
    /// Phụ thu phòng đơn cho khách có nhu cầu ở riêng 1 phòng.
    /// </summary>
    public decimal SingleSupplement { get; private set; }

    /// <summary>
    /// Tổng số lượng chỗ ngồi mở bán cho đợt khởi hành này.
    /// </summary>
    public int TotalSlots { get; private set; }

    /// <summary>
    /// Số lượng chỗ ngồi hiện tại còn trống có thể nhận khách.
    /// </summary>
    public int AvailableSlots { get; private set; }

    /// <summary>
    /// Trạng thái mở bán hiện tại của đợt bay.
    /// </summary>
    public TourDepartureStatus Status { get; private set; }

    /// <summary>
    /// Phương thức Factory khởi tạo một đợt khởi hành mới và kiểm tra toàn vẹn nghiệp vụ (Invariants).
    /// </summary>
    /// <param name="tourId">Mã Tour cha sở hữu.</param>
    /// <param name="departureDate">Ngày khởi hành dự kiến.</param>
    /// <param name="priceAdult">Giá cho người lớn.</param>
    /// <param name="priceChild">Giá cho trẻ em.</param>
    /// <param name="singleSupplement">Phụ thu phòng đơn.</param>
    /// <param name="totalSlots">Tổng số lượng vé mở bán.</param>
    /// <param name="availableSlots">Số lượng vé còn trống hiện tại.</param>
    /// <param name="status">Trạng thái mở bán ban đầu.</param>
    /// <param name="id">Mã định danh (mặc định 0 khi tạo mới trước khi lưu DB).</param>
    /// <returns>Result chứa TourDeparture nếu hợp lệ, hoặc lỗi nghiệp vụ nếu dữ liệu sai phạm.</returns>
    public static Result<TourDeparture> Create(
        long tourId,
        DateOnly departureDate,
        decimal priceAdult,
        decimal priceChild,
        decimal singleSupplement,
        int totalSlots,
        int? availableSlots = null,
        TourDepartureStatus status = TourDepartureStatus.Open,
        long id = 0)
    {
        if (priceAdult <= 0)
        {
            return Result.Failure<TourDeparture>(TourErrors.InvalidAdultPrice);
        }

        var actualAvailableSlots = availableSlots ?? totalSlots;

        if (actualAvailableSlots < 0 || actualAvailableSlots > totalSlots || totalSlots <= 0)
        {
            return Result.Failure<TourDeparture>(TourErrors.InvalidSlots);
        }

        var determinedStatus = actualAvailableSlots == 0 ? TourDepartureStatus.Full : status;

        var departure = new TourDeparture(
            id,
            tourId,
            departureDate,
            priceAdult,
            priceChild,
            singleSupplement,
            totalSlots,
            actualAvailableSlots,
            determinedStatus);

        return Result.Success(departure);
    }

    /// <summary>
    /// Cập nhật số chỗ trống khi có khách đặt hoặc giữ chỗ.
    /// </summary>
    /// <param name="numberOfSlots">Số lượng chỗ cần giữ.</param>
    /// <returns>Result thành công hoặc lỗi nếu không đủ chỗ.</returns>
    public Result ReserveSlots(int numberOfSlots)
    {
        if (numberOfSlots <= 0)
        {
            return Result.Failure(Error.Validation("TourDeparture.InvalidReservationCount", "Số chỗ giữ phải lớn hơn 0."));
        }

        if (AvailableSlots < numberOfSlots)
        {
            return Result.Failure(Error.Conflict("TourDeparture.NotEnoughSlots", "Không đủ số lượng chỗ trống để thực hiện giữ chỗ."));
        }

        AvailableSlots -= numberOfSlots;
        if (AvailableSlots == 0)
        {
            Status = TourDepartureStatus.Full;
        }

        SetUpdatedAtUtc();
        return Result.Success();
    }
}

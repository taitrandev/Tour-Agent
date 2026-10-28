using TourAgent.Domain.Common;

namespace TourAgent.Domain.Tours;

/// <summary>
/// Đại diện cho một Sản phẩm Tour Du lịch trong hệ sinh thái AI Travel.
/// Chứa thông tin lộ trình, thời lượng, giá sàn và các bộ chính sách cam kết.
/// </summary>
public sealed class Tour : Entity
{
    private readonly List<TourDeparture> _departures = [];

    /// <summary>
    /// Constructor rỗng bắt buộc phục vụ ORM EF Core.
    /// </summary>
    private Tour()
    {
    }

    /// <summary>
    /// Khởi tạo một đối tượng Tour với đầy đủ thuộc tính cơ bản.
    /// </summary>
    private Tour(
        long id,
        string code,
        string name,
        string destination,
        string country,
        string departureCity,
        int durationDays,
        int durationNights,
        decimal priceFrom,
        string currency,
        string? highlights,
        string? inclusions,
        string? exclusions,
        string? visaPolicy,
        string? cancellationPolicy,
        string? childPolicy,
        string? paymentPolicy,
        string? itinerary,
        bool isActive,
        string? slug = null,
        bool isFeatured = false,
        string? destinationsJson = null,
        string? tagsJson = null)
        : base(id)
    {
        Code = code;
        Name = name;
        Destination = destination;
        Country = country;
        DepartureCity = departureCity;
        DurationDays = durationDays;
        DurationNights = durationNights;
        PriceFrom = priceFrom;
        Currency = currency;
        Highlights = highlights;
        Inclusions = inclusions;
        Exclusions = exclusions;
        VisaPolicy = visaPolicy;
        CancellationPolicy = cancellationPolicy;
        ChildPolicy = childPolicy;
        PaymentPolicy = paymentPolicy;
        Itinerary = itinerary;
        IsActive = isActive;
        Slug = slug ?? code.ToLowerInvariant().Replace(' ', '-');
        IsFeatured = isFeatured;
        DestinationsJson = destinationsJson;
        TagsJson = tagsJson;
    }

    /// <summary>
    /// Đường dẫn tĩnh thân thiện (URL slug duy nhất).
    /// </summary>
    public string? Slug { get; private set; }

    /// <summary>
    /// Đánh dấu tour nổi bật trên trang chủ và landing page.
    /// </summary>
    public bool IsFeatured { get; private set; }

    /// <summary>
    /// Danh sách các địa danh điểm đến (JSON chuỗi mảng).
    /// </summary>
    public string? DestinationsJson { get; private set; }

    /// <summary>
    /// Danh sách thẻ phân loại nhu cầu (JSON chuỗi mảng: gia-dinh, le-tet, cao-cap...).
    /// </summary>
    public string? TagsJson { get; private set; }
    /// Mã định danh tour (ví dụ: 'JP-TOK-6N5D', duy nhất).
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Tên gọi đầy đủ của tour du lịch.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Điểm đến tham quan chính (ví dụ: 'Tokyo - Núi Phú Sĩ - Kyoto').
    /// </summary>
    public string Destination { get; private set; } = string.Empty;

    /// <summary>
    /// Quốc gia diễn ra chuyến đi (ví dụ: 'Japan', 'Vietnam').
    /// </summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Thành phố khởi hành (ví dụ: 'Hà Nội', 'TP. Hồ Chí Minh').
    /// </summary>
    public string DepartureCity { get; private set; } = string.Empty;

    /// <summary>
    /// Tổng số ngày trong hành trình.
    /// </summary>
    public int DurationDays { get; private set; }

    /// <summary>
    /// Tổng số đêm lưu trú trong hành trình.
    /// </summary>
    public int DurationNights { get; private set; }

    /// <summary>
    /// Giá sàn khởi điểm tham khảo của tour (VNĐ).
    /// </summary>
    public decimal PriceFrom { get; private set; }

    /// <summary>
    /// Đơn vị tiền tệ của giá tour (mặc định 'VND').
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// Các điểm nhấn nổi bật, trải nghiệm độc quyền của tour.
    /// </summary>
    public string? Highlights { get; private set; }

    /// <summary>
    /// Các dịch vụ nằm trong danh mục đã bao gồm.
    /// </summary>
    public string? Inclusions { get; private set; }

    /// <summary>
    /// Các dịch vụ không bao gồm hoặc chi phí cá nhân phát sinh.
    /// </summary>
    public string? Exclusions { get; private set; }

    /// <summary>
    /// Quy định và thủ tục làm visa nhập cảnh.
    /// </summary>
    public string? VisaPolicy { get; private set; }

    /// <summary>
    /// Quy định hoàn hủy tour và các điều kiện cam kết.
    /// </summary>
    public string? CancellationPolicy { get; private set; }

    /// <summary>
    /// Chính sách giá vé và dịch vụ dành cho trẻ em theo độ tuổi.
    /// </summary>
    public string? ChildPolicy { get; private set; }

    /// <summary>
    /// Chính sách thanh toán và các đợt đóng tiền tour.
    /// </summary>
    public string? PaymentPolicy { get; private set; }

    /// <summary>
    /// Lịch trình chi tiết các ngày của chuyến đi (định dạng JSON).
    /// </summary>
    public string? Itinerary { get; private set; }

    /// <summary>
    /// Trạng thái mở bán của tour (true: đang bán, false: tạm dừng).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Danh sách các đợt khởi hành theo ngày của tour.
    /// </summary>
    public IReadOnlyCollection<TourDeparture> Departures => _departures.AsReadOnly();

    /// <summary>
    /// Phương thức Factory khởi tạo đối tượng Tour mới kèm theo kiểm tra tính toàn vẹn nghiệp vụ.
    /// </summary>
    /// <param name="code">Mã tour.</param>
    /// <param name="name">Tên tour.</param>
    /// <param name="destination">Điểm đến.</param>
    /// <param name="country">Quốc gia.</param>
    /// <param name="departureCity">Nơi khởi hành.</param>
    /// <param name="durationDays">Số ngày.</param>
    /// <param name="durationNights">Số đêm.</param>
    /// <param name="priceFrom">Giá sàn.</param>
    /// <param name="currency">Đơn vị tiền tệ.</param>
    /// <param name="highlights">Điểm nhấn.</param>
    /// <param name="inclusions">Bao gồm.</param>
    /// <param name="exclusions">Không bao gồm.</param>
    /// <param name="visaPolicy">Chính sách visa.</param>
    /// <param name="cancellationPolicy">Chính sách hủy.</param>
    /// <param name="childPolicy">Chính sách trẻ em.</param>
    /// <param name="paymentPolicy">Chính sách thanh toán.</param>
    /// <param name="itinerary">Lịch trình JSON.</param>
    /// <param name="isActive">Trạng thái bán.</param>
    /// <param name="id">Mã định danh entity.</param>
    /// <returns>Result chứa Tour nếu hợp lệ, hoặc lỗi nghiệp vụ nếu dữ liệu vi phạm.</returns>
    public static Result<Tour> Create(
        string code,
        string name,
        string destination,
        string country,
        string departureCity,
        int durationDays,
        int durationNights,
        decimal priceFrom,
        string currency = "VND",
        string? highlights = null,
        string? inclusions = null,
        string? exclusions = null,
        string? visaPolicy = null,
        string? cancellationPolicy = null,
        string? childPolicy = null,
        string? paymentPolicy = null,
        string? itinerary = null,
        bool isActive = true,
        long id = 0,
        string? slug = null,
        bool isFeatured = false,
        string? destinationsJson = null,
        string? tagsJson = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Tour>(TourErrors.EmptyCode);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tour>(TourErrors.EmptyName);
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            return Result.Failure<Tour>(TourErrors.EmptyDestination);
        }

        if (durationDays < 1)
        {
            return Result.Failure<Tour>(TourErrors.InvalidDurationDays);
        }

        if (priceFrom <= 0)
        {
            return Result.Failure<Tour>(TourErrors.InvalidPriceFrom);
        }

        var tour = new Tour(
            id,
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            destination.Trim(),
            country.Trim(),
            departureCity.Trim(),
            durationDays,
            durationNights,
            priceFrom,
            currency.Trim().ToUpperInvariant(),
            highlights,
            inclusions,
            exclusions,
            visaPolicy,
            cancellationPolicy,
            childPolicy,
            paymentPolicy,
            itinerary,
            isActive,
            slug,
            isFeatured,
            destinationsJson,
            tagsJson);

        return Result.Success(tour);
    }

    /// <summary>
    /// Thêm một đợt khởi hành mới vào danh sách lịch trình của Tour.
    /// </summary>
    /// <param name="departure">Đợt khởi hành cần thêm.</param>
    /// <returns>Result thành công hoặc lỗi nếu ngày khởi hành đã tồn tại.</returns>
    public Result AddDeparture(TourDeparture departure)
    {
        if (departure.TourId != Id)
        {
            return Result.Failure(Error.Validation("Tour.DepartureMismatched", "Mã tour của đợt khởi hành không khớp với Tour hiện tại."));
        }

        if (_departures.Any(d => d.DepartureDate == departure.DepartureDate))
        {
            return Result.Failure(Error.Conflict("Tour.DuplicateDepartureDate", $"Tour đã có đợt khởi hành vào ngày '{departure.DepartureDate}'."));
        }

        _departures.Add(departure);
        SetUpdatedAtUtc();
        return Result.Success();
    }

    /// <summary>
    /// Xóa toàn bộ các đợt khởi hành hiện có để tái đồng bộ.
    /// </summary>
    public void ClearDepartures()
    {
        _departures.Clear();
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của sản phẩm tour du lịch.
    /// </summary>
    public Result Update(
        string name,
        string destination,
        string country,
        string departureCity,
        int durationDays,
        int durationNights,
        decimal priceFrom,
        string? highlights,
        string? inclusions,
        string? exclusions,
        string? visaPolicy,
        string? cancellationPolicy,
        string? childPolicy,
        string? paymentPolicy,
        string? itinerary,
        bool isActive,
        string? slug = null,
        bool isFeatured = false,
        string? destinationsJson = null,
        string? tagsJson = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(TourErrors.EmptyName);
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            return Result.Failure(TourErrors.EmptyDestination);
        }

        if (durationDays < 1)
        {
            return Result.Failure(TourErrors.InvalidDurationDays);
        }

        if (priceFrom <= 0)
        {
            return Result.Failure(TourErrors.InvalidPriceFrom);
        }

        Name = name.Trim();
        Destination = destination.Trim();
        Country = country.Trim();
        DepartureCity = departureCity.Trim();
        DurationDays = durationDays;
        DurationNights = durationNights;
        PriceFrom = priceFrom;
        Highlights = highlights;
        Inclusions = inclusions;
        Exclusions = exclusions;
        VisaPolicy = visaPolicy;
        CancellationPolicy = cancellationPolicy;
        ChildPolicy = childPolicy;
        PaymentPolicy = paymentPolicy;
        Itinerary = itinerary;
        IsActive = isActive;
        if (!string.IsNullOrWhiteSpace(slug)) Slug = slug.Trim();
        IsFeatured = isFeatured;
        DestinationsJson = destinationsJson;
        TagsJson = tagsJson;

        SetUpdatedAtUtc();
        return Result.Success();
    }

    /// <summary>
    /// Kích hoạt trạng thái mở bán của tour.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        SetUpdatedAtUtc();
    }

    /// <summary>
    /// Tạm dừng mở bán tour.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAtUtc();
    }
}

using System.Diagnostics.CodeAnalysis;

namespace TourAgent.Domain.Common;

/// <summary>
/// Đại diện cho kết quả của một thao tác nghiệp vụ (thành công hoặc thất bại).
/// Giúp loại bỏ việc lạm dụng Exception cho luồng xử lý thông thường.
/// </summary>
public class Result
{
    /// <summary>
    /// Khởi tạo một đối tượng Result với trạng thái thành công và mã lỗi tương ứng.
    /// </summary>
    /// <param name="isSuccess">Cờ xác định thao tác thành công hay không.</param>
    /// <param name="error">Lỗi đính kèm nếu thất bại.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Kết quả thành công không thể chứa lỗi.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Kết quả thất bại bắt buộc phải có lỗi.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Cho biết thao tác đã thực hiện thành công hay chưa.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Cho biết thao tác đã thất bại hay không.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Chi tiết lỗi nếu thao tác thất bại. Trả về Error.None nếu thành công.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Tạo một kết quả thành công không có giá trị trả về.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Tạo một kết quả thất bại với lỗi cụ thể.
    /// </summary>
    /// <param name="error">Lỗi gây ra thất bại.</param>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Tạo một kết quả thành công chứa giá trị dữ liệu trả về.
    /// </summary>
    /// <typeparam name="TValue">Kiểu dữ liệu của giá trị trả về.</typeparam>
    /// <param name="value">Giá trị dữ liệu.</param>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>
    /// Tạo một kết quả thất bại chứa kiểu dữ liệu mong đợi.
    /// </summary>
    /// <typeparam name="TValue">Kiểu dữ liệu của giá trị trả về.</typeparam>
    /// <param name="error">Lỗi gây ra thất bại.</param>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Đại diện cho kết quả của một thao tác nghiệp vụ có trả về giá trị dữ liệu.
/// </summary>
/// <typeparam name="TValue">Kiểu dữ liệu của kết quả trả về khi thành công.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Khởi tạo một đối tượng Result generic với giá trị, trạng thái và lỗi tương ứng.
    /// </summary>
    /// <param name="value">Giá trị trả về nếu thành công.</param>
    /// <param name="isSuccess">Cờ xác định thao tác thành công hay không.</param>
    /// <param name="error">Lỗi đính kèm nếu thất bại.</param>
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Lấy giá trị dữ liệu trả về. Ném ngoại lệ nếu truy cập khi thao tác thất bại.
    /// </summary>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Không thể truy cập giá trị của Result khi thất bại. Lỗi: {Error.Code} - {Error.Description}");

    /// <summary>
    /// Toán tử ngầm định chuyển đổi giá trị thành Result thành công.
    /// </summary>
    /// <param name="value">Giá trị cần chuyển đổi.</param>
    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

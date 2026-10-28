namespace TourAgent.Application.Common;

/// <summary>
/// Đại diện cho một câu truy vấn dữ liệu (Read-only query).
/// </summary>
/// <typeparam name="TResponse">Kiểu dữ liệu kết quả trả về của câu truy vấn.</typeparam>
public interface IQuery<out TResponse>
{
}

/// <summary>
/// Xử lý logic nghiệp vụ cho một câu truy vấn IQuery.
/// Được inject trực tiếp vào Minimal API endpoint mà không cần MediatR.
/// </summary>
/// <typeparam name="TQuery">Kiểu câu truy vấn đầu vào.</typeparam>
/// <typeparam name="TResponse">Kiểu dữ liệu kết quả trả về.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Thực thi xử lý câu truy vấn và trả về kết quả tương ứng.
    /// </summary>
    /// <param name="query">Dữ liệu tham số truy vấn.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Đối tượng kết quả kiểu TResponse.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Đại diện cho một lệnh thao tác thay đổi trạng thái (Command).
/// </summary>
/// <typeparam name="TResponse">Kiểu dữ liệu kết quả trả về sau khi thực thi.</typeparam>
public interface ICommand<out TResponse>
{
}

/// <summary>
/// Xử lý logic nghiệp vụ cho một lệnh ICommand.
/// Được inject trực tiếp vào Minimal API endpoint mà không cần MediatR.
/// </summary>
/// <typeparam name="TCommand">Kiểu lệnh đầu vào.</typeparam>
/// <typeparam name="TResponse">Kiểu dữ liệu kết quả trả về.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Thực thi xử lý lệnh và cập nhật dữ liệu.
    /// </summary>
    /// <param name="command">Dữ liệu tham số của lệnh.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Đối tượng kết quả kiểu TResponse.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

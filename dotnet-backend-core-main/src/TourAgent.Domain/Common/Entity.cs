namespace TourAgent.Domain.Common;

/// <summary>
/// Lớp cơ sở trừu tượng cho mọi Entity trong hệ thống Domain.
/// Định danh thực thể dựa trên khóa chính Id.
/// </summary>
/// <typeparam name="TId">Kiểu dữ liệu của khóa chính định danh.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Khởi tạo một Entity với mã định danh cụ thể.
    /// </summary>
    /// <param name="id">Mã định danh duy nhất của thực thể.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Constructor rỗng phục vụ cho ORM (EF Core) khi khởi tạo đối tượng từ cơ sở dữ liệu.
    /// </summary>
    protected Entity()
    {
        Id = default!;
    }

    /// <summary>
    /// Khóa chính định danh duy nhất của thực thể.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Thời điểm thực thể được khởi tạo trong hệ thống (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm thực thể được cập nhật lần cuối trong hệ thống (UTC).
    /// </summary>
    public DateTime? UpdatedAtUtc { get; protected set; }

    /// <summary>
    /// Cập nhật mốc thời gian sửa đổi gần nhất về thời điểm hiện tại (UTC).
    /// </summary>
    public void SetUpdatedAtUtc()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// So sánh hai thực thể dựa trên kiểu thực thể và giá trị của Id.
    /// </summary>
    /// <param name="other">Thực thể khác cần so sánh.</param>
    /// <returns>True nếu cùng kiểu và cùng Id, ngược lại là False.</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// So sánh thực thể với một đối tượng bất kỳ.
    /// </summary>
    /// <param name="obj">Đối tượng cần so sánh.</param>
    /// <returns>True nếu là cùng một thực thể, ngược lại là False.</returns>
    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && Equals(other);

    /// <summary>
    /// Lấy mã băm của thực thể dựa trên Id.
    /// </summary>
    /// <returns>Mã băm nguyên thủy.</returns>
    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id);

    /// <summary>
    /// Toán tử so sánh bằng giữa hai thực thể.
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    /// <summary>
    /// Toán tử so sánh khác nhau giữa hai thực thể.
    /// </summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}

/// <summary>
/// Lớp cơ sở cho Entity sử dụng khóa chính kiểu long (bigint) tương thích với cơ sở dữ liệu.
/// </summary>
public abstract class Entity : Entity<long>
{
    /// <summary>
    /// Khởi tạo Entity với mã định danh long cụ thể.
    /// </summary>
    /// <param name="id">Mã định danh long.</param>
    protected Entity(long id) : base(id)
    {
    }

    /// <summary>
    /// Constructor rỗng cho EF Core.
    /// </summary>
    protected Entity()
    {
    }
}

using Microsoft.EntityFrameworkCore;

namespace TourAgent.Infrastructure.Persistence;

/// <summary>
/// Dịch vụ khởi tạo bảng 'system_settings' tự động trong PostgreSQL/Supabase khi backend khởi động trên Cloud.
/// Đảm bảo tính sẵn sàng của cơ sở dữ liệu mà không cần chạy migration thủ công.
/// </summary>
public static class SystemSettingDataSeeder
{
    /// <summary>
    /// Đảm bảo bảng 'system_settings' tồn tại trong cơ sở dữ liệu PostgreSQL.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public static async Task EnsureSeededAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS system_settings (
                key VARCHAR(100) PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;

        await dbContext.Database.ExecuteSqlRawAsync(createTableSql, cancellationToken);
    }
}


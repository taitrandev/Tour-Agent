using Microsoft.Extensions.Diagnostics.HealthChecks;
using TourAgent.Infrastructure.Persistence;

namespace TourAgent.Infrastructure.Health;

/// <summary>
/// Health Check kiểm tra tính sẵn sàng kết nối tới cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo DatabaseHealthCheck với AppDbContext.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối cơ sở dữ liệu.</param>
    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Kiểm tra khả năng kết nối tới PostgreSQL (gửi ping SELECT 1).
    /// </summary>
    /// <param name="context">Ngữ cảnh kiểm tra sức khỏe.</param>
    /// <param name="cancellationToken">CancellationToken hủy tác vụ nếu quá thời gian.</param>
    /// <returns>Kết quả kiểm tra sức khỏe (Healthy hoặc Unhealthy).</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Kết nối cơ sở dữ liệu PostgreSQL hoạt động tốt.");
            }

            return HealthCheckResult.Unhealthy("Không thể kết nối tới cơ sở dữ liệu PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi ngoại lệ khi kiểm tra kết nối PostgreSQL.", ex);
        }
    }
}


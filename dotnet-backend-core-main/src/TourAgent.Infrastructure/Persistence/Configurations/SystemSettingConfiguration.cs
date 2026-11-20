using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Settings;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ thực thể SystemSetting sang bảng 'system_settings' trong PostgreSQL.
/// </summary>
public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    /// <summary>
    /// Thiết lập cấu trúc cột, khóa chính và giới hạn kích thước trường.
    /// </summary>
    /// <param name="builder">Bộ dựng cấu hình thực thể SystemSetting.</param>
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}


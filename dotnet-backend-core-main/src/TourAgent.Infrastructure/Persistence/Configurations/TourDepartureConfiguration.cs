using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Tours;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity TourDeparture vào bảng 'travel_departures' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class TourDepartureConfiguration : IEntityTypeConfiguration<TourDeparture>
{
    /// <summary>
    /// Định nghĩa chi tiết cấu trúc cột cho thực thể TourDeparture.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của TourDeparture.</param>
    public void Configure(EntityTypeBuilder<TourDeparture> builder)
    {
        builder.ToTable("travel_departures");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.TourId)
            .HasColumnName("travel_id")
            .IsRequired();

        builder.Property(d => d.DepartureDate)
            .HasColumnName("departure_date")
            .IsRequired();

        builder.Property(d => d.PriceAdult)
            .HasColumnName("price_adult")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(d => d.PriceChild)
            .HasColumnName("price_child")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(d => d.SingleSupplement)
            .HasColumnName("single_supplement")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(d => d.TotalSlots)
            .HasColumnName("total_slots")
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(d => d.AvailableSlots)
            .HasColumnName("available_slots")
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                statusEnum => statusEnum.ToString().ToLower(),
                statusString => Enum.Parse<TourDepartureStatus>(statusString, true));

        builder.Property(d => d.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Property(d => d.UpdatedAtUtc)
            .HasColumnName("updated_at");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Tours;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Tour vào bảng 'travels' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    /// <summary>
    /// Định nghĩa chi tiết cấu trúc cột và quan hệ cho thực thể Tour.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của Tour.</param>
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("travels");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Destination)
            .HasColumnName("route")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.DepartureCity)
            .HasColumnName("departure_city")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.DurationDays)
            .HasColumnName("duration_days")
            .IsRequired();

        builder.Property(t => t.DurationNights)
            .HasColumnName("duration_nights")
            .IsRequired();

        builder.Property(t => t.PriceFrom)
            .HasColumnName("price_from")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Ignore(t => t.Currency); // Mặc định VND, không lưu cột riêng nếu bảng travels chưa có

        builder.Property(t => t.Highlights)
            .HasColumnName("notes");

        builder.Property(t => t.Inclusions)
            .HasColumnName("inclusions")
            .HasColumnType("json");

        builder.Property(t => t.Exclusions)
            .HasColumnName("exclusions")
            .HasColumnType("json");

        builder.Property(t => t.VisaPolicy)
            .HasColumnName("visa_policy");

        builder.Property(t => t.CancellationPolicy)
            .HasColumnName("cancellation_policy");

        builder.Property(t => t.ChildPolicy)
            .HasColumnName("child_policy");

        builder.Property(t => t.PaymentPolicy)
            .HasColumnName("payment_policy");

        builder.Property(t => t.Itinerary)
            .HasColumnName("itinerary")
            .HasColumnType("json");

        builder.Property(t => t.IsActive)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                statusBool => statusBool ? "published" : "draft",
                statusString => statusString == "published" || statusString == "active");

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255);

        builder.Property(t => t.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(t => t.DestinationsJson)
            .HasColumnName("destinations")
            .HasColumnType("json");

        builder.Property(t => t.TagsJson)
            .HasColumnName("tags")
            .HasColumnType("json");

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.HasMany(t => t.Departures)
            .WithOne()
            .HasForeignKey(d => d.TourId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

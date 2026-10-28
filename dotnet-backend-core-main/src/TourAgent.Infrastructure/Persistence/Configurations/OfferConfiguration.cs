using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Offers;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Offer vào bảng 'offers' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột, khóa chính, quan hệ và chỉ mục cho thực thể Offer.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của Offer.</param>
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.TravelId).HasColumnName("travel_id");

        builder.Property(o => o.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Tagline)
            .HasColumnName("tagline")
            .HasMaxLength(255);

        builder.Property(o => o.TargetAudience)
            .HasColumnName("target_audience")
            .HasColumnType("text");

        builder.Property(o => o.HeroMechanism)
            .HasColumnName("hero_mechanism")
            .HasMaxLength(255);

        builder.Property(o => o.CorePromise)
            .HasColumnName("core_promise")
            .HasColumnType("text");

        builder.Property(o => o.OriginalPrice)
            .HasColumnName("original_price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(o => o.OfferPrice)
            .HasColumnName("offer_price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(o => o.DepositPercentage)
            .HasColumnName("deposit_percentage")
            .HasDefaultValue(30);

        builder.Property(o => o.DepositAmount)
            .HasColumnName("deposit_amount")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(o => o.DurationDays)
            .HasColumnName("duration_days")
            .HasDefaultValue(3);

        builder.Property(o => o.DurationNights)
            .HasColumnName("duration_nights")
            .HasDefaultValue(2);

        builder.Property(o => o.MaxSlotsPerWeek)
            .HasColumnName("max_slots_per_week")
            .HasDefaultValue(4);

        builder.Property(o => o.AvailableSlots)
            .HasColumnName("available_slots")
            .HasDefaultValue(4);

        builder.Property(o => o.UrgencyHours)
            .HasColumnName("urgency_hours")
            .HasDefaultValue(24);

        builder.Property(o => o.Guarantees)
            .HasColumnName("guarantees")
            .HasColumnType("jsonb");

        builder.Property(o => o.PaymentTerms)
            .HasColumnName("payment_terms")
            .HasColumnType("jsonb");

        builder.Property(o => o.SalesScript)
            .HasColumnName("sales_script")
            .HasColumnType("text");

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<OfferStatus>(v, true))
            .IsRequired();

        builder.Property(o => o.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Ignore(o => o.CreatedAt);
        builder.Ignore(o => o.UpdatedAt);
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");


        // Relationships
        builder.HasOne(o => o.Travel)
            .WithMany()
            .HasForeignKey(o => o.TravelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Offer)
            .HasForeignKey(i => i.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(o => o.Slug).IsUnique();
        builder.HasIndex(o => new { o.Status, o.IsFeatured });
        builder.HasIndex(o => o.TravelId);
    }
}

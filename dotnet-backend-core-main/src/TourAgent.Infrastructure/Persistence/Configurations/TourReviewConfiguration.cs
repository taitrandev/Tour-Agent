using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Tours;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity TourReview vào bảng 'travel_reviews' trong PostgreSQL.
/// </summary>
public sealed class TourReviewConfiguration : IEntityTypeConfiguration<TourReview>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột và quan hệ cho thực thể TourReview.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của TourReview.</param>
    public void Configure(EntityTypeBuilder<TourReview> builder)
    {
        builder.ToTable("travel_reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.TourId)
            .HasColumnName("travel_id")
            .IsRequired();

        builder.Property(r => r.ReviewerName)
            .HasColumnName("reviewer_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.ReviewerAvatarUrl)
            .HasColumnName("reviewer_avatar_url")
            .HasMaxLength(500);

        builder.Property(r => r.Rating)
            .HasColumnName("rating")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.TravelDate)
            .HasColumnName("travel_date")
            .HasMaxLength(50);

        builder.Property(r => r.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.HasIndex(r => r.TourId)
            .HasDatabaseName("idx_travel_reviews_travel_id");
    }
}


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Tours;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity TourFaq vào bảng 'travel_faqs' trong PostgreSQL.
/// </summary>
public sealed class TourFaqConfiguration : IEntityTypeConfiguration<TourFaq>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột và quan hệ cho thực thể TourFaq.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của TourFaq.</param>
    public void Configure(EntityTypeBuilder<TourFaq> builder)
    {
        builder.ToTable("travel_faqs");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.TourId)
            .HasColumnName("travel_id")
            .IsRequired();

        builder.Property(f => f.Question)
            .HasColumnName("question")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(f => f.Answer)
            .HasColumnName("answer")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(f => f.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(f => f.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(f => f.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.HasIndex(f => f.TourId)
            .HasDatabaseName("idx_travel_faqs_travel_id");
    }
}


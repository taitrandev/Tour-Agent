using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Offers;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity OfferItem vào bảng 'offer_items' trong PostgreSQL.
/// </summary>
public sealed class OfferItemConfiguration : IEntityTypeConfiguration<OfferItem>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột và khóa cho thực thể OfferItem.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của OfferItem.</param>
    public void Configure(EntityTypeBuilder<OfferItem> builder)
    {
        builder.ToTable("offer_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.OfferId).HasColumnName("offer_id").IsRequired();

        builder.Property(i => i.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(i => i.Type)
            .HasColumnName("type")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<OfferItemType>(v, true))
            .IsRequired();

        builder.Property(i => i.OriginalValue)
            .HasColumnName("original_value")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0);

        builder.Property(i => i.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(i => i.IsHighlighted)
            .HasColumnName("is_highlighted")
            .HasDefaultValue(false);

        builder.Ignore(i => i.CreatedAt);
        builder.Ignore(i => i.UpdatedAt);
        builder.Property(i => i.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at");


        builder.HasIndex(i => new { i.OfferId, i.Type });
    }
}

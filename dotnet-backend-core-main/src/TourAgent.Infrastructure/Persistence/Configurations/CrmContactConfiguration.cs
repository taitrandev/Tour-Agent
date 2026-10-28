using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Contacts;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity CrmContact vào bảng 'crm_contacts' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class CrmContactConfiguration : IEntityTypeConfiguration<CrmContact>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột, khóa chính, bộ lọc xóa mềm (Soft Delete) và chỉ mục cho thực thể CrmContact.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của CrmContact.</param>
    public void Configure(EntityTypeBuilder<CrmContact> builder)
    {
        builder.ToTable("crm_contacts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(c => c.TelegramChatId)
            .HasColumnName("telegram_chat_id")
            .HasMaxLength(100);

        builder.Property(c => c.TelegramUsername)
            .HasColumnName("telegram_username")
            .HasMaxLength(100);

        builder.Property(c => c.ZaloId)
            .HasColumnName("zalo_id")
            .HasMaxLength(100);

        builder.Property(c => c.Channel)
            .HasColumnName("channel")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("telegram");

        builder.Property(c => c.Stage)
            .HasColumnName("stage")
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(CrmContact.StageProspect);

        builder.Property(c => c.TagsJson)
            .HasColumnName("tags")
            .HasColumnType("jsonb");

        builder.Property(c => c.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(c => c.LastActiveAtUtc)
            .HasColumnName("last_active_at");

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.Property(c => c.DeletedAtUtc)
            .HasColumnName("deleted_at");

        // Bộ lọc toàn cục: tự động loại trừ các bản ghi đã bị xóa mềm
        builder.HasQueryFilter(c => c.DeletedAtUtc == null);

        // Chỉ mục hỗ trợ tìm kiếm nhanh
        builder.HasIndex(c => c.Phone).HasDatabaseName("idx_crm_contacts_phone");
        builder.HasIndex(c => c.Email).HasDatabaseName("idx_crm_contacts_email");
        builder.HasIndex(c => c.TelegramChatId).HasDatabaseName("idx_crm_contacts_telegram_chat_id");
        builder.HasIndex(c => c.ZaloId).HasDatabaseName("idx_crm_contacts_zalo_id");
        builder.HasIndex(c => c.Stage).HasDatabaseName("idx_crm_contacts_stage");
        builder.HasIndex(c => c.LastActiveAtUtc).HasDatabaseName("idx_crm_contacts_last_active");
    }
}

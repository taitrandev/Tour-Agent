namespace TourAgent.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Escalations;

/// <summary>
/// Cấu hình ánh xạ Entity AgentEscalation vào bảng 'agent_escalations' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class AgentEscalationConfiguration : IEntityTypeConfiguration<AgentEscalation>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột, kiểu dữ liệu và mối quan hệ bảng.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của AgentEscalation.</param>
    public void Configure(EntityTypeBuilder<AgentEscalation> builder)
    {
        builder.ToTable("agent_escalations");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(100);

        builder.Property(e => e.CustomerPhone)
            .HasColumnName("customer_phone")
            .HasMaxLength(30);

        builder.Property(e => e.ChatId)
            .HasColumnName("chat_id")
            .HasMaxLength(100);

        builder.Property(e => e.DiscountPercent)
            .HasColumnName("discount_percent");

        builder.Property(e => e.GroupSize)
            .HasColumnName("group_size");

        builder.Property(e => e.TravelId)
            .HasColumnName("travel_id");

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(e => e.AdminMessageId)
            .HasColumnName("admin_message_id");

        builder.Property(e => e.ResolvedBy)
            .HasColumnName("resolved_by")
            .HasMaxLength(100);

        builder.Property(e => e.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.Property(e => e.TravelResolutionMethod)
            .HasColumnName("travel_resolution_method")
            .HasMaxLength(50);

        builder.Property(e => e.TravelResolutionConfidence)
            .HasColumnName("travel_resolution_confidence")
            .HasColumnType("numeric(3,2)");

        builder.Property(e => e.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("json");

        builder.HasOne(e => e.Conversation)
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Travel)
            .WithMany()
            .HasForeignKey(e => e.TravelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

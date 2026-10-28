using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Conversations;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Conversation vào bảng 'conversations' trong PostgreSQL.
/// </summary>
public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột và quan hệ với bảng messages.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của Conversation.</param>
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Channel)
            .HasColumnName("channel")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.ChannelUserId)
            .HasColumnName("channel_user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                statusEnum => statusEnum == ConversationStatus.BotActive ? "bot_active"
                    : statusEnum == ConversationStatus.HumanActive ? "human_active" : "resolved",
                statusString => statusString == "human_active" ? ConversationStatus.HumanActive
                    : statusString == "resolved" ? ConversationStatus.Resolved : ConversationStatus.BotActive);

        builder.Property(c => c.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("json");

        builder.Property(c => c.LastMessageAtUtc)
            .HasColumnName("last_message_at");

        builder.Property(c => c.ContactId)
            .HasColumnName("contact_id");

        builder.Property(c => c.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at");

        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

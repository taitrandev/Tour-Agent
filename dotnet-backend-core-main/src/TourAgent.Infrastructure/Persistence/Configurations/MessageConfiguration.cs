using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Conversations;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Message vào bảng 'messages' trong PostgreSQL.
/// </summary>
public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột và index cho thực thể Message.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của Message.</param>
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasConversion(
                roleEnum => roleEnum.ToString().ToLower(),
                roleString => Enum.Parse<MessageRole>(roleString, true));

        builder.Property(m => m.Content)
            .HasColumnName("content");

        builder.Property(m => m.ToolName)
            .HasColumnName("tool_name")
            .HasMaxLength(100);

        builder.Property(m => m.ToolPayload)
            .HasColumnName("tool_payload")
            .HasColumnType("json");

        builder.Property(m => m.ChannelMessageId)
            .HasColumnName("channel_message_id")
            .HasMaxLength(100);

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Ignore(m => m.UpdatedAtUtc);

        builder.HasIndex(m => m.ChannelMessageId)
            .IsUnique();
    }
}

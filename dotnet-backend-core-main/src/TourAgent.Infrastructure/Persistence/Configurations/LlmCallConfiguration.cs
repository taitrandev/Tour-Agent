using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Conversations;

namespace TourAgent.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity LlmCall vào bảng 'llm_calls' trong PostgreSQL.
/// </summary>
public sealed class LlmCallConfiguration : IEntityTypeConfiguration<LlmCall>
{
    /// <summary>
    /// Định nghĩa cấu trúc cột cho thực thể LlmCall.
    /// </summary>
    /// <param name="builder">Đối tượng EntityTypeBuilder của LlmCall.</param>
    public void Configure(EntityTypeBuilder<LlmCall> builder)
    {
        builder.ToTable("llm_calls");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(l => l.Model)
            .HasColumnName("model")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.PromptTokens)
            .HasColumnName("prompt_tokens")
            .IsRequired();

        builder.Property(l => l.CompletionTokens)
            .HasColumnName("completion_tokens")
            .IsRequired();

        builder.Ignore(l => l.TotalTokens); // Tính toán từ prompt_tokens + completion_tokens

        builder.Property(l => l.CostUsd)
            .HasColumnName("cost_usd")
            .HasColumnType("numeric(10,6)");

        builder.Property(l => l.LatencyMs)
            .HasColumnName("latency_ms")
            .IsRequired();

        builder.Property(l => l.ToolsCalledJson)
            .HasColumnName("tools_called")
            .HasColumnType("json");

        builder.Property(l => l.Provider)
            .HasColumnName("provider")
            .HasMaxLength(30);

        builder.Property(l => l.FinishReason)
            .HasColumnName("finish_reason")
            .HasMaxLength(30);

        builder.Property(l => l.ToolPayloadsJson)
            .HasColumnName("tool_payloads")
            .HasColumnType("json");

        builder.Property(l => l.StatusBarSnapshot)
            .HasColumnName("status_bar_snapshot");

        builder.Property(l => l.StateSnapshotJson)
            .HasColumnName("state_snapshot")
            .HasColumnType("json");

        builder.Property(l => l.CreatedAtUtc)
            .HasColumnName("created_at");

        builder.Ignore(l => l.UpdatedAtUtc);
    }
}

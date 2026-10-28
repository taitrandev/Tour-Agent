namespace TourAgent.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourAgent.Domain.Orders;

/// <summary>
/// Cấu hình ánh xạ Entity TourOrder vào bảng 'tour_orders' trong cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class TourOrderConfiguration : IEntityTypeConfiguration<TourOrder>
{
    /// <summary>
    /// Định nghĩa chi tiết cấu trúc cột, khóa ngoại, bộ lọc Soft Delete và chỉ mục cho bảng tour_orders.
    /// </summary>
    public void Configure(EntityTypeBuilder<TourOrder> builder)
    {
        builder.ToTable("tour_orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.OrderCode)
            .HasColumnName("order_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.ContactId)
            .HasColumnName("contact_id");

        builder.Property(o => o.TravelId)
            .HasColumnName("travel_id");

        builder.Property(o => o.TravelDepartureId)
            .HasColumnName("travel_departure_id");

        builder.Property(o => o.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(o => o.Channel)
            .HasColumnName("channel")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("manual");

        builder.Property(o => o.PaxAdults)
            .HasColumnName("pax_adults")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(o => o.PaxChildren)
            .HasColumnName("pax_children")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(o => o.DepositPercent)
            .HasColumnName("deposit_percent")
            .IsRequired()
            .HasDefaultValue(50);

        builder.Property(o => o.DepositAmount)
            .HasColumnName("deposit_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(o => o.DepositStatus)
            .HasColumnName("deposit_status")
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue("pending");

        builder.Property(o => o.DepositPaidAt)
            .HasColumnName("deposit_paid_at");

        builder.Property(o => o.RemainingAmount)
            .HasColumnName("remaining_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(o => o.RemainingStatus)
            .HasColumnName("remaining_status")
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue("pending");

        builder.Property(o => o.RemainingPaidAt)
            .HasColumnName("remaining_paid_at");

        builder.Property(o => o.PaymentAccountNote)
            .HasColumnName("payment_account_note")
            .HasMaxLength(500);

        builder.Property(o => o.VisaStatus)
            .HasColumnName("visa_status")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("not_required");

        builder.Property(o => o.VisaNotes)
            .HasColumnName("visa_notes");

        builder.Property(o => o.OperatorName)
            .HasColumnName("operator_name")
            .HasMaxLength(255);

        builder.Property(o => o.OperatorBookingRef)
            .HasColumnName("operator_booking_ref")
            .HasMaxLength(100);

        builder.Property(o => o.DepartureDate)
            .HasColumnName("departure_date");

        builder.Property(o => o.TourGuideName)
            .HasColumnName("tour_guide_name")
            .HasMaxLength(255);

        builder.Property(o => o.TourGuidePhone)
            .HasColumnName("tour_guide_phone")
            .HasMaxLength(50);

        builder.Property(o => o.TourGuideNotes)
            .HasColumnName("tour_guide_notes");

        builder.Property(o => o.PreDepartureNotifiedAt)
            .HasColumnName("pre_departure_notified_at");

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("lead_consulting");

        builder.Property(o => o.AiSummary)
            .HasColumnName("ai_summary");

        builder.Property(o => o.Notes)
            .HasColumnName("notes");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(o => o.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Ignore(o => o.CreatedAtUtc);
        builder.Ignore(o => o.UpdatedAtUtc);

        // Relationships
        builder.HasOne(o => o.Contact)
            .WithMany()
            .HasForeignKey(o => o.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Travel)
            .WithMany()
            .HasForeignKey(o => o.TravelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Departure)
            .WithMany()
            .HasForeignKey(o => o.TravelDepartureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Conversation)
            .WithMany()
            .HasForeignKey(o => o.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Global query filter for soft delete
        builder.HasQueryFilter(o => o.DeletedAt == null);

        // Indexes
        builder.HasIndex(o => o.OrderCode).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.VisaStatus);
        builder.HasIndex(o => o.DepartureDate);
    }
}

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Tours;

namespace TourAgent.Infrastructure.Persistence;

/// <summary>
/// DbContext cài đặt chi tiết cho PostgreSQL kết nối vào cơ sở dữ liệu tour_agent_db.
/// Kế thừa DbContext và cài đặt interface IAppDbContext từ tầng Application.
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    /// <summary>
    /// Khởi tạo DbContext với DbContextOptions.
    /// </summary>
    /// <param name="options">Tùy chọn cấu hình DbContext.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Bảng quản lý danh mục Sản phẩm Tour du lịch.
    /// </summary>
    public DbSet<Tour> Tours => Set<Tour>();

    /// <summary>
    /// Bảng quản lý các đợt khởi hành cụ thể của Tour.
    /// </summary>
    public DbSet<TourDeparture> TourDepartures => Set<TourDeparture>();

    /// <summary>
    /// Bảng quản lý các phiên hội thoại tư vấn giữa khách hàng và AI Agent.
    /// </summary>
    public DbSet<Conversation> Conversations => Set<Conversation>();

    /// <summary>
    /// Bảng lưu trữ chi tiết các tin nhắn trong các phiên hội thoại.
    /// </summary>
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>
    /// Bảng theo dõi và kiểm toán các lượt gọi API mô hình LLM.
    /// </summary>
    public DbSet<LlmCall> LlmCalls => Set<LlmCall>();

    /// <summary>
    /// Bảng quản lý các gói Grand Slam Offers.
    /// </summary>
    public DbSet<TourAgent.Domain.Offers.Offer> Offers => Set<TourAgent.Domain.Offers.Offer>();

    /// <summary>
    /// Bảng quản lý các hạng mục trong Bảng xếp chồng giá trị (Value Stack Items).
    /// </summary>
    public DbSet<TourAgent.Domain.Offers.OfferItem> OfferItems => Set<TourAgent.Domain.Offers.OfferItem>();

    /// <summary>
    /// Bảng quản lý hồ sơ khách hàng trung tâm (CRM Contacts 360°).
    /// </summary>
    public DbSet<TourAgent.Domain.Contacts.CrmContact> Contacts => Set<TourAgent.Domain.Contacts.CrmContact>();

    /// <summary>
    /// Bảng quản lý Đơn Hàng Tour & Đường Ống Xử Lý Hồ Sơ Visa (Order & Visa Pipeline).
    /// </summary>
    public DbSet<TourAgent.Domain.Orders.TourOrder> TourOrders => Set<TourAgent.Domain.Orders.TourOrder>();

    /// <summary>
    /// Bảng quản lý các yêu cầu chuyển tiếp tư vấn cho chuyên viên (Human Escalation / Bàn Quyết Định).
    /// </summary>
    public DbSet<TourAgent.Domain.Escalations.AgentEscalation> Escalations => Set<TourAgent.Domain.Escalations.AgentEscalation>();

    /// <summary>
    /// Đăng ký tự động các IEntityTypeConfiguration trong cùng Assembly của Infrastructure.
    /// </summary>
    /// <param name="modelBuilder">Đối tượng ModelBuilder để cấu hình Fluent API.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

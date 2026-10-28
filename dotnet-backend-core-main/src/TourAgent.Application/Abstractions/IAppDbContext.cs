using Microsoft.EntityFrameworkCore;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Abstractions;

/// <summary>
/// Hợp đồng trừu tượng (Abstraction) truy cập cơ sở dữ liệu cho toàn bộ tầng Application.
/// Giúp tầng Application không bị phụ thuộc trực tiếp vào Implementation EF Core ở tầng Infrastructure.
/// </summary>
public interface IAppDbContext
{
    /// <summary>
    /// Bảng quản lý danh mục Sản phẩm Tour du lịch.
    /// </summary>
    DbSet<Tour> Tours { get; }

    /// <summary>
    /// Bảng quản lý các đợt khởi hành cụ thể của Tour.
    /// </summary>
    DbSet<TourDeparture> TourDepartures { get; }

    /// <summary>
    /// Bảng quản lý các phiên hội thoại tư vấn giữa khách hàng và AI Agent.
    /// </summary>
    DbSet<Conversation> Conversations { get; }

    /// <summary>
    /// Bảng lưu trữ chi tiết các tin nhắn trong các phiên hội thoại.
    /// </summary>
    DbSet<Message> Messages { get; }

    /// <summary>
    /// Bảng theo dõi và kiểm toán các lượt gọi API mô hình LLM.
    /// </summary>
    DbSet<LlmCall> LlmCalls { get; }

    /// <summary>
    /// Bảng quản lý các gói Grand Slam Offers.
    /// </summary>
    DbSet<TourAgent.Domain.Offers.Offer> Offers { get; }

    /// <summary>
    /// Bảng quản lý các hạng mục trong Bảng xếp chồng giá trị (Value Stack Items).
    /// </summary>
    DbSet<TourAgent.Domain.Offers.OfferItem> OfferItems { get; }

    /// <summary>
    /// Bảng quản lý hồ sơ khách hàng trung tâm (CRM Contacts 360°).
    /// </summary>
    DbSet<TourAgent.Domain.Contacts.CrmContact> Contacts { get; }

    /// <summary>
    /// Bảng quản lý Đơn Hàng Tour & Đường Ống Xử Lý Hồ Sơ Visa (Order & Visa Pipeline).
    /// </summary>
    DbSet<TourAgent.Domain.Orders.TourOrder> TourOrders { get; }

    /// <summary>
    /// Bảng quản lý các yêu cầu chuyển tiếp tư vấn cho chuyên viên (Human Escalation / Bàn Quyết Định).
    /// </summary>
    DbSet<TourAgent.Domain.Escalations.AgentEscalation> Escalations { get; }

    /// <summary>
    /// Lưu tất cả các thay đổi trên DbContext vào cơ sở dữ liệu PostgreSQL.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Số lượng bản ghi bị tác động trong cơ sở dữ liệu.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

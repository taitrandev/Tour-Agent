namespace TourAgent.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using TourAgent.Domain.Orders;

/// <summary>
/// Dịch vụ khởi tạo cấu trúc bảng và nạp dữ liệu mẫu ban đầu cho Đơn Hàng Tour & Visa Pipeline.
/// </summary>
public static class OrderDataSeeder
{
    /// <summary>
    /// Đảm bảo bảng 'tour_orders' tồn tại trong PostgreSQL, các chỉ mục sẵn sàng,
    /// đồng thời nạp dữ liệu mẫu đa dạng trạng thái nếu bảng đang rỗng.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public static async Task EnsureSeededAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // 1. Tạo bảng tour_orders và các index cần thiết nếu chưa tồn tại
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS tour_orders (
                id BIGSERIAL PRIMARY KEY,
                order_code VARCHAR(50) NOT NULL UNIQUE,
                contact_id BIGINT NULL REFERENCES crm_contacts(id) ON DELETE SET NULL,
                travel_id BIGINT NULL REFERENCES travels(id) ON DELETE SET NULL,
                travel_departure_id BIGINT NULL REFERENCES travel_departures(id) ON DELETE SET NULL,
                conversation_id BIGINT NULL REFERENCES conversations(id) ON DELETE SET NULL,
                channel VARCHAR(50) NOT NULL DEFAULT 'manual',
                pax_adults INTEGER NOT NULL DEFAULT 1,
                pax_children INTEGER NOT NULL DEFAULT 0,
                total_price NUMERIC(14,2) NOT NULL DEFAULT 0,
                deposit_percent INTEGER NOT NULL DEFAULT 50,
                deposit_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
                deposit_status VARCHAR(30) NOT NULL DEFAULT 'pending',
                deposit_paid_at TIMESTAMP WITH TIME ZONE NULL,
                remaining_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
                remaining_status VARCHAR(30) NOT NULL DEFAULT 'pending',
                remaining_paid_at TIMESTAMP WITH TIME ZONE NULL,
                payment_account_note VARCHAR(500) NULL,
                visa_status VARCHAR(50) NOT NULL DEFAULT 'not_required',
                visa_notes TEXT NULL,
                operator_name VARCHAR(255) NULL,
                operator_booking_ref VARCHAR(100) NULL,
                departure_date DATE NULL,
                tour_guide_name VARCHAR(255) NULL,
                tour_guide_phone VARCHAR(50) NULL,
                tour_guide_notes TEXT NULL,
                pre_departure_notified_at TIMESTAMP WITH TIME ZONE NULL,
                status VARCHAR(50) NOT NULL DEFAULT 'lead_consulting',
                ai_summary TEXT NULL,
                notes TEXT NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL,
                updated_at TIMESTAMP WITH TIME ZONE NULL,
                deleted_at TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE INDEX IF NOT EXISTS idx_tour_orders_order_code ON tour_orders (order_code);
            CREATE INDEX IF NOT EXISTS idx_tour_orders_contact_id ON tour_orders (contact_id);
            CREATE INDEX IF NOT EXISTS idx_tour_orders_travel_id ON tour_orders (travel_id);
            CREATE INDEX IF NOT EXISTS idx_tour_orders_status ON tour_orders (status);
            CREATE INDEX IF NOT EXISTS idx_tour_orders_visa_status ON tour_orders (visa_status);
            CREATE INDEX IF NOT EXISTS idx_tour_orders_departure_date ON tour_orders (departure_date);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(createTableSql, cancellationToken);

        // 2. Kiểm tra nếu đã có dữ liệu thì không nạp lại
        if (await dbContext.TourOrders.AnyAsync(cancellationToken))
        {
            return;
        }

        // 3. Lấy thông tin contacts và tours hiện có để liên kết dữ liệu mẫu
        var contacts = await dbContext.Contacts.OrderBy(c => c.Id).Take(5).ToListAsync(cancellationToken);
        var tours = await dbContext.Tours.OrderBy(t => t.Id).Take(5).ToListAsync(cancellationToken);

        long? contact1Id = contacts.Count > 0 ? contacts[0].Id : null;
        long? contact2Id = contacts.Count > 1 ? contacts[1].Id : null;
        long? contact3Id = contacts.Count > 2 ? contacts[2].Id : null;
        long? contact4Id = contacts.Count > 3 ? contacts[3].Id : null;
        long? contact5Id = contacts.Count > 4 ? contacts[4].Id : null;

        long? tour1Id = tours.Count > 0 ? tours[0].Id : null;
        long? tour2Id = tours.Count > 1 ? tours[1].Id : null;
        long? tour3Id = tours.Count > 2 ? tours[2].Id : null;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Order 1: Lead mới đang cần tư vấn gấp (Hot Lead)
        var order1Result = TourOrder.Create(
            contactId: contact1Id,
            travelId: tour1Id,
            travelDepartureId: null,
            conversationId: null,
            channel: "telegram",
            paxAdults: 2,
            paxChildren: 1,
            totalPrice: 65980000m,
            depositPercent: 50,
            departureDate: today.AddDays(25),
            paymentAccountNote: "STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)",
            aiSummary: "Khách hỏi qua Telegram lúc 10h sáng. Yêu cầu tour Nhật ngắm hoa anh đào cho 2 vợ chồng + 1 bé 5 tuổi. Cần visa du lịch tự túc hoặc theo đoàn. Khách sẵn sàng cọc nếu thủ tục visa đơn giản.",
            notes: "Khách VIP, cần gọi điện xác nhận lại trong 30 phút.");

        if (order1Result.IsSuccess)
        {
            var ord = order1Result.Value;
            typeof(TourOrder).GetProperty(nameof(TourOrder.Status))?.SetValue(ord, "lead_consulting");
            typeof(TourOrder).GetProperty(nameof(TourOrder.VisaStatus))?.SetValue(ord, "awaiting_docs");
            dbContext.TourOrders.Add(ord);
        }

        // Order 2: Đang chờ cọc 50%
        var order2Result = TourOrder.Create(
            contactId: contact2Id,
            travelId: tour2Id,
            travelDepartureId: null,
            conversationId: null,
            channel: "web",
            paxAdults: 4,
            paxChildren: 0,
            totalPrice: 119600000m,
            depositPercent: 50,
            departureDate: today.AddDays(18),
            paymentAccountNote: "STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)",
            aiSummary: "Nhóm 4 người lớn đăng ký tour Hàn Quốc Mùa Thu Vàng. Đã chốt hành trình và danh sách phòng 2 đôi. Sales đã gửi mã QR cọc 59.800.000 VNĐ.",
            notes: "Chờ kế toán ngân hàng đối soát biến động số dư.");

        if (order2Result.IsSuccess)
        {
            var ord = order2Result.Value;
            typeof(TourOrder).GetProperty(nameof(TourOrder.Status))?.SetValue(ord, "deposit_pending");
            typeof(TourOrder).GetProperty(nameof(TourOrder.VisaStatus))?.SetValue(ord, "awaiting_docs");
            dbContext.TourOrders.Add(ord);
        }

        // Order 3: Đã cọc 50%, đang làm Visa qua cty du lịch đối tác (Saigontourist)
        var order3Result = TourOrder.Create(
            contactId: contact3Id,
            travelId: tour1Id,
            travelDepartureId: null,
            conversationId: null,
            channel: "telegram",
            paxAdults: 2,
            paxChildren: 0,
            totalPrice: 59800000m,
            depositPercent: 50,
            departureDate: today.AddDays(14),
            paymentAccountNote: "STK: 1903688888 - Techcombank (Đã nhận cọc 29.900.000đ)",
            aiSummary: "Cặp đôi đi trăng mật Tokyo - Phú Sĩ. Đã thu đầy đủ hộ chiếu, sổ tiết kiệm và hợp đồng lao động. Đã nộp sang đối tác Saigontourist để làm hồ sơ visa đoàn.",
            notes: "Đối tác hẹn ngày 05 trả kết quả visa từ Lãnh sự quán.");

        if (order3Result.IsSuccess)
        {
            var ord = order3Result.Value;
            ord.MarkDepositPaid("Chuyển khoản VCB lúc 09:15 - Ref: MBVCB.6291823");
            ord.UpdateVisaStatus("sent_to_operator", "Hồ sơ đã chuyển giao qua bưu cục hỏa tốc cho đại diện Saigontourist", "Saigontourist B2B", "SGT-2026-NRT99");
            dbContext.TourOrders.Add(ord);
        }

        // Order 4: Visa đã ĐẬU! Cần thu nốt 50% còn lại trước ngày khởi hành
        var order4Result = TourOrder.Create(
            contactId: contact4Id,
            travelId: tour3Id,
            travelDepartureId: null,
            conversationId: null,
            channel: "manual",
            paxAdults: 3,
            paxChildren: 0,
            totalPrice: 89700000m,
            depositPercent: 50,
            departureDate: today.AddDays(7),
            paymentAccountNote: "STK: 1903688888 - Techcombank (Đã cọc 44.850.000đ)",
            aiSummary: "Gia đình 3 người. Đã nhận kết quả visa ĐẬU từ Lãnh sự quán. Sales đã thông báo tin vui qua Zalo và nhắc thu nốt 50% còn lại để xuất vé bay.",
            notes: "Khách hẹn chiều nay chuyển khoản nốt số tiền còn lại.");

        if (order4Result.IsSuccess)
        {
            var ord = order4Result.Value;
            ord.MarkDepositPaid("Đã cọc 50% qua Techcombank");
            ord.UpdateVisaStatus("approved", "Lãnh sự quán đã dán tem visa có hiệu lực 3 tháng", "Vietravel Outbound", "VTR-2026-VISA88");
            dbContext.TourOrders.Add(ord);
        }

        // Order 5: Sắp bay trong 3 ngày (T-3) — Đã thanh toán 100%, đã bàn giao Hướng Dẫn Viên
        var order5Result = TourOrder.Create(
            contactId: contact5Id,
            travelId: tour1Id,
            travelDepartureId: null,
            conversationId: null,
            channel: "telegram",
            paxAdults: 2,
            paxChildren: 0,
            totalPrice: 59800000m,
            depositPercent: 50,
            departureDate: today.AddDays(2),
            paymentAccountNote: "STK: 1903688888 - Techcombank (Đã thu đủ 100%)",
            aiSummary: "Khách đã hoàn tất 100% chi phí. Đã xuất vé máy bay Vietnam Airlines và bảo hiểm du lịch quốc tế Chubb. Đã dặn dò lịch tập trung sân bay.",
            notes: "HDV Nguyễn Văn Tuấn phụ trách đoàn. Đã kết bạn Zalo với khách.");

        if (order5Result.IsSuccess)
        {
            var ord = order5Result.Value;
            ord.MarkDepositPaid("Đã cọc đợt 1");
            ord.UpdateVisaStatus("approved", "Đã có visa hợp lệ");
            ord.MarkRemainingPaid();
            ord.MarkT3Notified(
                guideName: "Nguyễn Văn Tuấn",
                guidePhone: "0912.888.999",
                guideNotes: "Tập trung lúc 05:30 sáng tại Cột 10 - Ga đi Quốc tế sân bay Tân Sơn Nhất. Trang phục gọn nhẹ, mang hộ chiếu gốc.");
            dbContext.TourOrders.Add(ord);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

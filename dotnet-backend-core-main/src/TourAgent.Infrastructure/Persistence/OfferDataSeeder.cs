using Microsoft.EntityFrameworkCore;
using TourAgent.Domain.Offers;

namespace TourAgent.Infrastructure.Persistence;

/// <summary>
/// Dịch vụ khởi tạo cấu trúc bảng và nạp dữ liệu mẫu ban đầu cho Grand Slam Offers.
/// </summary>
public static class OfferDataSeeder
{
    /// <summary>
    /// Đảm bảo bảng 'offers' và 'offer_items' tồn tại trong PostgreSQL, đồng thời nạp dữ liệu mẫu nếu chưa có.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public static async Task EnsureSeededAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // 1. Tạo bảng nếu chưa tồn tại
        const string createTablesSql = """
            CREATE TABLE IF NOT EXISTS offers (
                id BIGSERIAL PRIMARY KEY,
                user_id BIGINT NULL,
                travel_id BIGINT NULL,
                title VARCHAR(255) NOT NULL,
                slug VARCHAR(255) UNIQUE NOT NULL,
                tagline VARCHAR(255) NULL,
                target_audience TEXT NULL,
                hero_mechanism VARCHAR(255) NULL,
                core_promise TEXT NULL,
                original_price NUMERIC(12,2) NOT NULL DEFAULT 0,
                offer_price NUMERIC(12,2) NOT NULL DEFAULT 0,
                deposit_percentage SMALLINT NOT NULL DEFAULT 30,
                deposit_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
                duration_days SMALLINT NOT NULL DEFAULT 3,
                duration_nights SMALLINT NOT NULL DEFAULT 2,
                max_slots_per_week SMALLINT NOT NULL DEFAULT 4,
                available_slots SMALLINT NOT NULL DEFAULT 4,
                urgency_hours SMALLINT NOT NULL DEFAULT 24,
                guarantees JSONB NULL,
                payment_terms JSONB NULL,
                sales_script TEXT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'draft',
                is_featured BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP NULL,
                updated_at TIMESTAMP NULL,
                deleted_at TIMESTAMP NULL
            );

            CREATE TABLE IF NOT EXISTS offer_items (
                id BIGSERIAL PRIMARY KEY,
                offer_id BIGINT NOT NULL REFERENCES offers(id) ON DELETE CASCADE,
                name VARCHAR(255) NOT NULL,
                description TEXT NULL,
                type VARCHAR(20) NOT NULL DEFAULT 'core',
                original_value NUMERIC(12,2) NOT NULL DEFAULT 0,
                sort_order SMALLINT NOT NULL DEFAULT 0,
                is_highlighted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP NULL,
                updated_at TIMESTAMP NULL
            );
            """;

        await dbContext.Database.ExecuteSqlRawAsync(createTablesSql, cancellationToken);

        // 2. Nạp dữ liệu mẫu nếu bảng offers đang rỗng
        if (!await dbContext.Offers.AnyAsync(cancellationToken))
        {
            var firstTravel = await dbContext.Tours.FirstOrDefaultAsync(cancellationToken);

            const string guaranteesJson = """
                [
                    {
                        "id": "zero_scam",
                        "title": "Cam kết an toàn cọc 100% (Zero-Scam Guarantee)",
                        "description": "Mọi giao dịch đều có Hợp đồng điện tử có Mã số thuế công ty và chuyển khoản trực tiếp vào Số tài khoản ngân hàng doanh nghiệp."
                    },
                    {
                        "id": "no_hidden_fees",
                        "title": "Cam kết minh bạch từng đồng — Không phụ phí (No Hidden Fees)",
                        "description": "Bảng kê chi phí 2 cột rõ ràng. Nếu đến nơi phát sinh bất kỳ khoản phí dịch vụ ẩn nào nằm ngoài thỏa thuận, Tyler đền bù gấp đôi số tiền phát sinh đó."
                    },
                    {
                        "id": "hassle_free",
                        "title": "Cam kết dịch vụ \"Êm Ru\" (Hassle-Free Guarantee)",
                        "description": "Nếu phòng khách sạn thực tế không đúng hạng phòng và hình ảnh đã cam kết trong hợp đồng, Tyler sẽ đổi ngay khách sạn tương đương hoặc cao cấp hơn hoàn toàn miễn phí trong vòng 60 phút."
                    }
                ]
                """;

            const string paymentTermsJson = """
                [
                    {
                        "phase": 1,
                        "name": "Đợt 1 (Cọc giữ chỗ)",
                        "percentage": 30,
                        "description": "Thanh toán 30% để khóa vé máy bay và phòng khách sạn giá tốt nhất."
                    },
                    {
                        "phase": 2,
                        "name": "Đợt 2 (Hoàn tất)",
                        "percentage": 70,
                        "description": "Thanh toán 70% còn lại trước ngày khởi hành 7 ngày sau khi nhận đầy đủ mã vé và mã phòng."
                    }
                ]
                """;

            const string salesScript = """
                Nghỉ ngơi là để xả stress, chứ không phải để nhận thêm một deadline lên lịch trình.

                Với Gói Combo Nghỉ Dưỡng 4 Chạm Thảnh Thơi (3N2Đ):
                ✅ Bạn không cần thức đêm so sánh vé và phòng.
                ✅ Bạn không lo bị lừa cọc với Hợp đồng Doanh nghiệp minh bạch 100%.
                ✅ Bạn có lịch trình chill theo đúng gu, ngủ đẫy mắt và ăn quán ngon chuẩn vị.
                ✅ Có trợ lý AI và chuyên viên hỗ trợ trực tiếp 24/7 giải quyết mọi phát sinh trong 5 phút.

                Tổng giá trị toàn bộ gói dịch vụ & quà tặng: 6.600.000đ.
                👉 Giá ưu đãi trọn gói hôm nay: Chỉ từ 3.290.000đ / khách (Chỉ cần cọc trước 30% để giữ chỗ).
                """;

            var initialOffer = Offer.Create(
                title: "Gói Combo Nghỉ Dưỡng 4 Chạm Thảnh Thơi (3N2Đ)",
                slug: "combo-nghi-duong-4-cham-thanh-thoi-3n2d",
                originalPrice: 6600000m,
                offerPrice: 3290000m,
                travelId: firstTravel?.Id,
                tagline: "Xách vali lên và đi, an tâm tiền cọc 100%",
                targetAudience: "Dân văn phòng 24 – 35 tuổi, cặp đôi, gia đình trẻ chịu áp lực công việc, muốn có kỳ nghỉ xả stress chuẩn gu mà không muốn mất thời gian tự tìm kiếm hay lo sợ rủi ro lừa cọc.",
                heroMechanism: "Hệ Thống Du Lịch 4 Chạm Thảnh Thơi (AI-Human Hybrid Travel Protocol)",
                corePromise: "Giúp bạn sở hữu trọn vẹn chuyến nghỉ dưỡng 3 ngày 2 đêm chuẩn gu (vé máy bay/xe + khách sạn view đẹp + xe đưa đón + lịch trình chill) chỉ trong 15 phút chốt đơn, loại bỏ 100% rủi ro lừa đảo và không phát sinh bất kỳ phụ phí nào.",
                depositPercentage: 30,
                durationDays: 3,
                durationNights: 2,
                maxSlotsPerWeek: 4,
                availableSlots: 4,
                urgencyHours: 24,
                guarantees: guaranteesJson,
                paymentTerms: paymentTermsJson,
                salesScript: salesScript,
                status: OfferStatus.Active,
                isFeatured: true);

            dbContext.Offers.Add(initialOffer);
            await dbContext.SaveChangesAsync(cancellationToken);

            var items = new (string Name, string Description, OfferItemType Type, decimal Price, bool Highlight)[]
            {
                ("Vé máy bay khứ hồi / Xe VIP Limousine", "Giờ bay đẹp (không bay đêm muộn làm mệt), bao gồm hành lý tiêu chuẩn.", OfferItemType.Core, 2200000m, false),
                ("Khách sạn 3-4 sao trung tâm / view đẹp", "2 đêm nghỉ tại phòng tiêu chuẩn cao, view thung lũng/biển, buffet sáng miễn phí.", OfferItemType.Core, 1800000m, false),
                ("Xe riêng đưa đón sân bay tận nơi", "Tài xế đón đúng giờ, không phải xếp hàng chờ taxi hay bị chặt chém.", OfferItemType.Core, 400000m, false),
                ("[BONUS 1] Bản đồ 10 quán ăn ngon bản địa", "Cẩm nang quán ăn chuẩn vị, giá niêm yết của dân địa phương, loại bỏ rủi ro ăn quán dở.", OfferItemType.Bonus, 500000m, true),
                ("[BONUS 2] Lịch trình May Đo Độc Quyền (PDF)", "Bản lịch trình thiết kế đẹp, chia khung giờ chill chill để forward cho cả nhà duyệt.", OfferItemType.Bonus, 500000m, true),
                ("[BONUS 3] Trợ lý AI Đồng Hành & Cứu Nguy 24/7", "Giải đáp thắc mắc tức thì và xử lý sự cố phát sinh (delay, đổi phòng) trong 5 phút.", OfferItemType.Bonus, 1000000m, true),
                ("[BONUS 4] Bảo hiểm du lịch nội địa trọn gói", "Bảo vệ tối đa quyền lợi y tế và sự cố hành lý trong suốt hành trình.", OfferItemType.Bonus, 200000m, true)
            };

            var sortOrder = 1;
            foreach (var itemData in items)
            {
                var item = OfferItem.Create(
                    offerId: initialOffer.Id,
                    name: itemData.Name,
                    description: itemData.Description,
                    type: itemData.Type,
                    originalValue: itemData.Price,
                    sortOrder: sortOrder++,
                    isHighlighted: itemData.Highlight);

                dbContext.OfferItems.Add(item);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TourAgent.Domain.Contacts;

namespace TourAgent.Infrastructure.Persistence;

/// <summary>
/// Dịch vụ khởi tạo cấu trúc bảng và nạp dữ liệu mẫu ban đầu cho CRM Contacts.
/// </summary>
public static class ContactDataSeeder
{
    /// <summary>
    /// Đảm bảo bảng 'crm_contacts' tồn tại trong PostgreSQL, cột 'contact_id' trên 'conversations' sẵn sàng,
    /// đồng thời nạp dữ liệu mẫu ban đầu nếu bảng đang rỗng.
    /// </summary>
    /// <param name="dbContext">AppDbContext kết nối PostgreSQL.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    public static async Task EnsureSeededAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // 1. Tạo bảng crm_contacts và các index cần thiết nếu chưa tồn tại
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS crm_contacts (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NULL,
                phone VARCHAR(50) NULL,
                email VARCHAR(255) NULL,
                telegram_chat_id VARCHAR(100) NULL,
                telegram_username VARCHAR(100) NULL,
                zalo_id VARCHAR(100) NULL,
                channel VARCHAR(50) NOT NULL DEFAULT 'telegram',
                stage VARCHAR(30) NOT NULL DEFAULT 'prospect',
                tags JSONB NULL,
                notes TEXT NULL,
                last_active_at TIMESTAMP NULL,
                created_at TIMESTAMP NULL,
                updated_at TIMESTAMP NULL,
                deleted_at TIMESTAMP NULL
            );

            CREATE INDEX IF NOT EXISTS idx_crm_contacts_phone ON crm_contacts (phone);
            CREATE INDEX IF NOT EXISTS idx_crm_contacts_email ON crm_contacts (email);
            CREATE INDEX IF NOT EXISTS idx_crm_contacts_telegram_chat_id ON crm_contacts (telegram_chat_id);
            CREATE INDEX IF NOT EXISTS idx_crm_contacts_stage ON crm_contacts (stage);
            CREATE INDEX IF NOT EXISTS idx_crm_contacts_last_active ON crm_contacts (last_active_at);

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name='conversations' AND column_name='contact_id'
                ) THEN
                    ALTER TABLE conversations ADD COLUMN contact_id BIGINT NULL REFERENCES crm_contacts(id) ON DELETE SET NULL;
                END IF;
            END $$;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(createTableSql, cancellationToken);

        // 2. Kiểm tra nếu đã có dữ liệu thì không nạp lại
        if (await dbContext.Contacts.AnyAsync(cancellationToken))
        {
            return;
        }

        // 3. Khởi tạo danh sách các hồ sơ khách hàng mẫu phong phú đại diện cho các kênh và stage
        var sampleContacts = new List<CrmContact>();

        var contact1 = CrmContact.Create(
            name: "Nguyễn Văn An",
            phone: "0912345678",
            email: "an.nguyen@example.com",
            telegramChatId: "7193309999",
            telegramUsername: "an_nguyen_travel",
            channel: "telegram",
            stage: CrmContact.StageHot,
            tagsJson: "[\"khach_vip\", \"tour_nhat\"]",
            notes: "Quan tâm tour Nhật Bản ngắm hoa anh đào cho 4 người lớn vào tháng 4. Đã chốt phương án bay Vietnam Airlines.");
        if (contact1.IsSuccess) sampleContacts.Add(contact1.Value);

        var contact2 = CrmContact.Create(
            name: "Trần Thị Mai",
            phone: "0987654321",
            email: "mai.tran@example.com",
            channel: "web_widget",
            stage: CrmContact.StageWarm,
            tagsJson: "[\"tour_chau_au\", \"can_visa\"]",
            notes: "Hỏi thủ tục làm visa Schengen và lịch khởi hành tour Ý - Pháp mùa hè 2026.");
        if (contact2.IsSuccess) sampleContacts.Add(contact2.Value);

        var contact3 = CrmContact.Create(
            name: "Lê Hoàng Nam",
            phone: null,
            email: null,
            telegramChatId: "6882201111",
            telegramUsername: "nam_traveler",
            channel: "telegram",
            stage: CrmContact.StageProspect,
            tagsJson: "[\"tu_van_gia\", \"tour_thai\"]",
            notes: "Hỏi bảng giá tour Thái Lan Bangkok - Pattaya 5N4Đ bay đêm.");
        if (contact3.IsSuccess) sampleContacts.Add(contact3.Value);

        var contact4 = CrmContact.Create(
            name: "Phạm Minh Đức",
            phone: "0903112233",
            email: "duc.pham@travelbiz.vn",
            zaloId: "zalo_user_9921",
            channel: "zalo",
            stage: CrmContact.StageCustomer,
            tagsJson: "[\"da_thanh_toan\", \"khach_than_thiet\"]",
            notes: "Đã cọc 30% tour Hàn Quốc mùa thu Seoul - Nami khởi hành 15/10/2026.");
        if (contact4.IsSuccess) sampleContacts.Add(contact4.Value);

        var contact5 = CrmContact.Create(
            name: "Đỗ Bảo Trâm",
            phone: "0934567890",
            email: "tram.dobao@gmail.com",
            channel: "manual",
            stage: CrmContact.StageHot,
            tagsJson: "[\"goi_dien_truc_tiep\", \"tour_bali\"]",
            notes: "Khách gọi hotline trực tiếp hỏi tour trăng mật Bali resort 5 sao.");
        if (contact5.IsSuccess) sampleContacts.Add(contact5.Value);

        await dbContext.Contacts.AddRangeAsync(sampleContacts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 4. Nếu có các conversation hiện tại, thử liên kết ngẫu nhiên với contact đầu tiên để hiển thị phiên chat
        var firstContact = sampleContacts.FirstOrDefault();
        if (firstContact != null)
        {
            var firstConv = await dbContext.Conversations.FirstOrDefaultAsync(cancellationToken);
            if (firstConv != null)
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE conversations SET contact_id = {0} WHERE id = {1}",
                    firstContact.Id,
                    firstConv.Id);
            }
        }
    }
}

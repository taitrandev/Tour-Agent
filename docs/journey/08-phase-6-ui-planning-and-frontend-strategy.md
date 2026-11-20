# 🗺️ Kỳ 8: Giai Đoạn 6 — Định Hình Chiến Lược Frontend & Tách Biệt SPA

> **Tài liệu hành trình kỹ thuật (Engineering Journey Document)**  
> **Chủ đề**: Tách rời hoàn toàn giao diện người dùng (Decoupled Single Page Application), bài học từ sự phình to của monolithic view PHP (208KB), quy hoạch kiến trúc Frontend hiện đại với Server-Sent Events (SSE) và quản trị chuyển giao ngữ cảnh (Session Transition).

---

## 💡 1. Bối Cảnh & Động Lực: Bài Học Từ Tệp View PHP 208KB

Khi khảo sát hệ thống cũ viết bằng Laravel, điểm nghẽn lớn nhất trong việc mở rộng và bảo trì chính là tệp `resources/views/welcome.blade.php`. Tệp duy nhất này có dung tích lên tới **208 KB**, gom toàn bộ:
- Mã HTML của 12 phân hệ quản trị (Offers, Tours, Bàn Quyết Định, CRM, Đơn Tour, AI Commander, Kênh Telegram, Zalo...).
- Các khối CSS inline khổng lồ và hàng trăm dòng JavaScript điều khiển AJAX/DOM lồng ghép phức tạp.
- Modal nhập liệu thủ công `#travel-entry-modal` với nhiều cấp con.

> ⚠️ **Nhận thức kỹ thuật quan trọng**: Đây là một Anti-pattern kinh điển (*The God View File*). Nếu trong dự án .NET tiếp tục dùng Razor Pages (`.cshtml`) hay Blazor Server gộp chung vào Backend, ta sẽ lặp lại vết xe đổ này: khó phân tách trách nhiệm (Separation of Concerns), khó chuyển giao dự án, và khiến Backend Controller bị dính chặt với logic hiển thị.

---

## 🎯 2. Các Quyết Định Kiến Trúc Trọng Tâm (Key Architecture Decisions)

### Quyết định 1: Tách rời hoàn toàn Frontend (Decoupled SPA)
- **Phương án**: Frontend được xây dựng dưới dạng ứng dụng độc lập (Single Page Application — Vite + React / Modern UI).
- **Giao tiếp**: Giao tiếp 100% qua chuẩn **REST API** (`/api/v1/tours`) và **Server-Sent Events** (`/api/v1/agent/chat/stream`).
- **Ưu điểm**:
  - Backend .NET 10 trở thành Stateless API Server thuần túy, có thể phục vụ cả Web Client, Mobile App hoặc Telegram Bot.
  - Frontend có thể build và deploy độc lập trên CDN (Vercel, Cloudflare Pages, Nginx), giảm tải hoàn toàn cho máy chủ ứng dụng C#.

### Quyết định 2: Tạm thời loại bỏ Zalo Integration để tập trung giá trị cốt lõi
- Mặc dù hệ thống PHP cũ có tích hợp Zalo qua microservice `zalo-bridge` (Node.js), nhưng để phục vụ mục tiêu **demo phỏng vấn sắc nét và gãy gọn**, việc tích hợp quá nhiều kênh bên thứ ba sẽ làm phân tán nguồn lực.
- Hệ thống tập trung tối ưu vào 2 kênh tiêu biểu nhất:
  1. **Web Client Real-time Chat** (kết nối trực tiếp qua SSE).
  2. **Telegram Omnichannel Bot** (kết nối qua Webhook + `System.Threading.Channels` bất đồng bộ).

### Quyết định 3: Thiết kế Dark Glassmorphism chuẩn cao cấp
- Khác với giao diện quản trị mặc định thô sơ thường thấy ở các đồ án sinh viên (Bootstrap trắng trơn), dự án sử dụng phong cách **Dark Glassmorphism** (kính mờ, nền obsidian, hiệu ứng glow cyan/emerald).
- Mục đích: Tạo ấn tượng thị giác (Wow factor) mạnh mẽ ngay từ 5 giây đầu tiên cho người phỏng vấn.

---

## 🔄 3. Kỹ Thuật Chuyển Giao Ngữ Cảnh Giữa Các Chat Session (Context Engineering)

Một trong những năng lực quan trọng của kỹ sư hiện đại khi cộng tác với AI (Pair Programming) là **quản lý vòng đời ngữ cảnh (Context Management)**.

Khi một phiên làm việc kéo dài (5 giai đoạn backend với hàng trăm lượt hội thoại), cửa sổ ngữ cảnh (Context Window) sẽ đạt ngưỡng bão hòa, dẫn đến tình trạng suy giảm khả năng suy luận (Context Drift / Degradation).

### Chiến lược chuyển giao không mất mát (Lossless Session Transition):
1. **Thiết lập Single Source of Truth (SSOT)**:
   - Các file ổn định, ít biến động: `AI_CONTEXT.md`, `ARCHITECTURE.md`, `DATABASE_SCHEMA.md`.
   - Các file theo dõi tiến độ thời gian thực: `PROGRESS.md`.
   - File đặc tả hợp đồng giao tiếp: `API_ENDPOINTS.md`.
2. **Kế hoạch Blueprint chuẩn bị trước**:
   - Viết sẵn `UI_PLAN.md` quy định rõ cây Component, bảng màu, API mapping.
3. **Mẫu Prompt kích hoạt phiên mới chuẩn xác (`PROMPTS.md`)**:
   - Khi mở phiên chat mới, người dùng chỉ cần copy 1 đoạn prompt ngắn gọn đã được đóng gói sẵn. AI mới sẽ đọc đúng các tài liệu trọng tâm và nhập cuộc làm việc trong < 30 giây.

---

## 🌟 4. Giá Trị Trình Bày Khi Phỏng Vấn (Talking Points for Interviewers)

1. *"Khi di chuyển từ PHP sang .NET, anh xử lý phần UI như thế nào? Có dùng Razor Views hay Blazor không?"*  
   ➔ **Trả lời**: *"Tôi chủ động tách rời hoàn toàn Frontend thành một SPA độc lập. Trong hệ thống PHP cũ, view `welcome.blade.php` bị phình to đến 208KB — một God View gây khó khăn lớn cho việc bảo trì. Việc tách riêng giúp Backend .NET 10 của tôi thuần khiết là REST & Streaming API, bảo vệ nguyên lý Single Responsibility và sẵn sàng cho các kênh client khác."*

2. *"Làm thế nào anh đảm bảo trải nghiệm chat với AI Agent không bị giật lag trên Web?"*  
   ➔ **Trả lời**: *"Tôi thiết kế endpoint `/api/v1/agent/chat/stream` phát dữ liệu theo chuẩn Server-Sent Events (SSE). Thay vì bắt người dùng chờ 2-3 giây để nhận toàn bộ khối text JSON, Frontend nhận từng token hiển thị ngay lập tức cùng với các event badge cho biết Agent đang tra cứu tool nào trong CSDL."*

# 🎨 UI_PLAN — Kế Hoạch Thiết Kế & Xây Dựng Frontend (UI/UX Blueprint)

> **Mục tiêu:** Bản thiết kế kiến trúc và lộ trình triển khai giao diện người dùng (Frontend) độc lập, kết nối với Backend .NET 10 qua REST API và SSE Streaming.  
> **Định vị sản phẩm:** Demo chuyên nghiệp cho phỏng vấn Senior/Tech Lead — Phong cách **Dark Glassmorphism**, trực quan, hiện đại, tối ưu trải nghiệm (SME Scope, không rườm rà enterprise).

---

## 💎 1. Tầm Nhìn & Phong Cách Thiết Kế (Design Philosophy)

* **Kiến trúc tách rời (Decoupled SPA)**:
  - Frontend là Single Page Application hoàn toàn độc lập, giao tiếp với Backend C# qua HTTP REST API và Server-Sent Events (`/api/v1/agent/chat/stream`).
  - Không sử dụng Razor Pages hay ASP.NET MVC Views để đảm bảo tính module hóa và dễ dàng chuyển giao repo.
* **Phong cách thị giác (Dark Glassmorphism Luxury)**:
  - **Nền chủ đạo**: Deep Slate / Obsidian Dark (`#0a0f1d`, `#111827`).
  - **Card & Panels**: Kính mờ siêu thực (Frosted Glass: `rgba(255, 255, 255, 0.03)` kết hợp `backdrop-filter: blur(16px)`, viền gradient mỏng `rgba(255, 255, 255, 0.08)`).
  - **Màu nhấn (Accents)**:
    - Primary: Electric Cyan (`#06b6d4` / `#0ea5e9`) — tượng trưng cho AI & công nghệ.
    - Secondary: Emerald Gold (`#10b981` / `#f59e0b`) — tượng trưng cho du lịch cao cấp và chốt deal.
    - Danger/Escalation: Crimson Rose (`#f43f5e`) — cảnh báo khi cần nhân viên can thiệp.
  - **Micro-interactions**: Chuyển động mượt mà (smooth transitions, pulsing badges khi AI đang gọi tool, token streaming typewriting effect).

---

## 🖼️ 2. Khảo Sát & Kế Thừa Từ Giao Diện PHP Cũ

Từ phân tích màn hình quản trị của phiên bản PHP cũ (`media_1790481938721.png`), hệ thống bao gồm 2 phân hệ người dùng chính:

```
┌────────────────────────────────────────────────────────────────────────┐
│                   HỆ THỐNG GIAO DIỆN AI AGENT TRAVEL                   │
├───────────────────────────────────┬────────────────────────────────────┤
│ 1. CLIENT PORTAL (Du Khách)       │ 2. ADMIN PORTAL (Quản Trị Viên)    │
├───────────────────────────────────┼────────────────────────────────────┤
│ • Danh mục Tour tra cứu & lọc     │ • Dashboard Tổng Quan (Stats Cards)│
│ • Chi tiết hành trình & đợt bay   │ • Quản lý Tour & Form Nhập Liệu    │
│ • Floating AI Consultant Widget   │ • Giám sát Hội thoại & Audit LLM   │
│   (Streaming SSE + Tool badges)   │ • Cấu hình Kênh Telegram Bot       │
└───────────────────────────────────┴────────────────────────────────────┘
```

> ⚠️ **Lưu ý nghiệp vụ**: Phân hệ **Quản Trị Kênh Zalo** tạm thời **SKIP** hoàn toàn trong giai đoạn này để tập trung cao độ vào Web Client và Telegram đã tích hợp ổn định.

---

## 🧱 3. Sơ Đồ Cấu Trúc Component (Component Tree)

```
Frontend Root (App)
├── AppNavbar / TopHeader
│   ├── Logo & Brand Indicator ("AI-Human Hybrid Travel")
│   ├── Environment Badge (".NET 10 Core Online")
│   ├── Live DB & Backend Ping Indicator
│   └── Mode Switcher (Du Khách Tra Cứu ⟷ Quản Trị Hệ Thống)
│
├── 🏖️ VIEW 1: Client Travel Portal
│   ├── HeroSearchBar (Điểm đến, Ngân sách trần, Nơi khởi hành)
│   ├── TourCatalogGrid
│   │   ├── TourFilterSidebar (Quốc gia, Ngày bay, Thời lượng)
│   │   └── TourCardsList (Hiển thị giá, đợt mở bán, highlights)
│   └── 🤖 AiConsultantDrawer / Floating Widget
│       ├── ChatHeader (Trạng thái AI DeepSeek-V3, nút Reset)
│       ├── ChatMessageStream (User bubbles & AI markdown bubbles)
│       ├── ToolExecutionBadge (Hiển thị "Đang tra cứu CSDL...", "Kiểm tra giá...")
│       ├── TokenStatsFooter (Token count, Latency ms)
│       └── ChatInputBox (Hỗ trợ Enter gửi tin, auto-scroll)
│
└── ⚡ VIEW 2: Admin Operations Dashboard
    ├── MetricStatsRow (Tổng số Tour, Đợt bay còn slot, Phiên tư vấn, Chi phí LLM)
    ├── AdminTabNavigation
    │   ├── Tab 1: 📋 Quản Lý Tour (Tour Data Table, Phân trang, Nút Thêm mới)
    │   ├── Tab 2: 👁️ Giám Sát Hội Thoại (Xem log chat khách hàng, Tool calling, Tokens)
    │   └── Tab 3: ✈️ Kênh Telegram (Webhook status, Ping test, Chat ID monitor)
    └── TourEntryModal / Drawer (Form nhập liệu thủ công chuẩn hóa)
```

---

## 🔌 4. Ánh Xạ Với Backend .NET 10 (API Endpoints Mapping)

Mọi tương tác trên Frontend được kết nối trực tiếp với các Endpoints đã xây dựng trong [API_ENDPOINTS.md](API_ENDPOINTS.md):

| Component Frontend | Endpoint Backend | Method | Giao Thức / Mục Đích |
| :--- | :--- | :---: | :--- |
| **TourCatalogGrid** | `/api/v1/tours` | `GET` | Lấy danh sách tour có bộ lọc & phân trang |
| **AiConsultantDrawer** | `/api/v1/agent/chat/stream` | `POST` | Gửi câu hỏi & nhận Server-Sent Events (SSE) theo thời gian thực |
| **AiConsultant (Fallback)**| `/api/v1/agent/chat` | `POST` | HTTP JSON sync khi trình duyệt không hỗ trợ SSE |
| **TelegramTab** | `/api/v1/webhooks/telegram/status` | `GET` | Healthcheck kiểm tra kết nối bot Telegram |
| **TourDetailsModal** | `/api/v1/tours/{id}` *(Proposed)* | `GET` | Xem toàn bộ hành trình ngày 1..N và danh sách đợt khởi hành |
| **TourEntryForm** | `/api/v1/tours` *(Proposed)* | `POST` | Lưu tour mới vào database PostgreSQL |
| **ConversationAuditor** | `/api/v1/conversations` *(Proposed)* | `GET` | Lấy danh sách lịch sử các phiên tư vấn để quản trị |

---

## 🚀 5. Lộ Trình Triển Khai Giao Diện (Phase 6 Implementation Steps)

### 📍 Bước 6.1: Blueprint & Architecture (Đã hoàn thành)
- Hoàn thiện tài liệu `UI_PLAN.md` và `API_ENDPOINTS.md`.
- Thống nhất phong cách Dark Glassmorphism, responsive, không phụ thuộc Razor.

### 📍 Bước 6.2: Khởi Tạo Dự Án Frontend SPA (Thực hiện ở Session mới)
- Khởi tạo project Frontend (Vite + React / TypeScript hoặc Vanilla CSS Component System).
- Thiết lập hệ thống Design Tokens (CSS Variables: màu sắc, hiệu ứng kính blur, typography Google Fonts Outfit/Inter).
- Xây dựng Layout khung sườn: AppHeader, Sidebar điều hướng, Theme wrapper.

### 📍 Bước 6.3: Client Portal & Real-time AI Consultant Widget
- Tích hợp gọi API `GET /api/v1/tours` hiển thị danh sách tour dạng lưới thẻ bài hiện đại.
- Triển khai **AI Consultant Chat Widget**:
  - Kết nối SSE streaming `POST /api/v1/agent/chat/stream`.
  - Hiển thị hiệu ứng gõ chữ thời gian thực (typing animation).
  - Hiển thị Pills/Badges khi Agent kích hoạt tool: `search_travels`, `check_departures`, `get_travel_itinerary`.

### 📍 Bước 6.4: Admin Dashboard & Tour Management
- Dựng bảng điều khiển thống kê (Stats Cards): Số tour mở bán, chi phí LLM, số cuộc trò chuyện.
- Dựng bảng dữ liệu Tour Management (Data Table) với tính năng tìm kiếm, xem nhanh đợt bay.
- Trang giám sát trạng thái Telegram Webhook Bot.

---

## 🎯 6. Tiêu Chuẩn Nghiệm Thu Frontend (Acceptance Criteria)

1. **Thẩm mỹ (Wow Factor)**: Giao diện đạt chuẩn Dark Glassmorphism cao cấp, không dùng màu sắc mặc định thô sơ, font chữ hiện đại.
2. **Khả năng tương tác thời gian thực**: Trải nghiệm chat với AI phản hồi streaming mượt mà từng token, hiển thị minh bạch tool nào đang được kích hoạt.
3. **Độ ổn định & Clean Code**: Cấu trúc thư mục frontend rõ ràng, tách biệt giữa API Services, Components và Hooks/State.
4. **Sẵn sàng cho phỏng vấn**: Có thể demo trực tiếp từ trình duyệt kết nối live vào backend .NET 10 đang chạy trên máy local.

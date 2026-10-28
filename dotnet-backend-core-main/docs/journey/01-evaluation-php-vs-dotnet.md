# 🧭 Hành Trình Chuyển Đổi: Kỳ 1 — Đánh Giá Công Nghệ (PHP 8.4/Laravel 12 sang .NET 10)

> **Mục tiêu tài liệu**: Ghi lại góc nhìn tư duy, các câu hỏi phản biện kỹ thuật và phân tích trade-off khi quyết định chuyển đổi toàn bộ backend từ Laravel sang .NET 10 để chuẩn bị hồ sơ phỏng vấn Senior/Lead C# .NET.

---

## 1. 🎯 Bối Cảnh & Động Lực Chuyển Đổi

### Thực trạng dự án ban đầu:
- Dự án ban đầu là **AI Agent Travel** được viết trên nền tảng **PHP 8.4 / Laravel 12** với kiến trúc module (`app/Core/`), tích hợp DeepSeek V3 qua OpenAI Function Calling, database SQLite/MySQL, chạy 78 Pest tests xanh và PHPStan Level 8.
- Dự án phục vụ mục tiêu kinh doanh tư vấn tour du lịch 24/7 và chốt cọc tour combo chuẩn gu cho dân văn phòng.

### Động lực của tôi (Developer):
- Sau **15 tháng không trực tiếp code C# / .NET**, tôi muốn chứng minh năng lực **vibe coding**, khả năng thích ứng công nghệ đỉnh cao và làm chủ hệ sinh thái **.NET 10 mới nhất**.
- Tôi muốn sở hữu một ứng dụng thực chiến chuẩn mực (Production-ready) với kiến trúc sạch, có cơ chế kiểm thử kiến trúc tự động, sẵn sàng demo trực tiếp khi phỏng vấn để thuyết phục nhà tuyển dụng rằng: *"Dù ngắt quãng 15 tháng, tư duy kiến trúc và tốc độ làm chủ công nghệ mới của tôi vẫn vượt trội"*.

---

## 2. ❓ Câu Hỏi Đặt Ra Cho AI Assistant (Tư Duy Phản Biện Ban Đầu)

Trước khi bắt tay vào code bất kỳ dòng nào, tôi không yêu cầu AI viết code ngay mà yêu cầu phân tích thấu đáo các khía cạnh:
1. *Tất cả các tính năng cốt lõi (Module Engine, Hook system, Auth, AI Agent Loop, Guardrails, Queue) có chuyển đổi 100% được sang .NET Core không?*
2. *Ưu điểm và nhược điểm khi chuyển sang .NET Core là gì?*
3. *Có bất kỳ công cụ hoặc thư viện nào bên PHP có mà bên .NET Core bị giới hạn không?*
4. *Trade-off (được gì và mất gì) khi quyết định chuyển đổi ngay lúc này?*

---

## 3. ⚖️ Bảng Ánh Xạ Tính Năng Cốt Lõi (Laravel ➔ .NET 10)

| Thành phần trong Laravel Core | Tương đương trong .NET 10 | Đánh giá & Khả năng tương thích |
| :--- | :--- | :--- |
| **Module Engine (`app/Core/`)** (`BaseModuleServiceProvider`, auto-load routes/migrations) | **Modular ASP.NET Core** (Class Libraries theo Clean Architecture + Extension methods) | **100% Tương thích**. .NET quản lý Assembly và Dependency Injection chặt chẽ hơn. |
| **Hook System (`do_action`, `apply_filters`)** (WordPress style) | **Domain Events / In-Memory Channel Event Dispatcher** | **Vượt trội hơn**. Thay thế hook chuỗi lỏng lẻo bằng Generic Strongly-typed events, compile-time type safe. |
| **Auth Suite & 2FA** (Sanctum SPA Cookie, Google2FA TOTP) | **ASP.NET Core Identity + JWT Bearer Tokens** | **Vượt trội hơn**. Chuẩn hóa Token-based Auth cho API tách biệt FE, hỗ trợ 2FA chuẩn RFC 6238. |
| **AI Agent Loop & Tool Calling** (DeepSeekDriver, 5 Tools tra cứu tour) | **`Microsoft.Extensions.AI` / OpenAI Client** kết hợp Custom Agent Orchestrator | **Vượt trội hơn**. Microsoft là đối tác chiến lược hàng đầu của AI thế giới, hỗ trợ native streaming token và strongly-typed schemas. |
| **Database & Metadata** (Eloquent ORM + `HasMeta` EAV) | **Entity Framework Core 10** + **JSON Columns (`.ToJson()`)** | **Vượt trội hơn**. Không cần tạo bảng `metas` trung gian cồng kềnh, map trực tiếp vào JSON column của PostgreSQL. |
| **Background Processing** (Laravel Queue Worker) | **`System.Threading.Channels` + `BackgroundService`** | **Vượt trội hơn**. Xử lý đa luồng thực thụ trong tiến trình (in-process multi-threading), không phụ thuộc queue worker bên ngoài. |
| **Omnichannel Adapters** (Telegram Webhook, Zalo Bridge) | **Telegram.Bot** + ASP.NET Webhook Controllers | **Tương thích hoàn toàn**. Giữ nguyên microservice Node.js `zalo-bridge` (Zero Impact). |

---

## 4. 📊 Phân Tích Ưu Điểm, Nhược Điểm & Trade-Off

### ✅ Ưu điểm:
1. **Hiệu năng & Throughput**: ASP.NET Core nhanh hơn PHP-FPM từ 5x - 10x, tiết kiệm RAM đáng kể khi phục vụ hàng nghìn lượt chat đồng thời.
2. **Asynchronous Non-blocking (`async/await`) thực thụ**: Khi AI gọi API DeepSeek (mất 1-3 giây) hoặc chờ Webhook, thread server không bị block.
3. **Khả năng Stream câu trả lời của AI**: Với `IAsyncEnumerable<T>`, việc stream token từng chữ ra giao diện người dùng mượt như ChatGPT là tính năng tự nhiên của .NET, khắc phục nhược điểm lớn của PHP truyền thống.
4. **Compile-time Safety**: Thay vì phải dựa vào static analysis bên ngoài (PHPStan Level 8), trình biên dịch C# bắt mọi lỗi sai kiểu dữ liệu ngay khi đang gõ phím.

### ❌ Nhược điểm & Thách thức:
1. **Mất đi tính Dynamic của PHP**: C# đòi hỏi type rõ ràng (Generics, Records, DTOs), không thể tùy tiện truyền array lỏng lẻo.
2. **Chi phí thời gian xây dựng ban đầu**: Dựng solution C# cần nhiều boilerplate hơn Laravel (DI configuration, DbContext, Fluent API mappings).

### 💡 Trade-off cốt lõi:
- **Đánh đổi**: Chấp nhận thời gian dựng khung dự án lúc đầu lâu hơn 20-30% so với PHP.
- **Thu lại**: Độ tin cậy cực cao, hiệu năng vượt trội, không còn lỗi runtime ngầm, và sở hữu một dự án chuẩn mực công nghệ Microsoft để chứng minh năng lực khi đi phỏng vấn.

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Tại sao bạn quyết định viết lại dự án từ Laravel sang .NET 10 thay vì tiếp tục với PHP?"*

**Câu trả lời chuẩn mực**:
> *"Hệ thống AI Agent có đặc thù là độ trễ I/O cao (DeepSeek API mất 1-3 giây mỗi lượt gọi) và yêu cầu streaming token thời gian thực về client. Trong khi PHP truyền thống xử lý request-response đồng bộ hoặc cần các giải pháp runtime phức tạp như Octane/Swoole, thì .NET 10 cung cấp mô hình bất đồng bộ `async/await` non-blocking native và `IAsyncEnumerable` hoàn hảo cho việc stream AI. Ngoài ra, việc chuyển đổi sang .NET 10 giúp tôi tận dụng hệ sinh thái `Microsoft.Extensions.AI` mới nhất và đảm bảo an toàn kiểu dữ liệu compile-time tuyệt đối."*

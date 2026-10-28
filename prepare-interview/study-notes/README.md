# 📚 Study Notes — Phỏng Vấn Senior .NET Engineer
> Dựa trên dự án TourAgent (`dotnet-backend-core-main` — .NET 10 Clean Architecture)

---

## 📂 Danh Sách Files

| File | Nhóm | Ưu tiên | Trạng thái |
|---|---|---|---|
| [01_clean_architecture.md](01_clean_architecture.md) | Clean Architecture & Design Principles | 🔴 CAO NHẤT | Đang học |
| [02_dotnet10_csharp.md](02_dotnet10_csharp.md) | .NET 10 & C# Modern Features | 🟠 CAO | Đang học |
| [03_ai_integration_llm.md](03_ai_integration_llm.md) | AI Integration & LLM Patterns | 🟡 CAO (điểm khác biệt) | Đang học |
| [04_efcore_database.md](04_efcore_database.md) | EF Core 10 & Database | 🟢 TRUNG BÌNH | Đang học |
| [05_system_design_async.md](05_system_design_async.md) | System Design & Async Patterns | 🔵 TRUNG BÌNH | Đang học |

---

## 🎯 Cách Sử Dụng Files

1. **Học**: Đọc từng section, làm theo IPO (Input → Process → Output bằng lời)
2. **Cập nhật**: Thêm vào section `🆕 Cập Nhật Mới` khi có câu hỏi/kiến thức mới
3. **Ôn tập**: Dùng Checklist cuối mỗi file để tự test

---

## ⚡ Quick Reference — Câu Hỏi Thường Gặp

| Câu hỏi | File |
|---|---|
| Clean Architecture là gì? Trade-off? | 01 |
| Tại sao không dùng MediatR / AutoMapper / Generic Repo? | 01 |
| Result\<T\> vs Exception | 01 |
| Record vs Class? | 02 |
| CancellationToken tại sao phải propagate? | 02 |
| Scoped / Singleton / Transient | 02 |
| Agent Loop hoạt động như thế nào? | 03 |
| Zero Hallucination đạt được bằng cách nào? | 03 |
| Channels vs RabbitMQ | 03 + 05 |
| AsNoTracking khi nào dùng? | 04 |
| N+1 Problem là gì? Cách fix? | 04 |
| DbContext thread-safe không? | 04 |
| SSE vs WebSocket | 05 |
| Tại sao BackgroundService cần IServiceScopeFactory? | 05 |

---

## 📅 Lịch Học (2 tuần)

| Ngày | File | Mục tiêu |
|---|---|---|
| Day 1-2 | `01` | Hiểu 4 layers, giải thích được không nhìn tài liệu |
| Day 3-4 | `02` | Nắm C# modern features từ code TourAgent |
| Day 5-6 | `03` | Trace Agent Loop, giải thích IPO |
| Day 7 | Ôn | Tự kiểm tra checklist file 01-03 |
| Day 8-9 | `04` | EF Core patterns, query optimization |
| Day 10-11 | `05` | Channels, SSE, BackgroundService |
| Day 12-13 | Mock | Tự hỏi → tự trả lời → record lại |
| Day 14 | Story | Luyện kể 15 tháng trong 3 phút |

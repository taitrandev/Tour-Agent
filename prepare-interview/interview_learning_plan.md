# 🎯 Kế Hoạch Ôn Luyện Phỏng Vấn — .NET Senior Engineer
> Dựa trên source `dotnet-backend-core-main` (TourAgent — .NET 10 Clean Architecture)
> Bối cảnh: Gap 15 tháng, vibe coding, mạnh mindset & AI, cần làm mới kiến thức kỹ thuật

---

## 📌 Tổng Quan Chiến Lược

Điểm mạnh của bạn là **mindset, quy trình, AI-first thinking** — đây là thứ senior developer thực sự cần.
Điểm cần bổ sung là **kiến thức kỹ thuật .NET latest** — dùng chính TourAgent project làm "bộ giáo trình sống".

**Nguyên tắc học theo IPO:**
- **Input**: đọc code, doc, câu hỏi
- **Process**: tự hỏi bản chất → cơ chế → trade-off → khi nào dùng/không dùng
- **Output**: trả lời được câu hỏi bằng lời, viết được code tay, giải thích bằng ẩn dụ

---

## 🗺️ 5 Nhóm Kiến Thức Cần Nắm (Theo Độ Ưu Tiên)

### 🔴 NHÓM 1: Clean Architecture & Design Principles (ƯU TIÊN CAO NHẤT)
*Đây là thứ interviewer sẽ hỏi ngay — project TourAgent là minh chứng sống*

### 🟠 NHÓM 2: .NET 10 & C# Modern Features
*Kiến thức kỹ thuật mới nhất cần cập nhật sau gap 15 tháng*

### 🟡 NHÓM 3: AI Integration & LLM Patterns
*Điểm độc đáo của bạn — kết hợp với kỹ thuật sẽ tạo ra sức mạnh đặc biệt*

### 🟢 NHÓM 4: Database & EF Core 10
*Nền tảng backend quan trọng*

### 🔵 NHÓM 5: System Design & Async Patterns
*Channels, Background Services, SSE — những pattern hiện đại*

---

## 📚 NHÓM 1: Clean Architecture & Design Principles

### Kiến Thức Cần Nắm (từ source TourAgent)

| Concept | File tham khảo | Mức độ |
|---|---|---|
| 4-layer dependency flow | `ARCHITECTURE.md` | ⭐⭐⭐ PHẢI BIẾT |
| Dependency Inversion Principle | `Domain/*.csproj` (no external refs) | ⭐⭐⭐ |
| Feature Folders / Vertical Slice | `Application/Features/` | ⭐⭐⭐ |
| Result\<T\> Pattern (thay exception) | `Domain/Common/Result.cs` | ⭐⭐⭐ |
| Entity Invariants + Factory Method | `Domain/Tours/`, `Domain/Conversations/` | ⭐⭐ |
| Architecture Tests (NetArchTest) | `tests/TourAgent.ArchitectureTests` | ⭐⭐ |
| YAGNI, DRY trong thực tế | `AGENTS.md` — Forbidden List | ⭐⭐ |

### Câu Hỏi Phỏng Vấn & Cách Trả Lời

**Q1: "Em giải thích Clean Architecture và tại sao dùng nó?"**
> **Cách trả lời (dùng ẩn dụ):**
> - Dùng ẩn dụ hành nhân-cơ thể: Domain là não (không biết cơ thể gồm gì), Application là thần kinh (điều phối), Infrastructure là tay chân (làm việc cụ thể), Api là miệng (nhận lệnh từ bên ngoài).
> - **Trade-off cần nhớ**: Chi phí setup ban đầu cao hơn, nhưng khi cần thay đổi DB hay LLM vendor, chỉ cần thay Infrastructure, không đụng đến business logic.
> - **Chỉ ra bằng code thật**: TourAgent thay DeepSeek thành GPT-4 → chỉ cần đổi 1 file `LlmClient.cs` trong Infrastructure.

**Q2: "Tại sao không dùng MediatR? Không dùng AutoMapper?"**
> - **MediatR**: Thêm 1 lớp trừu tượng không cần thiết. Handler được inject trực tiếp → dễ trace code, dễ debug, không magic.
> - **AutoMapper**: Rủi ro license (v11+ có thể có phí), hiệu năng kém hơn manual mapping, lỗi runtime khó debug. Manual mapping = kiểm soát hoàn toàn + hiệu năng tốt hơn.
> - Đây là ví dụ về **trade-off thinking** — không phải "AutoMapper xấu" mà là "ở quy mô này, cost > benefit".

**Q3: "Generic Repository Pattern — khi nào nên và không nên dùng?"**
> - **Không nên** (như TourAgent): Project SME, tránh boilerplate. EF Core `DbSet<T>` đã là repository rồi.
> - **Nên dùng khi**: Cần mock DB trong unit test phức tạp, hoặc cần swap ORM.
> - **Bản chất**: Generic Repository là abstraction. Hỏi YAGNI — "Bây giờ có cần không?"

**Q4: "NetArchTest là gì? Tại sao cần?"**
> - Test kiểm tra ranh giới giữa các layer tự động. Ví dụ: Domain không được import EF Core.
> - Nếu dev vô tình vi phạm → CI/CD báo lỗi ngay, không phải review bằng mắt thường.
> - **Mindset**: Quy trình tốt là quy trình tự động kiểm soát, không dựa vào kỷ luật con người.

---

## 📚 NHÓM 2: .NET 10 & C# Modern Features

### Kiến Thức Cần Cập Nhật

| Feature | Giải thích | Khi nào dùng |
|---|---|---|
| **Minimal APIs** | Thay Controller, ít ceremony hơn | API nhỏ/microservice |
| **Primary Constructors** (C# 12) | `class Foo(IService svc)` | Giảm boilerplate DI |
| **Required Members** (C# 11) | `required string Name` | Bắt buộc init, thay nullable warnings |
| **Nullable Reference Types** | `#nullable enable` — compiler check | Mọi project mới |
| **TreatWarningsAsErrors** | Nullable warning → compile error | Enforced clean code |
| **Global Usings** | `global using System;` | Giảm boilerplate |
| **Record types** | Immutable data, value equality | DTOs, Value Objects |
| **Pattern Matching** (C# 9-12) | `switch expression`, `is not null` | Thay if-else phức tạp |
| **CancellationToken** | Mọi async method phải có | Best practice async |
| **IAsyncEnumerable** | Streaming data | SSE, large datasets |

### Câu Hỏi Phỏng Vấn

**Q5: "Minimal API vs Controller-based API — trade-off?"**
> - **Minimal API**: Ít code hơn, nhanh hơn một chút, phù hợp microservice. Nhưng khó test riêng lẻ hơn, ít convention hơn.
> - **Controller**: Convention rõ ràng, dễ maintain trong team lớn, nhiều middleware hơn.
> - **TourAgent dùng**: Controllers inject trực tiếp Handler — lấy cái tốt nhất của cả 2.

**Q6: "CancellationToken tại sao phải truyền qua mọi hàm async?"**
> - Khi client disconnect, nếu không có CancellationToken, server vẫn tiếp tục chạy query DB, gọi LLM — lãng phí tài nguyên.
> - **Root cause** của nhiều timeout issue là thiếu propagation CancellationToken đúng chỗ.
> - Rule trong TourAgent: `PHẢI nhận CancellationToken ct` — đây là rule bắt buộc.

**Q7: "Result\<T\> pattern vs Exception — khi nào dùng cái nào?"**
> - **Exception**: Lỗi thực sự không mong đợi, lỗi infrastructure (DB down), lỗi lập trình.
> - **Result\<T\>**: Lỗi business logic bình thường (tour không tìm thấy, validation fail). Caller biết trước cần handle.
> - **Bản chất**: Exception là "emergency", Result\<T\> là "expected branch". Dùng exception cho business flow = code smell.

---

## 📚 NHÓM 3: AI Integration & LLM Patterns

### Đây Là Điểm Mạnh Nhất Của Bạn — Cần Kết Hợp Với Kỹ Thuật

| Concept | Nguồn trong TourAgent | Ứng dụng kỹ thuật |
|---|---|---|
| **Tool Calling / Function Calling** | `Application/Agent/Tools/` | OpenAI protocol, JSON schema |
| **Agent Loop** | `Application/Agent/Services/AgentLoop.cs` | Max turns, audit trail |
| **Harness Pattern** | `Application/Agent/Harness/` | Tool guardrail pipeline |
| **Zero Hallucination** | `AGENTS.md` — AI Rules | Tool-only data retrieval |
| **ILlmClient Abstraction** | `Application/Abstractions/ILlmClient.cs` | Vendor-neutral, testable |
| **SSE Streaming** | `Api` — `/chat/stream` endpoint | Real-time UX |
| **Omnichannel Queue** | `System.Threading.Channels` | Telegram, Web, Zalo |
| **Agent State** | Conversation + Message entities | Memory trong hội thoại |

### Câu Hỏi Phỏng Vấn

**Q8: "Em xây AI Agent ra sao? Kiến trúc như thế nào?"**
> - **Trả lời bằng IPO**: Input = user message → Process = Agent Loop (LLM quyết định tool nào gọi → gọi tool → inject result vào prompt → LLM trả lời cuối) → Output = response + audit log.
> - **Max 5 turns**: Tránh infinite loop, tránh tốn token. Đây là `Tool Guardrail` — nguyên tắc hàng rào.
> - **Zero Hallucination**: LLM không được tự bịa giá tour. Chỉ được lấy từ DB qua Tool. Inversion Thinking → "LLM có thể fail theo cách nào?" → tự bịa số liệu → giải pháp: force tool-only data.

**Q9: "Tại sao cần ILlmClient abstraction thay vì gọi trực tiếp OpenAI SDK?"**
> - Vendor lock-in risk: Hôm nay dùng DeepSeek, ngày mai dùng GPT-4o, tháng sau dùng Gemini.
> - Testability: Dùng `FakeLlmClient` trong unit test mà không cần internet.
> - **Root cause của dependency hell**: Không có abstraction → refactor tốn 10x thời gian.

**Q10: "System.Threading.Channels vs Message Queue (RabbitMQ, Azure Service Bus)?"**
> - **Channels**: In-process, zero latency, không cần infra thêm, phù hợp single instance.
> - **Message Queue**: Cross-service, durable (persist sau khi restart), phù hợp microservice, distributed system.
> - **Trade-off**: Channels nhanh hơn, đơn giản hơn, nhưng không bền sau khi app restart. Nếu Telegram message mà app crash → mất. Chấp nhận được ở quy mô SME.

---

## 📚 NHÓM 4: Database & EF Core 10

### Kiến Thức Cần Nắm

| Concept | TourAgent tương ứng |
|---|---|
| **EF Core Code First** | Entity Configurations trong Infrastructure |
| **IAppDbContext** | Abstraction cho testability |
| **Migration** | `dotnet ef migrations add` |
| **Query optimization** | Include, AsSplitQuery, AsNoTracking |
| **PostgreSQL với Npgsql** | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| **Transaction** | Unit of Work qua SaveChangesAsync |

### Câu Hỏi Phỏng Vấn

**Q11: "AsNoTracking — khi nào dùng, khi nào không?"**
> - **Dùng**: Query chỉ để đọc (GET), không cần update entity sau đó. Tăng hiệu năng đáng kể.
> - **Không dùng**: Khi cần update entity sau khi query (EF Core cần track thay đổi).
> - **Root cause**: Change tracker của EF Core tốn memory và CPU để theo dõi. AsNoTracking = tắt tính năng này khi không cần.

**Q12: "N+1 Query Problem là gì? Cách phòng tránh trong EF Core?"**
> - **Vấn đề**: Loop qua 100 tours, mỗi tour lại query thêm departures → 101 queries thay vì 1.
> - **Giải pháp**: `.Include(t => t.Departures)` — Eager Loading. Hoặc `.AsSplitQuery()` khi có nhiều collection navigations.
> - **Bản chất**: Lazy loading tiện nhưng ẩn nguy hiểm. Explicit loading = kiểm soát tốt hơn.

---

## 📚 NHÓM 5: System Design & Async Patterns

### Câu Hỏi Phỏng Vấn

**Q13: "Thiết kế hệ thống xử lý tin nhắn Telegram không bị timeout?"**
> - Pattern: **Webhook → Channel → BackgroundService** (TourAgent đã implement)
> - Telegram gọi webhook → nhận message → ack ngay (200 OK) → đẩy vào Channel → BackgroundService xử lý async → gọi LLM → gửi response.
> - **Root cause** của timeout: Xử lý đồng bộ trong webhook → Telegram timeout sau 60s → retry loop.

**Q14: "SSE vs WebSocket — trade-off?"**
> - **SSE**: Một chiều (server → client), đơn giản hơn, HTTP/2 native, phù hợp streaming text.
> - **WebSocket**: Hai chiều, phức tạp hơn, cần handling reconnect, phù hợp chat real-time.
> - **TourAgent dùng SSE** cho AI streaming vì LLM response là one-way stream.

---

## 🎤 Câu Hỏi "Kể Về Bản Thân" — Story Framework

### Framework Kể Chuyện 15 Tháng (STAR Enhanced)

```
HOOK (15 giây):
"15 tháng nghỉ việc là quyết định táo bạo nhất trong sự nghiệp của em —
và cũng là thứ biến em từ một senior engineer thành một người tư duy khác hẳn."

JOURNEY (2 phút, 4 giai đoạn):
Giai đoạn 1 → AI discovery (pixel density / root cause / solution bias)
Giai đoạn 2 → Mindset development (note suy nghĩ, hỏi AI, Circle of Competence)
Giai đoạn 3 → Process mastery (YouTube, quy trình, feedback loop)
Giai đoạn 4 → AI Engineering (Agent, Harness, TourAgent project)

PROOF (30 giây):
"Minh chứng cụ thể là project TourAgent — .NET 10, Clean Architecture,
41/41 tests passed, AI Agent với Zero Hallucination, Telegram Omnichannel."

CLOSING (15 giây):
"Em quay lại không chỉ với kỹ thuật — mà với bộ công cụ tư duy
để đóng góp vào cách nhóm tiếp cận và giải quyết vấn đề."
```

---

## 📅 Lịch Học Đề Xuất (2 Tuần)

| Ngày | Nhóm | Hoạt động |
|---|---|---|
| **Day 1-2** | Nhóm 1 | Đọc code Domain, Application. Tự trả lời Q1-Q4 bằng lời |
| **Day 3-4** | Nhóm 2 | Tìm từng C# feature trong code TourAgent. Tự giải thích |
| **Day 5-6** | Nhóm 3 | Trace AgentLoop, Tools, Harness. Vẽ sơ đồ IPO |
| **Day 7** | Review | Ôn lại Q1-Q10, tự trả lời không nhìn |
| **Day 8-9** | Nhóm 4 | EF Core configurations, IAppDbContext pattern |
| **Day 10-11** | Nhóm 5 | Channels pattern, SSE endpoint |
| **Day 12-13** | Mock Interview | Tự đặt câu hỏi, record lại, nghe lại |
| **Day 14** | Story Practice | Luyện kể 15 tháng trong đúng 3 phút |

---

## 🧠 Mindset Áp Dụng Khi Học

> **IPO Rule**: Mỗi concept = Input (đọc) → Process (tự hỏi bản chất/trade-off) → Output (giải thích được bằng lời/ví dụ)

> **Inversion Thinking**: "Interviewer ghét nghe gì? → Trả lời không có trade-off, không có lý do, học thuộc lòng không hiểu bản chất"

> **Circle of Competence**: Biết rõ cái mình biết. Câu nào chưa chắc → nói thật: "Em cần xem lại cụ thể, nhưng bản chất em hiểu là..."

> **Root Cause First**: Mỗi câu hỏi kỹ thuật → đừng vội trả lời cách làm → hỏi ngược: "Tại sao pattern này tồn tại? Vấn đề nó giải quyết là gì?"

---

## ✅ Checklist "Sẵn Sàng Phỏng Vấn"

- [ ] Giải thích được Clean Architecture bằng ẩn dụ, không đọc lại
- [ ] Nói được trade-off của 5 quyết định kiến trúc trong TourAgent
- [ ] Giải thích Agent Loop (IPO) không cần nhìn code
- [ ] Kể được câu chuyện 15 tháng trong đúng 3 phút
- [ ] Trả lời được "Tại sao không dùng MediatR/AutoMapper/Generic Repo?"
- [ ] Giải thích Result\<T\> vs Exception bằng ví dụ thực tế
- [ ] Demo được project TourAgent live (dotnet run thành công)

# 🧭 Hành Trình Chuyển Đổi: Kỳ 6 — Core AI Agent Engine, 5 Travel Tools & Multi-Turn Loop

> **Mục tiêu tài liệu**: Ghi lại quá trình xây dựng trái tim của hệ thống: Cổng giao tiếp LLM trừu tượng (`ILlmClient`), bộ 5 công cụ tra cứu nghiệp vụ tour lữ hành, hàng rào phòng thủ chống ảo giác (Anti-Hallucination Guardrails), và trình điều phối chu trình suy luận đa lượt tự động (`AgentLoop`).

---

## 1. 🧠 Thiết Kế Trừu Tượng `ILlmClient` & Tích Hợp DeepSeek V3

### Vấn đề kiến trúc:
- Trong các dự án AI, việc phụ thuộc cứng vào SDK của một nhà cung cấp duy nhất (như OpenAI SDK hoặc Google Gemini SDK) sẽ dẫn đến rủi ro "Vendor Lock-in".
- Khi chi phí API thay đổi hoặc khi doanh nghiệp muốn đổi sang mô hình mã nguồn mở nội bộ (Self-hosted vLLM/Ollama), việc sửa code sẽ làm ảnh hưởng tới toàn bộ hệ thống.

### Giải pháp Clean Architecture:
Tôi thiết lập giao diện trừu tượng [ILlmClient](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/ILlmClient.cs) tại tầng `TourAgent.Application`:
- Hỗ trợ gọi chuẩn Request-Response ([CompleteChatAsync](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/ILlmClient.cs#L140-L149)).
- Hỗ trợ truyền phát trực tiếp ([StreamChatAsync](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Abstractions/ILlmClient.cs#L151-L158)) qua `IAsyncEnumerable<ChatStreamChunk>` — thế mạnh vượt trội của .NET Core so với PHP-FPM truyền thống.
- Thực thi [DeepSeekLlmClient](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Llm/DeepSeekLlmClient.cs) tại tầng `Infrastructure` sử dụng `HttpClient` chuẩn mực của .NET 10, tương thích giao thức OpenAI Function Calling.

---

## 2. 🛡️ Hàng Rào Chống Ảo Giác (Anti-Hallucination Guardrails)

Trong tư vấn du lịch, việc AI "bịa" ra ngày bay không có thật hoặc tự ý hứa giảm giá là lỗi cấm kỵ tuyệt đối. Tôi đã chuyển hóa toàn bộ bộ quy tắc nghiêm ngặt từ PHP sang [TravelAgentPrompts.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Prompts/TravelAgentPrompts.cs):

1. **Nguyên tắc "No Tool, No Claim"**: Mọi thông tin về giá, ngày khởi hành, lịch trình, chính sách visa BẮT BUỘC phải lấy từ kết quả thực thi của Tool. Cấm tuyệt đối nói *"em đã gửi sếp"* nếu chưa kích hoạt tool và nhận được mã ticket thực tế `#ESC-...`.
2. **Kích hoạt công cụ ngay lượt đầu tiên (Zero-Shot Action)**: Khi khách hỏi "có bao nhiêu tour?" hoặc "tháng 10 có tour nào?", Agent phải gọi ngay `search_travels` hoặc `check_departures` thay vì hỏi lại người dùng một cách máy móc.
3. **Kỹ thuật Micro-Response & Proactive Discovery**:
   - Chia nhỏ văn bản tối đa 2-3 câu mỗi đoạn, tối ưu cho màn hình di động.
   - Luôn chủ động hỏi 1 câu ngắn gợi mở nhu cầu (số lượng khách, ngày dự kiến) và khéo léo gom SĐT/Zalo liên hệ.
4. **Bơm ngữ cảnh tour đang xem (Tour Context Injection)**: Khi khách chat từ trang chi tiết của một tour, hệ thống tự động inject `travel_id`, `travel_title`, `country` vào System Prompt để AI hiểu ngầm câu hỏi chung mà không cần hỏi lại.

---

## 3. 🧰 Triển Khai 5 Agent Tools Nghiệp Vụ Du Lịch

Toàn bộ 5 công cụ đều hiện thực hóa giao diện [IAgentTool](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Contracts/IAgentTool.cs), truy vấn trực tiếp qua `IAppDbContext`:

| Tên Công Cụ (Tool Name) | File Thực Thi | Chức Năng Nghiệp Vụ | Dữ Liệu Khai Thác |
| :--- | :--- | :--- | :--- |
| **`search_travels`** | [SearchTravelsTool.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Tools/SearchTravelsTool.cs) | Tra cứu số lượng, danh sách tour theo quốc gia, điểm đến, ngân sách trần, từ khóa. | Bảng `travels` (status = published) |
| **`check_departures`** | [CheckDeparturesTool.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Tools/CheckDeparturesTool.cs) | Tra cứu các ngày khởi hành thực tế, giá người lớn/trẻ em, số chỗ còn trống. | Bảng `travel_departures` |
| **`get_travel_itinerary`** | [GetTravelItineraryTool.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Tools/GetTravelItineraryTool.cs) | Bóc tách lịch trình chi tiết theo từng ngày (Day 1, Day 2...) và link ảnh điểm đến. | Cột `itinerary` (JSON) trong `travels` |
| **`get_travel_policies`** | [GetTravelPoliciesTool.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Tools/GetTravelPoliciesTool.cs) | Tra cứu điều khoản theo chủ đề: visa, hủy tour, thanh toán, bao gồm/không bao gồm. | Các cột policy và json inclusions/exclusions |
| **`escalate_to_human`** | [EscalateToHumanTool.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Tools/EscalateToHumanTool.cs) | Chuyển tiếp phiên tư vấn sang nhân viên (Sales/CSKH), tạo mã phiếu `#ESC-...`, cập nhật SĐT. | Chuyển `Conversation.Status` sang `HumanActive` |

---

## 4. 🔄 Trình Điều Phối Chu Trình Suy Luận Đa Lượt: `AgentLoop`

File [AgentLoop.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Services/AgentLoop.cs) đóng vai trò là "bộ não" điều phối:

```
[Khách gửi tin nhắn]
        │
        ▼
┌───────────────────────────────────────────────┐
│ Nạp lịch sử chat (tối đa 10 tin) + System     │
│ Prompt (Guardrails) + Danh sách 5 Tools       │
└───────────────────────┬───────────────────────┘
                        │
                        ▼ (Lượt 1)
┌───────────────────────────────────────────────┐
│ Gọi DeepSeek V3 (CompleteChatAsync)           │
│ Ghi nhận LlmCall (Tokens, Latency, Cost USD)  │
└───────────────────────┬───────────────────────┘
                        │
         Có ToolCall không?
         ├── CÓ:
         │    1. Lưu Message (Assistant + ToolCall)
         │    2. Tìm và chạy IAgentTool tương ứng
         │    3. Lưu Message (Role: Tool, Payload: JSON)
         │    4. Bơm kết quả vào Messages ➔ Lặp lại (Lượt 2)
         │
         └── KHÔNG (Hoặc đạt MaxTurns = 5):
              1. Lưu Message (Role: Assistant, Content)
              2. SaveChangesAsync() vào PostgreSQL
              3. Trả về AgentChatResult (Text, Tokens, Cost, Tools)
```

### Bài học kỹ thuật giải quyết trong quá trình build:
- **Lỗi biên dịch CS1631**: Trong C#, không được phép `yield return` bên trong khối `catch` của một iterator method. Giải pháp: Lưu `errorMessage` bên ngoài và yield return sau khối catch.
- **Quy tắc phân tích mã CA2024**: Không dùng `reader.EndOfStream` trong phương thức async vì gây chặn luồng đồng bộ. Giải pháp: Sử dụng `while (await reader.ReadLineAsync(cancellationToken) is { } line)`.

---

## 5. 🧪 Kiểm Thử Hermetic & Kết Quả Tổng Thể

- Xây dựng [FakeLlmClient.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/tests/TourAgent.Application.UnitTests/Common/FakeLlmClient.cs) giả lập chuỗi tương tác đa lượt mà không cần gọi API thật tốn tiền hay phụ thuộc mạng.
- **[AgentToolTests.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/tests/TourAgent.Application.UnitTests/Agent/AgentToolTests.cs)**: Kiểm tra độc lập cả 5 tools với in-memory database (5/5 PASSED).
- **[AgentLoopTests.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/tests/TourAgent.Application.UnitTests/Agent/AgentLoopTests.cs)**: Kiểm tra trả lời trực tiếp 1 lượt, chu trình đa lượt tự gọi tool và cơ chế tự ngắt an toàn chống lặp vô hạn (3/3 PASSED).
- 👉 **TỔNG KẾT TOÀN DỰ ÁN: 39/39 TESTS XANH 100% (Zero Warnings)**:
  - 24 Domain Tests
  - 12 Application Tests (4 Slice SearchTours + 5 Agent Tools + 3 Agent Loop)
  - 3 Architecture Boundary Tests (`NetArchTest.Rules`)

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Làm thế nào bạn thiết kế Agent Loop tự động gọi Tool trong .NET và làm sao kiểm soát được rủi ro AI bị lặp vô hạn (Infinite Loop) hoặc ảo giác dữ liệu (Hallucination)?"*

**Câu trả lời chuẩn mực**:
> *"Về mặt kiến trúc, tôi áp dụng mô hình Autonomous Multi-Turn Loop được đóng gói trong class `AgentLoop`. Mỗi công cụ là một implementation của `IAgentTool`, tự định nghĩa JSON Schema và được inject qua DI. Khi LLM trả về `finish_reason: 'tool_calls'`, AgentLoop tự động phân giải tool tương ứng, thực thi qua `IAppDbContext` và bơm lại kết quả dưới dạng `ChatMessageRoles.Tool` để LLM tổng hợp câu trả lời ở lượt tiếp theo.*
> 
> *Để kiểm soát rủi ro, tôi thiết lập 3 lớp phòng thủ:*
> 1. *Giới hạn cứng `MaxTurns = 5` để triệt tiêu hoàn toàn khả năng lặp vô hạn nếu model bị kẹt giữa các tool calls.*
> 2. *Hàng rào phòng thủ 'No Tool, No Claim' trong System Prompt: cấm tuyệt đối trả lời giá hay ngày bay nếu không qua Tool, và cấm hứa hẹn chuyển tiếp nếu chưa có mã ticket thực tế.*
> 3. *Kiểm toán 100% các lượt gọi qua entity `LlmCall`, lưu vết chính xác model, prompt/completion tokens, độ trễ và chi phí USD để phục vụ giám sát chi phí vận hành."*

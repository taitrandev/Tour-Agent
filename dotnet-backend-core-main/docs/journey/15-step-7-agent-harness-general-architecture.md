# 15. Bước 7: Khung Giám Sát & Điều Khiển Tác Nhân (Agent Harness Architecture - Phase A: General Techniques)

> **Mục tiêu:** Áp dụng bài học kiến trúc từ hệ thống PHP 8.4 sang .NET 10 để nâng cấp độ tin cậy của AI Agent từ dạng "Chatbot gọi hàm cơ bản" lên **"Hệ Thống Tác Nhân Tự Trị Có Kiểm Soát (Production-Grade Agent Harness)"** qua 3 kỹ thuật tổng quát: **Agent State Store (G1)**, **General Agent Status Bar Framework (G2)** và **Tool Guardrail Pipeline (G3)**.

---

## 1. Bối Cảnh & Vấn Đề Khi Không Có Agent Harness

### 1.1 Hiện Tượng Trong Thực Tế (Vanilla LLM Agent)
Khi xây dựng Agent chỉ dựa vào System Prompt và vòng lặp `while (tools.Count > 0)` đơn giản, hệ thống gặp phải các hội chứng nguy hiểm trong môi trường sản xuất:
1. **Hội chứng "Đãng Trí" (State Loss across Turns)**: Khi hội thoại kéo dài hoặc LLM gọi qua nhiều công cụ, mô hình quên mất mục tiêu ban đầu, quên các dữ kiện bất biến đã xác nhận (như số điện thoại, ngày đi, ngân sách).
2. **Ảo Giác Tham Số Công Cụ (Malformed & Missing Arguments)**: LLM sinh tham số JSON thiếu các trường bắt buộc (`required`), sai kiểu dữ liệu hoặc JSON bị vỡ cú pháp. Nếu gọi trực tiếp vào Tool, ứng dụng sẽ ném ngoại lệ (`JsonException` / `NullReferenceException`) làm sập luồng xử lý của khách hàng.
3. **Mù Trạng Thái Thực Tế (State-Action Disconnect)**: LLM không có góc nhìn phản xạ thực tế (State Reflection) về ngân sách lượt gọi còn lại, trạng thái workflow hiện tại và dữ liệu domain của hệ thống.
4. **Tranh Chấp Ghi Trạng Thái (Concurrency Conflict)**: Nhiều sự kiện hoặc webhook đẩy vào đồng thời có thể gây ghi đè làm mất trạng thái tiến trình của Agent.

### 1.2 Giải Pháp Kiến Trúc: Bộ Khung Agent Harness 3 Tầng
Tham khảo thiết kế từ dự án PHP, chúng tôi xây dựng bộ khung bảo vệ 3 lớp trong .NET 10:

```text
               ┌─────────────────────────────────────────────────────────┐
               │                     User Chat Message                   │
               └────────────────────────────┬────────────────────────────┘
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kỹ Thuật G1] Agent State Store & OCC                   │
               │ - Đọc/Nạp AgentState có Version                         │
               │ - Nhận diện sự thật bất biến (Facts: SĐT, Tour ID)     │
               └────────────────────────────┬────────────────────────────┘
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kỹ Thuật G2] General Agent Status Bar Framework        │
               │ - Dựng XML <agent_status> (Reflection Context)          │
               │ - Tiêm vào cuối tin nhắn User gần nhất (Recency Bias)   │
               └────────────────────────────┬────────────────────────────┘
                                            │
                                            ▼
                               [DeepSeek V3 LLM Orchestrator]
                                            │
                                AI yêu cầu gọi Tool
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kỹ Thuật G3] Tool Guardrail Pipeline (3 Lớp)           │
               ├─────────────────────────────────────────────────────────┤
               │ 1. Constrain:  Tiền kiểm Schema & Required fields        │
               │ 2. Execute:    Bọc an toàn Try-Catch mọi Exception      │
               │ 3. Verify:     Hậu kiểm lỗi nghiệp vụ & sinh Hint tự sửa│
               └────────────────────────────┬────────────────────────────┘
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kiểm Toán & Lưu Trữ]                                   │
               │ - Cập nhật AgentState (Tăng Version) vào metadata       │
               │ - Lưu snapshot XML & State vào bảng kiểm toán llm_calls │
               └─────────────────────────────────────────────────────────┘
```

---

## 2. Chi Tiết Kỹ Thuật Phase A Đã Triển Khai

### 2.1 Kỹ Thuật G1: Agent State Store & Optimistic Concurrency Control (OCC)
- **Container Trạng Thái Có Phiên Bản (`AgentState`)**:
  - `Version`: Số nguyên tự tăng sau mỗi lần ghi, đóng vai trò khóa lạc quan.
  - `WorkflowStatus`: Trạng thái luồng nghiệp vụ hiện tại (`idle`, `in_progress`, `escalated`, `completed`).
  - `CurrentGoal`: Mục tiêu hiện tại Agent đang nỗ lực đạt được.
  - `ConfirmedFacts`: Từ điển chứa các sự thật bất biến đã xác thực (ví dụ `customer_phone`, `departure_date`, `pax`).
  - `DomainState`: Dữ liệu ngữ cảnh nghiệp vụ mở rộng.
- **`IAgentStateStore` & `DatabaseAgentStateStore`**:
  - Tách biệt hoàn toàn trạng thái logic của Agent khỏi cấu trúc bảng CSDL bằng cách lưu dưới dạng JSON trong cột `conversations.metadata->'agent_state'`.
  - Phương thức `SaveAsync(long conversationId, AgentState state, int expectedVersion)` kiểm tra phiên bản hiện tại trước khi ghi. Nếu có tiến trình khác đã ghi trước (`currentState.Version != expectedVersion`), hệ thống trả về lỗi `Error.Conflict("AgentState.VersionConflict")` thay vì ghi đè âm thầm.

### 2.2 Kỹ Thuật G2: General Agent Status Bar Framework & State Reflection
- **Cơ chế Tiêm Thanh Trạng Thái (Status Bar Injection)**:
  - Thay vì nhét trạng thái vào System Prompt cố định (dễ bị mô hình "bỏ quên" do context dài và bị browser/proxy cache), trạng thái được định dạng thành thẻ XML chuẩn và **tiêm vào cuối tin nhắn của người dùng gần nhất**:
    ```xml
    <agent_status version="1">
      <workflow_status>in_progress</workflow_status>
      <current_goal>consulting</current_goal>
      <confirmed_facts>
        <fact key="customer_phone">0909349909</fact>
      </confirmed_facts>
      <domain_context domain="travel">
        <property key="active_tour">JP-TOK-01</property>
      </domain_context>
    </agent_status>
    ```
  - **Tận Dụng Recency Bias**: LLM luôn chú ý cao nhất đến nội dung nằm ở cuối prompt, giúp mô hình phản xạ cực kỳ chính xác về mục tiêu và dữ kiện bất biến.
  - **Plugin Architecture (`IAgentStatusProvider`)**: Cho phép các domain con (Tour, Khách sạn, Visa) tự động cắm thêm dữ liệu phản xạ mà không sửa đổi `StatusBarBuilder`.

### 2.3 Kỹ Thuật G3: Tool Guardrail Pipeline (3 Lớp Bảo Vệ)
Mọi lượt gọi công cụ của AI Agent bắt buộc phải đi qua `ToolGuardrailPipeline`:
1. **Pha 1: Constrain (Tiền Kiểm)**:
   - Dùng `SchemaToolValidator` kiểm tra tính hợp lệ của cú pháp JSON.
   - Đối chiếu với `ParametersSchema` của công cụ. Nếu thiếu trường bắt buộc (`required`), pipeline chặn ngay lập tức, trả về thông báo lỗi kèm `recovery_hint` hướng dẫn AI trích xuất dữ liệu hoặc hỏi lại khách trước khi gọi lại. Không để gọi vào thân công cụ.
2. **Pha 2: Execute (Thực Thi An Toàn)**:
   - Tự động làm giàu tham số với ngữ cảnh phiên (`conversation_id`, `chat_id`).
   - Bọc toàn bộ quá trình thực thi bằng khối `try-catch`. Nếu có lỗi kết nối hoặc ngoại lệ bất ngờ, pipeline bọc lại thành phản hồi JSON thân thiện kèm cờ `_guardrail_status = "execution_exception"`, đo đạc chính xác thời gian chạy `latencyMs`.
3. **Pha 3: Verify & Correct (Hậu Kiểm & Tự Sửa Lỗi)**:
   - Dùng `DefaultPostConditionVerifier` phân tích kết quả trả về của Tool.
   - Nếu Tool trả về rỗng hoặc thông báo lỗi nghiệp vụ (`success: false`), pipeline chuẩn hóa phản hồi và nhúng thêm hướng dẫn phục hồi (`recovery_hint`) giúp LLM biết cách xin lỗi khách hàng lịch sự và chuyển hướng hỗ trợ.

---

## 3. Các Thách Thức Kỹ Thuật & Bài Học Đắt Giá (Post-Mortem)

### 3.1 Bẫy EF Core Local Change Tracker vs Query Database
- **Vấn đề**: Khi một cuộc hội thoại mới được tạo trong `AgentLoop` bằng `_dbContext.Conversations.Add(conversation)`, nếu chưa gọi `SaveChangesAsync`, phương thức `_dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id)` trong `DatabaseAgentStateStore` sẽ gửi truy vấn SQL trực tiếp vào PostgreSQL. Do bản ghi chưa được commit xuống database, SQL trả về `null` khiến `SaveAsync` báo lỗi `NotFound` và bỏ qua việc lưu trạng thái!
- **Giải pháp Senior**: Luôn kiểm tra bộ nhớ đệm cục bộ của EF Core trước khi gửi câu lệnh SQL ra ngoài:
  ```csharp
  var conversation = _dbContext.Conversations.Local.FirstOrDefault(c => c.Id == conversationId)
      ?? await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
  ```
  Đồng thời gọi `await _dbContext.SaveChangesAsync(cancellationToken)` ngay sau khi tạo mới conversation để đảm bảo tính nhất quán dữ liệu.

### 3.2 Sự Cố XML Escaping với Tiếng Việt
- **Vấn đề**: Sử dụng `System.Net.WebUtility.HtmlEncode` ban đầu làm mã hóa sai một số ký tự tiếng Việt có dấu thành các thực thể HTML (`&#...;`), làm méo mó ngữ cảnh của LLM.
- **Giải pháp**: Thay thế bằng hàm mã hóa XML 5 thực thể cốt lõi chuẩn W3C (`&`, `<`, `>`, `"`, `'`) để bảo toàn 100% các ký tự Unicode tiếng Việt có dấu.

### 3.3 Tối Ưu Hóa Token với UnsafeRelaxedJsonEscaping
- **Vấn đề**: Mặc định, `System.Text.Json` mã hóa tất cả ký tự phi ASCII thành chuỗi escape hex dạng `\uXXXX` (ví dụ: `\u1EF1\u1ED1...`). Điều này làm phình to payload gửi cho LLM, lãng phí chi phí token và làm suy giảm khả năng hiểu ngữ nghĩa của mô hình.
- **Giải pháp**: Sử dụng `JsonSerializerOptions` với `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` trên toàn bộ pipeline và bộ lưu trữ. Tiếng Việt được giữ nguyên dạng chuỗi UTF-8 trong sáng.

### 3.4 Khóa Tập Tin DLL Trong Môi Trường Windows (Process Locking)
- **Vấn đề**: Khi tiến trình backend daemon đang chạy ngầm, lệnh `dotnet test` cố gắng rebuild solution sẽ bị lỗi `MSB3027: Could not copy... The file is locked by: TourAgent.Api`.
- **Giải pháp**: Quản lý vòng đời tiến trình bài bản qua công cụ quản lý tác vụ nền (`manage_task`), giải phóng tiến trình trước khi thực thi compile/test toàn cục.

---

## 4. Kết Quả Nghiệm Thu Thực Tế

### 4.1 Bộ Kiểm Thử Tự Động (100% Green)
Toàn bộ **68 bài kiểm tra đơn vị** của hệ thống chạy thành công trọn vẹn:
- **`TourAgent.Domain.UnitTests`**: 35 tests passed.
- **`TourAgent.ArchitectureTests`**: 3 tests passed (Ranh giới kiến trúc Clean Architecture được bảo toàn).
- **`TourAgent.Application.UnitTests`**: 30 tests passed, bao gồm:
  - `AgentStateStoreTests`: Xác thực khởi tạo trạng thái, tăng Version, kiểm soát xung đột OCC `VersionConflict`, và cơ chế `PatchAsync`.
  - `StatusBarBuilderTests`: Xác thực render XML chuẩn, cơ chế escape ký tự đặc biệt, và plugin `domain_context`.
  - `ToolGuardrailPipelineTests`: Xác thực cả 3 pha: chặn tham số lỗi ở Constrain, bọc ngoại lệ ở Execute, chuẩn hóa phản hồi ở Verify & Correct.

### 4.2 Thử Nghiệm Tác Nhân Trực Tiếp (Live End-to-End Test)
Gửi yêu cầu chat tư vấn qua HTTP API:
```bash
POST /api/v1/agent/chat
{
  "conversationId": 33,
  "message": "Cho mình hỏi thêm là tour Nhật Bản này có bao gồm vé máy bay khứ hồi chưa em? SĐT mình 0909349909 nhé."
}
```
**Kết Quả Ghi Nhận**:
1. **Agent Phản Hồi**: Thực thi tự động 2 công cụ liên tiếp qua 3 lượt suy luận: `search_travels` ➔ `get_travel_policies` ➔ Tổng hợp giải thích chi tiết cho khách hàng.
2. **Trạng Thái Lưu Trữ Trong CSDL (`conversations.metadata`)**:
   ```json
   {
     "agent_state": {
       "conversation_id": 1790519982223,
       "version": 2,
       "current_goal": "consulting",
       "workflow_status": "in_progress",
       "confirmed_facts": {
         "customer_phone": "0909349909"
       },
       "domain_state": {},
       "updated_at": "2026-09-27T14:37:16.9666227Z"
     }
   }
   ```
3. **Bản Ghi Kiểm Toán (`llm_calls`)**:
   Lưu lại chính xác **`status_bar_snapshot`** (XML thanh trạng thái gửi cho AI tại từng lượt) và **`state_snapshot`** (ảnh chụp trạng thái tại thời điểm kích hoạt), phục vụ toàn diện cho mục tiêu Observability và Debugging của Senior Engineer.

---

## 5. Điểm Nhấn Cho Buổi Phỏng Vấn Tuyển Dụng Senior / Lead .NET

1. **Từ Prototype đến Production**: Làm rõ sự khác biệt giữa một Chatbot đơn thuần (chỉ biết nối API LLM) và một **Production Agentic System** (có State Store, OCC Lock, Reflection StatusBar và 3-Phase Tool Guardrails).
2. **Kinh Nghiệm Thực Chiến Đa Ngôn Ngữ**: Khả năng đọc hiểu kiến trúc từ PHP 8.4 và chuyển đổi tinh hoa sang .NET 10 bằng các công nghệ chuẩn mực (`System.Text.Json`, `EF Core Local Tracker`, `Dependency Injection`).
3. **Tư Duy Chống Sụp Đổ Hệ Thống (Defensive Programming)**: Tool Guardrail Pipeline ngăn chặn 100% các lỗi format JSON từ LLM làm sập ứng dụng, tự động phục hồi và hướng dẫn mô hình tự sửa sai.

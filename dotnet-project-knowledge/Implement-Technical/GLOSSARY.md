# 📖 BẢNG TRA CỨU NHANH (GLOSSARY) — 5 KỸ THUẬT AGENT HARNESS (TASK-26)

> **Mục tiêu tài liệu:** Giúp bạn xóa bỏ "vibe coding debt", nhớ ngay bản chất của từng class, bảng, cột chỉ trong 1 câu giải thích đời thực, có thể mang ra nói chuyện hoặc debug bất cứ lúc nào.

---

## 🗺️ 1. Bức Tranh Tổng Thể: 5 Kỹ Thuật Kết Hợp Như Thế Nào?

Hãy tưởng tượng **AI Agent** giống như một **nhân viên tư vấn tour mới vào nghề**:

```text
[Khách nhắn tin]
       │
       ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 1. G1 (AgentStateStore): Cuốn sổ tay lưu lại tên khách, SĐT, tour chốt  │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │ Đọc sổ
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 2. G2 (StatusBarBuilder): Dán mảnh giấy nhớ trước mặt AI:               │
│    "<agent_status> Khách tên Tuấn, đang chọn Tour Thái 5N4Đ </...>"     │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │ Gửi vào đầu vào LLM
                                   ▼
                       [AI suy nghĩ & gọi Tool]
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 3. G3 (ToolGuardrailPipeline): Vệ sĩ kiểm soát đầu ra của AI:          │
│    - Constrain: Kiểm tra AI truyền đúng tham số không                  │
│    - Execute: Chạy tool an toàn                                        │
│    - Verify & Correct: Phát hiện lỗi và sửa lại cho chuẩn               │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │ Chạy tool thành công
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 4. D1 (TourGateway & Reducer): Kế toán cập nhật kho vé:                │
│    - TourGateway: Chỉ cho phép hỏi tour qua cổng chuẩn, cấm sờ DB bừa  │
│    - TourStateReducer: Ghi nhận trạng thái tour (Tìm thấy ➔ Đang xem ➔ Chốt)│
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │ Khi cần chuyển nhân viên (Escalate)
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 5. D2 (TourStatusProvider & Resolver): Thám tử xác minh tour:          │
│    - TourReferenceResolver: Truy tìm chính xác 100% khách đang nói     │
│      về tour nào (kể cả sau 50 tin nhắn), không bao giờ đoán bừa       │
│    - Ghi lại bằng chứng: Độ tin cậy (confidence) & Cách tìm (method)   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 🧠 2. Chi Tiết Từng Kỹ Thuật

---

### 🔹 KỸ THUẬT G1: Agent State Store & Versioned Container
* **Nó là gì?** "Cuốn sổ tay ghi nhớ bất biến" của Agent.
* **Nếu KHÔNG có nó:** Chat dài quá 12-20 tin nhắn, các tin cũ bị cắt bớt để tiết kiệm token ➔ **AI bị mất trí nhớ**, hỏi lại những câu vô duyên như *"Anh tên gì?", "Anh đi mấy người?"*. Hoặc khi khách gửi 2 tin nhắn dồn dập, dữ liệu bị ghi đè loạn xạ.

| Khái niệm / Class / Cột | Vị trí / Kiểu dữ liệu | Ý nghĩa 1 câu đời thực |
| :--- | :--- | :--- |
| `conversations.agent_state` | Cột JSON trong DB | Ngăn tủ chứa toàn bộ sổ tay trạng thái của phiên chat này. |
| `AgentStateDTO` | Class PHP (DTO) | Khuôn mẫu chuẩn hoá dữ liệu trạng thái khi code trong PHP. |
| `DatabaseAgentStateStore` | Service Class | Thủ thư chịu trách nhiệm đọc/ghi dữ liệu từ DB vào DTO và ngược lại. |
| `version` (trong JSON) | Số nguyên (`1, 2, 3...`) | **Chốt khóa chống xung đột (OCC)**: Ngăn 2 tin nhắn đến cùng lúc ghi đè làm mất dữ liệu của nhau. |
| `current_goal` | String (`consulting`, ...) | Mục tiêu AI đang làm, tránh bị khách kéo đi lạc đề. |
| `workflow_status` | String (`in_progress`, ...) | AI đang ở giai đoạn nào (đang tư vấn, hay đã chuyển giao cho sale). |
| `confirmed_facts` | Mảng Key-Value | Bộ nhớ bất biến: SĐT, Tên, Số khách — đã lưu là **không bao giờ quên**. |
| `domain_state` | Mảng Key-Value | Ngăn chứa thông tin đặc thù của Tour (Tour nào đang xem, Tour nào đã chốt). |

---

### 🔹 KỸ THUẬT G2: Agent Status Bar Framework
* **Nó là gì?** "Mảnh giấy nhớ dán trước trán AI" trước mỗi lượt trả lời.
* **Nếu KHÔNG có nó:** Sổ tay G1 lưu dữ liệu nhưng **không ai nhắc AI đọc**. AI sẽ tự tưởng tượng ra tiến độ và nói dối: *"Em đã book vé cho anh rồi"* dù thực tế chưa hề gọi tool tạo vé.

| Khái niệm / Class | Vị trí | Ý nghĩa 1 câu đời thực |
| :--- | :--- | :--- |
| `StatusBarBuilder` | Service Class | Máy in tự động gom dữ liệu từ G1 và D2 đóng thành khối XML `<agent_status>`. |
| `<agent_status>` | Khối XML dán vào prompt | Bảng thông báo gắn kèm tin nhắn gửi cho LLM để LLM luôn biết thực tại. |
| `<goal>` & `<status>` | Thẻ XML bên trong status bar | Nhắc AI: *"Mục tiêu hiện tại là gì? Tiến trình đang ở bước nào?"* |
| `<facts>` | Thẻ XML | Nhắc AI: *"Các sự thật đã xác nhận (Tên khách, SĐT)"*. |
| `AgentStatusProviderInterface` | Interface | Cổng cắm (Plugin): Cho phép module Travel (hoặc module khác sau này) tự dán thêm thông tin riêng vào status bar mà không cần sửa core. |

---

### 🔹 KỸ THUẬT G3: Tool Guardrail Pipeline
* **Nó là gì?** "Vệ sĩ 3 lớp kiểm soát" bọc quanh mỗi lần AI gọi Tool.
* **Nếu KHÔNG có nó:** AI thường xuyên bị ảo giác truyền sai tham số (ví dụ: truyền mã tour bậy bạ, thiếu SĐT), hoặc Tool bị lỗi Exception làm sập cả hệ thống chat của khách.

| Khái niệm / Class / Giai đoạn | Vai trò | Ý nghĩa 1 câu đời thực |
| :--- | :--- | :--- |
| `ToolGuardrailPipeline` | Service Class | Vòng kiểm soát chạy qua 3 pha trước - trong - sau khi gọi Tool. |
| **Pha 1: Constrain** | Trước khi chạy Tool | Soi vé: Kiểm tra tham số AI gửi lên có hợp lệ không (có trống không, đúng kiểu dữ liệu không). |
| **Pha 2: Execute** | Lúc chạy Tool | Chạy Tool trong vòng bảo vệ `try-catch`, không để sập ứng dụng nếu có lỗi bất ngờ. |
| **Pha 3: Verify & Correct** | Sau khi chạy Tool | Kiểm tra kết quả trả về, nếu Tool lỗi thì bọc lại thành lời giải thích thân thiện để AI biết đường xin lỗi khách hoặc thử lại. |
| `GuardrailResultDTO` | Class DTO | Giấy chứng nhận: Báo cáo xem Tool chạy thành công hay thất bại, kèm thông điệp đã được làm sạch. |

---

### 🔹 KỸ THUẬT D1: Tour Gateway & State Reducer
* **Nó là gì?** "Cổng giao tiếp kho tour chuẩn chỉ" và "Bộ máy theo dõi vòng đời quan tâm của khách".
* **Nếu KHÔNG có nó:** Tool tự ý query Eloquent model bừa bãi (rò rỉ logic DB vào AI). AI không biết khách đang thích tour nào, đã chốt tour nào hay chỉ mới tìm kiếm sơ qua.

| Khái niệm / Class | Vị trí | Ý nghĩa 1 câu đời thực |
| :--- | :--- | :--- |
| `TourGatewayInterface` & `TourGateway` | Contract & Service | "Cửa sổ một cửa": Tool chỉ được xin dữ liệu tour qua đây, cấm chạm trực tiếp vào Model CSDL. |
| `TourStateReducer` | Service Class | Nhân viên kế toán ngồi nghe sự kiện tool chạy xong để tự động cập nhật `domain_state['travel']`. |
| **Vòng đời: `candidate`** | Trạng thái Tour | Các tour AI vừa tìm thấy qua tìm kiếm (khách có thể đang phân vân). |
| **Vòng đời: `proposed`** | Trạng thái Tour | Tour mà khách vừa bấm vào xem chi tiết (`view_tour_details`). |
| **Vòng đời: `confirmed`** | Trạng thái Tour | Tour mà khách đã gật đầu chọn để chốt giữ chỗ / liên hệ tư vấn viên. |

---

### 🔹 KỸ THUẬT D2: Tour Status Provider & Tour Reference Resolver
* **Nó là gì?** "Thám tử phân giải tour chính xác" và "Hồ sơ kiểm toán minh bạch khi chuyển cho Sale".
* **Nếu KHÔNG có nó:** Đoạn chat dài 50 tin nhắn, khách bảo *"Chốt cho anh tour hồi nãy nhé"*. Hệ thống cũ quét 20 tin nhắn gần nhất (`take(20)`) không thấy đâu ➔ Gán đại một tour số 1 trong danh sách hoặc bỏ trống! Sale nhận ticket không biết vì sao AI lại gán tour đó.

| Khái niệm / Class / Cột | Vị trí | Ý nghĩa 1 câu đời thực |
| :--- | :--- | :--- |
| `TourStatusProvider` | Service Class | Lấy thông tin tour đang active từ `domain_state` đưa vào thẻ XML `<domain_context domain="travel">` của G2. |
| `TourReferenceResolver` | Service Class | **Thám tử 5 tầng**: Lần lượt tìm xem khách đang nói đến tour nào theo thứ tự ưu tiên (Xem chi tiết bảng dưới). |
| `TourResolutionResultDTO` | DTO Class | Kết quả điều tra: Gồm ID tour, độ tin cậy (`0.0 -> 1.0`), phương pháp tìm thấy, và cờ `isAmbiguous` (bị mập mờ). |
| `travel_resolution_method` | Cột trong `agent_escalations` | Bằng chứng: Lưu tên phương pháp tìm ra tour (ví dụ: `state_confirmed`, `explicit_argument`...). |
| `travel_resolution_confidence` | Cột trong `agent_escalations` | Bằng chứng: Điểm tin cậy từ `0.0` đến `1.0` (1.0 = chắc chắn 100%). |
| `metadata` | Cột JSON trong `agent_escalations` | Hộp đen kiểm toán: Lưu toàn bộ manh mối mà thám tử đã dùng để ra quyết định. |

---

## 🎯 3. Thám Tử TourReferenceResolver Hoạt Động Như Thế Nào? (5 Tầng Ưu Tiên)

Khi cần tìm tour (ví dụ lúc Escalate cho Sale), hệ thống hỏi `TourReferenceResolver` qua 5 bước:

1. **Tầng 1 (Tham số rõ ràng - Confidence 1.0):** Tool được AI gọi đích danh kèm `travel_id=123`.
2. **Tầng 2 (Sổ tay trạng thái D1 - Confidence 0.85 -> 1.0):** 
   - Nếu trong sổ đã đánh dấu `confirmed` ➔ Chắc chắn 100% (1.0).
   - Nếu khách vừa xem chi tiết (`proposed`) ➔ Tin cậy 90% (0.90).
   - Nếu cả cuộc trò chuyện chỉ tìm ra đúng 1 tour duy nhất (`single_candidate`) ➔ Tin cậy 85% (0.85).
3. **Tầng 3 (Metadata hội thoại - Confidence 0.85):** Tìm trong thông tin đính kèm của hội thoại.
4. **Tầng 4 (So khớp tiêu đề trong DB - Confidence 0.80 hoặc 0.0):**
   - Quét từ khóa trong tin nhắn đối chiếu danh mục CSDL.
   - ⚠️ **Quy tắc vàng:** Nếu tìm thấy **2 tour trở lên** mà không rõ khách chọn tour nào ➔ Đánh dấu `ambiguous` (mập mờ), trả về `travel_id = null`, **tuyệt đối không đoán mò!**
5. **Tầng 5 (Không tìm thấy):** Trả về null an toàn.

---

## ⚡ 4. Cheat-Sheet "Nói Nhanh Trong 30 Giây" Khi Cần Trình Bày

* **G1:** Giúp AI không bị đãng trí khi chat dài và không bị đè dữ liệu khi mạng giật.
* **G2:** Nhắc bài cho AI trước mỗi tin nhắn để AI không bị ảo giác chém gió tiến độ.
* **G3:** Lớp áo giáp ngăn AI gọi tool bậy bạ và không làm sập server.
* **D1:** Chuẩn hóa cách lấy dữ liệu tour và ghi nhớ hành trình từ xem tour đến chốt tour.
* **D2:** Đảm bảo dù chat 100 tin nhắn vẫn nhận diện đúng tour khách muốn chốt, lưu bằng chứng minh bạch cho Sale.

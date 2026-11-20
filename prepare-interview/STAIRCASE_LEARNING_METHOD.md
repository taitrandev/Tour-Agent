# 🪜 PHƯƠNG PHÁP BẬC THANG KIẾN THỨC (Staircase Learning Method)

> **Mục tiêu:** Xóa bỏ "vibe coding debt" — chuyển từ "cảm giác hiểu" sang "hiểu thật sự, có thể nói lại được".
>
> Vấn đề gốc rễ: AI dump cho bạn cùng lúc nhiều thứ, dẫn đến **não bị quá tải bộ nhớ ngắn hạn** (working memory overload). Bạn "cảm giác hiểu" nhưng thưc tế thì não chỉ kịp **nhận dạng** chứ chưa **tiêu hóa**
>
> **Nguyên tắc cốt lõi:** Mỗi lần chỉ leo 1 bậc. Không ép não nuốt WHAT + WHY + HOW + Trade-off cùng lúc.

---

## 1. Bậc Thang Đầy Đủ

```
                           Bậc 7: FAILURE — Hỏng thì triệu chứng gì?         [Optional — khi vận hành]
                        Bậc 6: TRADE-OFF — Đánh đổi gì? Khi nào KHÔNG nên?   [Optional — kiến trúc lớn]
                     Bậc 5: ƯU / NHƯỢC — Tốt ở đâu? Xấu ở đâu?               [Optional — kiến trúc lớn]
                  Bậc 4: WHEN — Khi nào, điều kiện nào thì áp dụng?          [Required]
               Bậc 3: HOW — Hoạt động bằng cách/cơ chế nào?                  [Required]
            Bậc 2: WHY — Mục đích, giải quyết vấn đề gì? Không có thì sao?   [Required]
         Bậc 1b: WHERE — Nằm ở đâu trong codebase?                           [Optional — khi cần debug]
      Bậc 1a: WHAT — Nó là cái gì?                                           [Required]
```

### Chi tiết từng bậc

| Bậc         | Tên        | Mục đích                                                                                                                      | Câu hỏi mẫu                                                                             | Loại tư duy                             | Required?                 |
| :----------- | :---------- | :------------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------------------------------------------- | :---------------------------------------- | :------------------------ |
| **1a** | WHAT        | Xây bức tranh ban đầu trong não — biết "nó là cái gì" trước khi hỏi bất kỳ điều gì sâu hơn                  | "Nó là cái gì? Mô tả bằng 1-2 câu đời thường."                                 | Nhận diện, mô tả                      | ✅ Required               |
| **1b** | WHERE       | Định vị nhanh trong codebase để sau này biết mở đúng file khi debug, không mò mẫm                                   | "File/class nào chịu trách nhiệm chính?"                                              | Định vị                                | ⚙️ Optional (khi debug) |
| **2**  | WHY         | Hiểu lý do tồn tại — không biết WHY thì sẽ không nhớ lâu và không biết khi nào cần dùng                        | "Nếu KHÔNG có nó, chuyện gì xảy ra? Hậu quả cụ thể là gì?"                    | Hiểu bản chất, mục đích             | ✅ Required               |
| **3**  | HOW         | Hiểu cơ chế bên trong — đây là bậc giúp debug được, không chỉ biết "nó làm gì" mà biết "nó làm thế nào" | "Nó hoạt động bằng cơ chế/nguyên lý gì? (KHÔNG phải code cụ thể)"            | Hiểu cơ chế                            | ✅ Required               |
| **4**  | WHEN        | Biết điều kiện áp dụng — tránh dùng quá tay (over-engineering) hoặc bỏ sót khi cần                                 | "Khi nào cần áp dụng? Khi nào KHÔNG cần?"                                           | Ứng dụng, liên hệ thực tế           | ✅ Required               |
| **5**  | Ưu/Nhược | Nhìn thấy 2 mặt — hiểu hạn chế để không mù quáng áp dụng vào mọi tình huống                                    | "Điểm mạnh chính? Điểm yếu/hạn chế nào cần lưu ý?"                            | Phân tích, đánh giá                  | ⚙️ Optional             |
| **6**  | Trade-off   | So sánh được với phương án thay thế — đây là mức tư duy của người ra quyết định kiến trúc                 | "Để được lợi ích đó, ta phải đánh đổi gì? Có cách nào tốt hơn không?" | So sánh, phản biện, đánh giá 2 mặt | ⚙️ Optional             |
| **7**  | FAILURE     | Biết triệu chứng khi lỗi — không cần đọc lại code mới biết bắt đầu debug từ đâu                                | "Khi nó hỏng, nhìn thấy triệu chứng gì? Nên kiểm tra ở đâu đầu tiên?"       | Vận hành, troubleshooting               | ⚙️ Optional             |

**Điểm hay:** Thứ tự này đi từ  **nhận thức nông → sâu** , hoàn toàn khớp với cách não xử lý thông tin mới:

* **Bậc 1a → 2 (WHAT, WHY):** Xây nền — *"Cái gì? Để làm gì?"* → Nhẹ nhàng, không áp lực, ai cũng trả lời được sau khi đọc GLOSSARY.
* **Bậc 3 (HOW):** Chuyển từ "biết" sang "hiểu" — *"OK tôi biết nó làm gì, nhưng nó làm bằng cách nào?"* → Đây là bậc quan trọng nhất để debug được.
* **Bậc 4 (WHEN):** Bắt đầu liên hệ thực tế — *"Khi nào thì cần? Khi nào thì thừa?"* → Bậc này xây trực giác kỹ thuật.
* **Bậc 5-6 (Ưu/Nhược, Trade-off):** Tư duy phản biện — *"Nó tốt hơn cách khác ở đâu? Đánh đổi gì?"* → Chỉ người thực sự hiểu mới trả lời được. Không cần vội.
* **Bậc 7 (FAILURE):** Tư duy vận hành — chỉ leo khi hệ thống đã lên production hoặc đang gặp lỗi thực tế.

---

## 2. Cách Sử Dụng

### 2.1. Chọn độ sâu phù hợp

| Loại kiến thức                 | Leo đến bậc                       | Ví dụ                                                       |
| :-------------------------------- | :----------------------------------- | :------------------------------------------------------------ |
| Cột DB, field DTO, biến config  | 1a → 2 (2 bậc)                     | `travel_resolution_confidence`, `version`                 |
| Class, Service, Pattern           | 1a → 2 → 3 → 4 (4 bậc)           | `ToolGuardrailPipeline`, `StatusBarBuilder`               |
| Quyết định kiến trúc lớn    | 1a → 2 → 3 → 4 → 5 → 6 (6 bậc) | "Tách TourGateway khỏi Eloquent", "OCC vs Pessimistic Lock" |
| Đang vận hành/debug production | Thêm 1b + 7                         | Bất kỳ component nào đang gây lỗi                       |

### 2.2. Quy tắc leo

1. **Mỗi session:** Chỉ leo **1-2 concept × 2-3 bậc**. Không tham.
2. **Câu kiểm tra:** Sau mỗi bậc, tự **nói lại bằng lời mình** (không phải trắc nghiệm).
   - ❌ Trắc nghiệm: "AgentStateDTO thuộc kỹ thuật nào?" → Chỉ test trí nhớ máy móc
   - ✅ Nói lại: "Giải thích AgentStateDTO là gì bằng 1 câu?" → Buộc não tái tạo
3. **Pass/Fail:** AI xác nhận đúng/sai → Pass thì lên bậc tiếp → Sai thì giải thích lại rồi thử lại.
4. **Không phải concept nào cũng cần leo đủ 7 bậc.** Tham khảo bảng 2.1.

**Ví dụ lịch leo không bị mệt (1 tuần cho 5 kỹ thuật G1-G3, D1-D2):**

> Mỗi buổi = **1 concept × 2 bậc liên tiếp**, ~10-15 phút. Sau đó nghỉ, không leo thêm.

| Buổi           | Làm gì                           | Cảm giác sau khi xong                                             |
| :-------------- | :--------------------------------- | :------------------------------------------------------------------ |
| Buổi 1 (Sáng) | G1: Bậc 1a (WHAT) → Bậc 2 (WHY) | Hiểu*G1 là gì và tại sao cần*                               |
| Buổi 2 (Sáng) | G1: Bậc 3 (HOW) → Bậc 4 (WHEN)  | Hiểu*G1 hoạt động thế nào và khi nào dùng* → G1 xong ✅ |
| Buổi 3         | G2: Bậc 1a → Bậc 2              | Hiểu*G2 là gì và tại sao cần*                               |
| Buổi 4         | G2: Bậc 3 → Bậc 4               | G2 xong ✅                                                          |
| Buổi 5         | G3: Bậc 1a → Bậc 2              | Hiểu*G3 là gì và tại sao cần*                               |
| Buổi 6         | G3: Bậc 3 → Bậc 4               | G3 xong ✅                                                          |
| Buổi 7         | D1: Bậc 1a → Bậc 2              | Hiểu*D1 là gì và tại sao cần*                               |
| Buổi 8         | D1: Bậc 3 → Bậc 4 → Bậc 5     | D1 nâng cao (kiến trúc lớn) ✅                                  |
| Buổi 9         | D2: Bậc 1a → Bậc 2              | Hiểu*D2 là gì và tại sao cần*                               |
| Buổi 10        | D2: Bậc 3 → Bậc 4 → Bậc 5     | D2 nâng cao ✅ — Hoàn thành cả 5 kỹ thuật 🎉                 |

* ✅ **Nên:** Xong 2 bậc thì dừng, dù còn hứng — để não consolidate.
* ✅ **Nên:** Ôn lại bậc cũ bằng 1 câu nói miệng trước khi leo bậc mới.
* ❌ **KHÔNG nên:** Leo G1+G2+G3+D1+D2 đồng loạt cùng 1 buổi.

### 2.3. Tại sao cách này hiệu quả?

| Nguyên lý khoa học           | Giải thích                                                                                                                           |
| :------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------- |
| **Cognitive Load Theory** | Mỗi bậc chỉ 1 loại tư duy → não không bị ngộp                                                                                |
| **Scaffolding**           | Bậc trước là nền móng cho bậc sau. WHAT → WHY → HOW theo trình tự tự nhiên                                                |
| **Sense of Progress**     | Hoàn thành mỗi bậc = phần thưởng nhỏ → tạo động lực                                                                       |
| **Gate-keeping**          | Câu kiểm tra ngăn leo lên tầng mới khi tầng cũ chưa vững                                                                     |
| **Active Recall**         | "Nói lại" buộc não chuyển từ "nhận dạng" sang "tái tạo" — sự khác biệt giữa*cảm giác hiểu* và *hiểu thật sự* |

**Tổng kết**

| Điểm                                                  | Đánh giá                                   |
| ------------------------------------------------------- | --------------------------------------------- |
| Ý tưởng bậc thang 5 tầng                           | ✅ Rất tốt, đúng khoa học nhận thức    |
| Câu kiểm tra giữa các bậc                          | ✅ Quan trọng, nên dùng format "nói lại" |
| Bổ sung: Phân loại nông/sâu theo loại kiến thức | 💡 Không phải cái nào cũng cần 5 bậc   |
| Bổ sung: Giới hạn 1-2 concept/session                | 💡 Chống quá tải                           |

**Thực tế sử dụng:**

* **Học nhanh (MVP):** Bậc 1a → 2 → 3 → 4 (4 bậc, đủ dùng)
* **Học kỹ:** Thêm bậc 5 + 6
* **Đang debug/vận hành:** Thêm 1b + 7

---

## 3. Prompt Template

### 3.1. Prompt bắt đầu 1 buổi leo bậc thang

```
Chào AI, tôi muốn leo bậc thang kiến thức theo phương pháp tại:
@project-knowledge/STAIRCASE_LEARNING_METHOD.md

Hôm nay tôi muốn học kỹ thuật:
- Tên kỹ thuật: Kỹ thuật G1: Agent State Store & Versioned State Container
- Mục tiêu buổi này: Bậc 1a (WHAT) ➔ Bậc 2 (WHY)

Tài liệu nguồn để bạn tham khảo kiến thức:
- Source of Truth kỹ thuật: @project-knowledge/Implement-Technical-For-Agent--Chat-Session/AGENT_HARNESS_5_TECHNIQUES_SOT.md
- Từ điển tra cứu nhanh: @project-knowledge/GLOSSARY.md

Quy tắc kèm học:
1. Bây giờ CHỈ đưa ra duy nhất Bậc 1a (WHAT).
2. Dựa vào file SOT và GLOSSARY, hãy giải thích ngắn gọn bằng 1-2 câu đời thường, trực quan.
3. Đặt cho tôi 1 câu hỏi "nói lại bằng lời mình" để kiểm tra xem tôi đã hiểu chưa.
4. TUYỆT ĐỐI KHÔNG đưa ra Bậc 2 ngay bây giờ. Hãy dừng lại và chờ tôi trả lời câu kiểm tra của Bậc 1a!
```

### 3.2. Prompt khởi động session leo bậc

Dùng prompt này khi muốn bắt đầu leo bậc thang cho 1 concept mới:

```
Tôi muốn leo bậc thang kiến thức cho: [TÊN CONCEPT]

Ngữ cảnh: [MÔ TẢ NGẮN VỀ CONCEPT NÀY TRONG DỰ ÁN CỦA TÔI — nếu có]

Độ sâu mong muốn: [CHỌN 1]
- Nhanh (Bậc 1a → 2 → 3 → 4)
- Kỹ (Bậc 1a → 2 → 3 → 4 → 5 → 6)
- Debug (Thêm 1b + 7)

Quy tắc:
- Mỗi lần chỉ đưa ra 1 bậc
- Sau mỗi bậc, hỏi tôi 1 câu "nói lại bằng lời mình" để kiểm tra
- Nếu tôi trả lời đúng → lên bậc tiếp
- Nếu tôi trả lời sai hoặc thiếu → giải thích lại rồi hỏi lại
- Dùng ví dụ thực tế từ dự án của tôi (nếu có)
- Dùng tiếng Việt, ngôn ngữ đời thường, tránh jargon không cần thiết
```

### 3.3. Prompt tiếp tục session (khi đã pass vài bậc)

```
Tiếp tục leo bậc thang cho: [TÊN CONCEPT]
Tôi đã pass đến bậc: [SỐ BẬC]
Hãy đưa ra bậc tiếp theo.
```

### 3.4. Prompt leo nhiều concept trong 1 session

```
Tôi muốn leo bậc thang cho các concept sau (mỗi concept chỉ 2-3 bậc/session):

1. [CONCEPT A] — hiện tại ở bậc [X], muốn leo đến bậc [Y]
2. [CONCEPT B] — hiện tại ở bậc [X], muốn leo đến bậc [Y]

Quy tắc: Hoàn thành concept A trước, rồi mới chuyển sang B.
```

---

## 4. Ví Dụ Cụ Thể

---

### 4.1. Leo bậc thang cho G1: Agent State Store

---

#### 📍 Bậc 1a — WHAT

**AI giải thích:**

> Agent State Store là "cuốn sổ tay ghi nhớ" của AI Agent. Nó lưu lại những thông tin quan trọng trong suốt cuộc hội thoại (tên khách, SĐT, tour đang chọn, mục tiêu hiện tại) vào 1 cột JSON trong database, để AI không bao giờ bị quên dù chat dài bao nhiêu.

**✋ Câu kiểm tra:**

> *Hãy nói lại: Agent State Store là gì? Dùng 1-2 câu của bạn, không cần chính xác từ ngữ.*

**✅ Ví dụ trả lời đạt:** "Là nơi lưu trữ trạng thái, thông tin quan trọng của cuộc trò chuyện để AI không bị quên khi chat dài."

---

#### 📍 Bậc 2 — WHY

**AI giải thích:**

> Hệ thống chỉ gửi 12-20 tin nhắn gần nhất cho LLM (để tiết kiệm token và tốc độ). Nếu khách nói tên + SĐT ở tin nhắn số 2, rồi hỏi linh tinh 30 tin, đến tin số 35 khách bảo "lên đơn cho anh nhé" — tin nhắn số 2 đã bị cắt, AI bị mất trí nhớ và hỏi lại câu vô duyên: "Anh tên gì? SĐT bao nhiêu?".
>
> Agent State Store lưu `confirmed_facts` ra ngoài lịch sử chat, nên dù tin nhắn bị cắt, dữ liệu vẫn còn nguyên 100%.
>
> Ngoài ra, khi khách gửi 2 tin nhắn dồn dập (mạng Telegram retry), cột `version` (OCC) ngăn 2 tiến trình ghi đè nát dữ liệu của nhau.

**✋ Câu kiểm tra:**

> *Nếu KHÔNG có Agent State Store, chuyện gì xảy ra khi khách chat 40 tin nhắn? Kể 1 tình huống cụ thể.*

**✅ Ví dụ trả lời đạt:** "Khách nói SĐT ở đầu chat, sau 30 tin thì tin đó bị cắt khỏi context gửi cho LLM, AI bị quên và hỏi lại SĐT → khách bực mình."

---

#### 📍 Bậc 3 — HOW

**AI giải thích:**

> Cơ chế hoạt động gồm 3 phần:
>
> 1. **Cột `agent_state` (JSON)** trong bảng `conversations`: Lưu toàn bộ trạng thái dưới dạng JSON có cấu trúc (`version`, `current_goal`, `workflow_status`, `confirmed_facts`, `domain_state`).
> 2. **AgentStateDTO**: Khuôn mẫu PHP chuẩn hóa dữ liệu JSON đó. Code không đọc JSON thô mà đọc qua DTO.
> 3. **DatabaseAgentStateStore**: Thủ thư đọc/ghi. Khi đọc: lấy JSON từ DB → chuyển thành DTO. Khi ghi: so sánh `version` trong DTO với `version` trong DB — nếu khớp thì ghi + tăng version, nếu không khớp (có người khác ghi trước) thì từ chối → đây là OCC (Optimistic Concurrency Control).

**✋ Câu kiểm tra:**

> *Giải thích cơ chế chống xung đột (OCC) hoạt động như thế nào khi 2 tin nhắn đến cùng lúc? Dùng ví dụ.*

**✅ Ví dụ trả lời đạt:** "Tin nhắn A và B cùng đọc version=5. A ghi xong, version tăng lên 6. B cũng muốn ghi nhưng version trong tay B vẫn là 5, DB đã là 6 → không khớp → từ chối ghi → tránh đè mất dữ liệu của A."

---

#### 📍 Bậc 4 — WHEN

**AI giải thích:**

> **Cần Agent State Store khi:**
>
> - Hội thoại có thể dài (>10-15 tin nhắn)
> - Agent cần nhớ thông tin qua nhiều lượt (tên, SĐT, sở thích, tour đã chọn)
> - Có nguy cơ đồng thời (multi-channel: Telegram + Zalo cùng 1 khách)
>
> **KHÔNG cần khi:**
>
> - Chatbot trả lời 1 lượt rồi quên (FAQ bot, không cần ngữ cảnh)
> - Hội thoại luôn ngắn (<5 tin nhắn) và không cần nhớ gì

**✋ Câu kiểm tra:**

> *Một bot FAQ trả lời "giờ mở cửa là 8h-17h" rồi kết thúc. Bot này có cần Agent State Store không? Tại sao?*

**✅ Ví dụ trả lời đạt:** "Không cần, vì bot FAQ không cần nhớ gì giữa các câu hỏi, mỗi câu hỏi độc lập, chat luôn ngắn."

---

### 4.2. Leo bậc thang cho D1: Tour Gateway & State Reducer

---

#### 📍 Bậc 1a — WHAT

**AI giải thích:**

> D1 gồm 2 thành phần:
>
> - **TourGateway**: "Cửa sổ một cửa" để lấy dữ liệu tour. Mọi Tool muốn tìm tour, xem chi tiết tour đều phải hỏi qua Gateway, cấm truy vấn DB trực tiếp.
> - **TourStateReducer**: "Kế toán tự động" ngồi nghe sự kiện tool chạy xong, rồi cập nhật trạng thái tour trong `domain_state['travel']` (tour này đang ở bước: tìm thấy → đang xem → đã chốt).

**✋ Câu kiểm tra:**

> *Nói lại TourGateway và TourStateReducer là gì? Mỗi cái 1 câu.*

**✅ Ví dụ trả lời đạt:**

> - "TourGateway là đầu mối duy nhất để tool lấy dữ liệu tour, không cho tool tự query DB."
> - "TourStateReducer là bộ phận tự động ghi nhận trạng thái tour (tìm → xem → chốt) sau mỗi lần tool chạy."

---

#### 📍 Bậc 2 — WHY

**AI giải thích:**

> **Không có TourGateway:** Mỗi Tool tự viết query Eloquent riêng. Tool A query kiểu này, Tool B query kiểu kia → logic DB rò rỉ khắp nơi → sửa 1 chỗ phải sửa 5 chỗ → dễ bug.
>
> **Không có TourStateReducer:** AI tìm thấy 3 tour Thái, khách bảo "xem cái thứ 2", rồi bảo "chốt luôn" → Nhưng không ai ghi lại quá trình này. Đến lúc Escalate, hệ thống không biết khách đã chốt tour nào hay chỉ mới xem sơ qua.

**✋ Câu kiểm tra:**

> *Nếu không có TourStateReducer, khi khách nói "chốt tour hồi nãy cho anh", hệ thống gặp khó khăn gì?*

**✅ Ví dụ trả lời đạt:** "Hệ thống không biết khách đã 'chốt' tour nào vì không có ai ghi lại việc khách chuyển từ 'xem' sang 'chốt'. Phải đoán hoặc quét lại tin nhắn cũ — dễ sai."

---

#### 📍 Bậc 3 — HOW

**AI giải thích:**

> **TourGateway hoạt động bằng cách:**
> Đặt 1 interface (`TourGatewayInterface`) làm hợp đồng. Tool chỉ gọi các method trong interface (ví dụ: `searchTours()`, `getTourDetail()`). Bên trong Gateway mới chạy Eloquent query. Nếu mai sau đổi từ Eloquent sang API bên ngoài → chỉ sửa Gateway, không sửa Tool.
>
> **TourStateReducer hoạt động bằng cách:**
> Đăng ký lắng nghe hook `TOOL_EXECUTED`. Mỗi khi bất kỳ tool nào chạy xong, Reducer kiểm tra:
>
> - Tool `search_tours` chạy → ghi các tour tìm thấy vào `domain_state.candidates[]`
> - Tool `view_tour_details` chạy → đánh dấu tour đó là `proposed`
> - Tool `escalate_to_human` chạy → đánh dấu tour đó là `confirmed`
>
> Tất cả ghi vào `domain_state['travel']` trong Agent State Store (G1).

**✋ Câu kiểm tra:**

> *Khi khách tìm tour Nhật, xem chi tiết 1 tour, rồi chốt — TourStateReducer ghi lại 3 bước đó vào đâu và trạng thái lần lượt là gì?*

**✅ Ví dụ trả lời đạt:** "Ghi vào `domain_state['travel']`. Bước 1: search → ghi danh sách `candidates`. Bước 2: view detail → đánh dấu tour đó `proposed`. Bước 3: escalate → đánh dấu `confirmed`."

---

#### 📍 Bậc 4 — WHEN

**AI giải thích:**

> **Cần D1 khi:**
>
> - Agent có nhiều Tool cùng truy cập 1 nguồn dữ liệu (tour) → cần Gateway chuẩn hóa
> - Agent cần theo dõi "hành trình quan tâm" của khách qua nhiều lượt tool call → cần Reducer
> - Có nhu cầu biết chính xác khách đang ở giai đoạn nào (tìm kiếm / đang xem / đã chốt)
>
> **KHÔNG cần khi:**
>
> - Chỉ có 1 tool duy nhất truy cập tour (không cần Gateway, query trực tiếp cũng được)
> - Agent không cần nhớ trạng thái giữa các tool call (mỗi tool độc lập, không liên quan nhau)

**✋ Câu kiểm tra:**

> *Một chatbot chỉ có 1 tool là `search_tours` (tìm và trả kết quả, không có xem chi tiết hay chốt). Bot này có cần TourStateReducer không? Tại sao?*

**✅ Ví dụ trả lời đạt:** "Không cần, vì chỉ có 1 bước duy nhất (tìm kiếm), không có hành trình candidate → proposed → confirmed để theo dõi."

---

## 5. Theo Dõi Tiến Độ Leo Bậc

Dùng bảng này để tự đánh dấu tiến độ:

| Concept                    | Bậc 1a | Bậc 1b | Bậc 2 | Bậc 3 | Bậc 4 | Bậc 5 | Bậc 6 | Bậc 7 | Ghi chú                              |
| :------------------------- | :-----: | :-----: | :----: | :----: | :----: | :----: | :----: | :----: | :------------------------------------ |
| G1: Agent State Store      |   ☐   |   —   |   ☐   |   ☐   |   ☐   |   —   |   —   |   —   | Mục tiêu: 4 bậc                    |
| G2: Status Bar             |   ☐   |   —   |   ☐   |   ☐   |   ☐   |   —   |   —   |   —   | Mục tiêu: 4 bậc                    |
| G3: Tool Guardrails        |   ☐   |   —   |   ☐   |   ☐   |   ☐   |   —   |   —   |   —   | Mục tiêu: 4 bậc                    |
| D1: Tour Gateway & Reducer |   ☐   |   —   |   ☐   |   ☐   |   ☐   |   ☐   |   ☐   |   —   | Mục tiêu: 6 bậc (kiến trúc lớn) |
| D2: Resolver & Provider    |   ☐   |   —   |   ☐   |   ☐   |   ☐   |   ☐   |   ☐   |   —   | Mục tiêu: 6 bậc (kiến trúc lớn) |

> **Chú thích:** ☐ = chưa leo | ✅ = đã pass | — = bỏ qua (optional, chưa cần)

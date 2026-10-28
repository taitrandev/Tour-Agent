# 🧠 LESSONS_LEARNED.md — Cẩm Nang Kỹ Thuật & Kiến Thức Cốt Lõi AI Agent

> **Tài liệu chưng cất kiến thức kỹ thuật & kinh doanh (Knowledge Distillation)** lưu lại các nguyên lý thiết kế, mô hình 5 trụ cột, tư duy Product vs Offer, và bài học kinh nghiệm trong quá trình phát triển hệ thống **AI Agent**.
> Các kiến thức trong file này có tính **tổng quát cao (general principles)**, áp dụng được cho bất kỳ dự án AI Agent / E-commerce / SaaS nào trong tương lai.

---

## 💎 1. 6 Thành Phần Cốt Lõi Của Một Tool Chuẩn (Function Calling Specification)

Theo tiêu chuẩn công nghiệp của **OpenAI Function Calling, Anthropic Tool Use và Gemini Tools**, một Tool chuẩn không chỉ là một hàm code thông thường mà là một giao ước (Contract) đa chiều gồm đúng **6 thành phần bắt buộc**:

```php
interface AgentToolInterface
{
    public function name(): string;
    public function description(): string;
    public function schema(): array;
    public function requiresConfirmation(): bool;
    public function preview(array $arguments): array;
    public function execute(array $arguments, ?int $userId = null): array;
}
```

```
┌────────────────────────────────────────────────────────────────────────┐
│                        CẤU TRÚC 1 TOOL CHUẨN                           │
├────────────────────────────────────────────────────────────────────────┤
│ 1. name                  → Định danh duy nhất (snake_case)            │
│ 2. description           → Ngữ nghĩa để LLM hiểu KHI NÀO nên dùng     │
│ 3. schema (JSON Schema)  → Cấu trúc tham số đầu vào (Types & Rules)    │
│ 4. requiresConfirmation  → Phân loại an toàn: Read-only vs Sensitive   │
│ 5. preview               → Tóm tắt cho người dùng duyệt trước khi chạy│
│ 6. execute               → Gọi API / Service thật để ghi dữ liệu       │
└────────────────────────────────────────────────────────────────────────┘
```

### Chi Tiết Từng Thành Phần:

1. **`name(): string` (Tên định danh duy nhất)**:
   * Viết dạng `snake_case`, ngắn gọn, rõ nghĩa (Ví dụ: `update_offer_price`, `get_offers`, `add_bonus_item`).
   * LLM sẽ trả về tên này trong trường `tool_calls[0].name` khi quyết định kích hoạt công cụ.

2. **`description(): string` (Mô tả công dụng & ngữ cảnh)**:
   * **Đây là phần quan trọng nhất đối với LLM!** LLM không đọc code PHP bên trong; nó chỉ đọc dòng `description` này để suy luận xem câu nói của người dùng có khớp với công năng của Tool hay không.
   * *Nguyên tắc viết:* Phải nêu rõ hành động, đối tượng tác động và giá trị mang lại (Ví dụ: *"Cập nhật giá bán ưu đãi (offer_price) và tự động tính lại tiền cọc 30% cho gói sản phẩm"*).

3. **`schema(): array` (JSON Schema tham số)**:
   * Định nghĩa chặt chẽ theo chuẩn **JSON Schema (draft-07)**:
     - `type`: `object`
     - `properties`: Danh sách các tham số kèm kiểu (`number`, `string`, `integer`, `boolean`, `enum`...) và mô tả chi tiết của từng tham số.
     - `required`: Mảng các tham số bắt buộc phải có.
   * Giúp LLM tự động trích xuất đúng kiểu dữ liệu từ ngôn ngữ tự nhiên (Ví dụ: từ *"ba triệu rưỡi"* trích xuất thành số `3500000`).

4. **`requiresConfirmation(): bool` (Chính sách an toàn)**:
   * Phân loại cấp độ rủi ro:
     - `false` (Read-only): Chỉ đọc dữ liệu (vd: xem danh sách sản phẩm) ➔ Thực thi ngay lập tức, trả kết quả tức thì.
     - `true` (Sensitive/Write): Các thao tác thay đổi giá, đổi slot, xóa dữ liệu ➔ Bắt buộc dừng lại để kích hoạt **2-Step Confirmation Guard**.

5. **`preview(array $arguments): array` (Bản xem trước cho con người)**:
   * Chuyển đổi các tham số kỹ thuật thô thành ngôn ngữ tiếng Việt trực quan, dễ hiểu (Ví dụ: *"Đổi giá gói Combo từ 3.29tr sang 3.5tr (Cọc 30% = 1.05tr)"*).
   * Dữ liệu này được hiển thị lên Card xác nhận màu vàng cam trên giao diện trước khi gọi API.

6. **`execute(array $arguments, ?int $userId = null): array` (Thực thi nghiệp vụ)**:
   * Chạy qua các tầng Service/Repository của Laravel (có đầy đủ validation, tính toán phụ, hook events).
   * Tự động ghi lại nhật ký vào bảng `agent_audit_logs`.

---

## 🔌 2. Kiến Trúc Một LLM Driver Chuẩn (LlmDriverInterface)

Để hệ thống có thể chuyển đổi linh hoạt giữa các nhà cung cấp AI (**DeepSeek, OpenAI, Gemini, Anthropic**) mà không làm vỡ code ứng dụng, Driver cần tuân thủ **5 phương thức tiêu chuẩn**:

```php
interface LlmDriverInterface
{
    public function name(): string;
    public function chat(array $messages, array $tools = [], array $options = []): array;
    public function synthesize(array $messages, string $toolCallId, string $toolName, array $toolResult, array $options = []): string;
    public function testConnection(): array;
    public function getAvailableModels(): array;
}
```

| Phương thức | Ý nghĩa & Nhiệm vụ |
|---|---|
| **1. `name(): string`** | Tên driver (Ví dụ: `deepseek`, `openai`, `gemini`, `simulator`). |
| **2. `chat(messages, tools, options)`** | Gửi tin nhắn và danh sách Tool Schemas lên LLM, sau đó chuẩn hóa phản hồi: nếu LLM gọi Tool ➔ trả về `tool_calls`; nếu LLM trả lời text ➔ trả về `content`. |
| **3. `synthesize(...)`** | Đưa kết quả chạy Tool quay lại cho LLM để tổng hợp thành câu trả lời tự nhiên cho người dùng (Agentic Follow-up Synthesis). |
| **4. `testConnection(): array`** | Gửi một request ping nhẹ (`model list` hoặc 1 prompt ngắn) để kiểm tra API Key còn hạn và kết nối mạng ổn định hay không. |
| **5. `getAvailableModels(): array`** | Trả về danh sách model mà driver này hỗ trợ (Ví dụ DeepSeek: `deepseek-chat`, `deepseek-reasoner`). |

---

## 🛡️ 3. Bài Học Về Bảo Mật & Ổn Định Khi Vận Hành AI Agent

1. **Nguyên Tắc "No Direct DB Access":**
   * Không bao giờ cho LLM sinh câu lệnh SQL hoặc chọc thẳng vào Database.
   * Mọi hành động của AI phải đi qua REST API / Service Layer để giữ nguyên toàn bộ Business Rules (Tính cọc tự động, Hook Events, Soft-delete).
2. **2-Step Confirmation Guard (Human-in-the-loop):**
   * Mọi hành động làm thay đổi tiền hoặc xóa dữ liệu đều phải sinh `confirmation_token` tạm thời trong Cache. Chỉ khi người dùng bấm "Xác Nhận" thì lệnh mới được chạy.
3. **Audit Trail (Lưu Vết):**
   * Mọi lệnh từ AI đều được lưu vào bảng `agent_audit_logs` kèm `previous_state` (dữ liệu cũ) và `new_state` (dữ liệu mới) để dễ dàng rollback khi cần.

---

## ⚡ 4. Bài Học Về Prompt Caching & Agentic Tool Pattern

### ❌ Sai lầm (Anti-Pattern): Nhúng Biến Động (Thời Gian, Giây) Vào System Prompt
* Các mô hình hiện đại như **DeepSeek V3, Claude 3.5, OpenAI GPT-4o** sử dụng cơ chế **Prefix Prompt Caching** (KV Cache) để tái sử dụng bộ nhớ đệm từ những token đầu tiên của System Prompt.
* Nếu chúng ta nhúng ngày giờ động vào System Prompt (ví dụ: `22:45:10`), **mỗi giây System Prompt sẽ thay đổi một lần** ➔ Làm vỡ toàn bộ bộ đệm Prompt Cache ➔ Tăng 10x chi phí API và làm chậm thời gian phản hồi (TTFT - Time to First Token).

### ✅ Giải pháp chuẩn: Giữ System Prompt 100% TĨNH & Cung Cấp Tool `get_current_time`
* **System Prompt luôn cố định 100%**: Chỉ chứa vai trò, tính cách và hướng dẫn hành vi chung để **luôn đạt tỷ lệ Cache Hit cao nhất**.
* **Trang bị Tool `get_current_time`**: Khi người dùng hỏi *"hôm nay thứ mấy"*, *"mấy giờ rồi"*, LLM sẽ chủ động gọi tool `get_current_time` để lấy ngày giờ thực tế từ hệ điều hành và trả lời tự nhiên.

---

## 🔍 5. Hiện Tượng Rò Rỉ Cú Pháp DSML (DeepSeek Markup Language) & Fallback Parser

### Vấn Đề:
* **DeepSeek V3** được huấn luyện với cú pháp nội bộ gọi là **DSML** (`< | | DSML | | tool_calls> ... < | | DSML | | invoke name="update_offer_pricing"> ...`).
* Đôi khi, thay vì trả về trường JSON `tool_calls` qua giao thức API chuẩn, DeepSeek lại in chuỗi DSML thô trực tiếp vào nội dung tin nhắn (`content`), kèm theo tên tool bị biến thể (ví dụ: `update_offer_pricing` thay vì `update_offer_price`).
* Hậu quả: Backend không bắt được `tool_calls` chuẩn nên xem đó là tin nhắn văn bản thường, in nguyên đoạn mã XML/DSML lên màn hình người dùng và không kích hoạt được Card Xác Nhận.

### Giải Pháp Triệt Để Trong `DeepSeekDriver.php`:
1. **DSML Fallback Regex Parser**: Nếu `tool_calls` rỗng nhưng `content` có chứa thẻ `< | | DSML | | invoke ...>`, Driver sẽ tự động bóc tách tên tool và tham số (`offer_id`, `offer_price`) và biến thành `tool_call` hợp lệ.
2. **Tool Name Normalizer (Alias Mapping)**: Tự động chuẩn hóa các biến thể tên tool (`update_offer_pricing` ➔ `update_offer_price`, `add_gift` ➔ `add_bonus_item`).
3. **Giảm Temperature**: Đặt `temperature = 0.1` khi gọi function calling để mô hình tuân thủ tuyệt đối cấu trúc API chuẩn.

---

## 🏛️ 6. Mô Hình 5 Trụ Cột Kinh Doanh & Ánh Xạ Kiến Trúc Phần Mềm (5 Business Pillars Framework)

Mọi hệ thống kinh doanh tạo ra dòng tiền bền vững (theo phương pháp luận của *Alex Hormozi, Sabri Suby, ASSP*) đều cấu thành từ **5 Trụ Cột Cốt Lõi**. Trong kiến trúc phần mềm hướng module, mỗi trụ cột tương ứng với một phân hệ độc lập:

```
┌────────────────────────────────────────────────────────────────────────┐
│                   HỆ THỐNG 5 TRỤ CỘT KINH DOANH & PHẦN MỀM             │
├───────────────────┬───────────────────────────────────┬────────────────┤
│ Trụ Cột           │ Định Nghĩa Kinh Doanh             │ Nhiệm Vụ Kỹ Thuật (Module)│
├───────────────────┼───────────────────────────────────┼────────────────┤
│ 1. OFFER          │ "Lời đề nghị không thể từ chối"   │ Module `Offer` │
│    (Đóng Gói)     │ Giá trị > Giá bán, Value Stack,   │ Quản lý giá, slot khan hiếm, │
│                   │ 3 Lớp Cam Kết, Quà tặng số 0đ.    │ tiền cọc, bảo chứng rủi ro.  │
├───────────────────┼───────────────────────────────────┼────────────────┤
│ 2. ATTRACTION     │ "Hút khách hàng tiềm năng"        │ Module `Content / Ads` │
│    (Thu Hút)      │ Kéo đúng tệp Dream Buyer qua Ads, │ Bộ Ad Copy 3 Angle, Kịch bản │
│                   │ Video VSL, SEO, Content Organic.  │ VSL, Lead Magnet, Thumbnail. │
├───────────────────┼───────────────────────────────────┼────────────────┤
│ 3. CONVERSION     │ "Biến quan tâm thành tiền cọc"    │ Landing Page + AI Agent │
│    (Chuyển Đổi)   │ Landing Page chuyển đổi cao (CRO),│ RAG tư vấn 24/7, xử lý từ chối│
│                   │ AI Agent tư vấn thông minh 4 Chạm.│ chốt cọc trong 15 phút.      │
├───────────────────┼───────────────────────────────────┼────────────────┤
│ 4. DELIVERY       │ "Thực thi & Tạo WOW cho khách"    │ Module `Delivery` │
│    (Bàn Giao)     │ Giao đúng cam kết + Trải nghiệm số│ Onboarding khách, cấp kho quà │
│                   │ vượt kỳ vọng (App cứu trợ, Recap).│ số, app SOS, video recap.    │
├───────────────────┼───────────────────────────────────┼────────────────┤
│ 5. OPERATION      │ "Vận hành tự động & Kiểm soát"    │ Module `AiAgent` Commander │
│    (Vận Hành)     │ Quản trị hệ thống qua AI Agent,   │ Dashboard Admin, Audit Log,  │
│                   │ điều hành giá/slot, lưu vết an toàn│ phân quyền, giám sát API.    │
└───────────────────┴───────────────────────────────────┴────────────────┘
```

> 💡 **Bài học kiến trúc:** Khi xây dựng bất kỳ hệ thống kinh doanh mới nào, hãy luôn tự hỏi: *"Hệ thống đã có đủ 5 trụ cột này chưa? Trụ cột nào đang là điểm nghẽn (bottleneck) khiến doanh thu chưa bứt phá?"*

---

## 📦 7. Tư Duy Thiết Kế Lõi: "Product (Sản Phẩm)" vs "Offer (Lời Đề Nghị Bán Hàng)"

Một trong những sai lầm phổ biến nhất của các lập trình viên và người làm kinh doanh truyền thống là **đồng nhất Sản Phẩm với Lời Đề Nghị**. Đây là hai thực thể hoàn toàn khác nhau về bản chất:

```
 ┌───────────────────────────────────────────────────────────────┐
 │                   SẢN PHẨM LÕI (CORE PRODUCT)                 │
 │ • Là "Cái mà bạn giao cho khách" (Hàng hóa, dịch vụ, tính năng)│
 │ • Chứa: Thông số kỹ thuật, lịch trình, chi phí sản xuất gốc,  │
 │   tài liệu hướng dẫn, FAQ, điều khoản và chính sách.          │
 └───────────────────────────────┬───────────────────────────────┘
                                 │
                                 │ (1 Product được đóng gói thành Nhiều Offers)
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
 ┌───────────────────────────────┐ ┌───────────────────────────────┐
 │            OFFER A            │ │            OFFER B            │
 │ • Đối tượng: Khách mua sớm    │ │ • Đối tượng: Khách VIP/Gia đình│
 │ • Giá bán ưu đãi: 3.290.000đ  │ │ • Giá bán cao cấp: 4.500.000đ │
 │ • Cọc: 30%                    │ │ • Cọc: 50%                    │
 │ • Slot khan hiếm: 4 suất/tuần │ │ • Slot khan hiếm: 2 suất/tháng│
 │ • Value Stack: Quà số AI 1-7  │ │ • Value Stack: Dịch vụ VIP    │
 │ • 3 Lớp Cam Kết Godfather     │ │ • Cam kết hoàn tiền 100%      │
 └───────────────────────────────┘ └───────────────────────────────┘
```

### So Sánh Chi Tiết:

| Tiêu Chí | Product (Sản Phẩm) | Offer (Lời Đề Nghị) |
|---|---|---|
| **Bản chất** | Hàng hóa / Dịch vụ thuần túy (Commodity). | Gói giải pháp trọn vẹn không thể từ chối. |
| **Tính độc quyền** | Thường dễ bị sao chép (đối thủ cũng có tour/phần mềm tương tự). | **Độc bản 100%** (nhờ bộ quà tặng số, cơ chế cam kết riêng). |
| **Giá cả** | Bị so sánh giá với thị trường. | Thoát khỏi cuộc chiến phá giá (Price Inelastic). |
| **Database Schema** | Bảng `products` (title, description, specs, policy, itinerary, cost_price). | Bảng `offers` (product_id, offer_price, original_price, deposit_rate, slots, guarantees, value_stack). |
| **Mối quan hệ** | **1 Product ───< N Offers** (1 Sản phẩm có thể tạo ra nhiều Offer cho nhiều tệp khách khác nhau). | Thuộc về 1 Product cụ thể. |
| **Nhiệm vụ của AI Agent** | **RAG Knowledge Base**: Agent tra cứu thông tin chi tiết của Product để trả lời thắc mắc (lịch trình, tính năng, quy định). | **Sales & Action**: Agent dùng Offer để báo giá, tặng quà, tạo tính khan hiếm và chốt đơn. |

---

## 🛠️ 8. Quy Chuẩn "Minimal Surgical Edits" (Sửa Cục Bộ Tránh Ghi Đè & Nhiễu Git Diff)

### ❌ Sai lầm (Anti-Pattern): Ghi Đè Toàn Bộ File (`write_to_file`)
* **Hiện tượng Stale Context Overwrite:** Khi lập trình viên tự tay sửa một vài chữ trên IDE, file trên đĩa đã đổi. Nếu AI dùng lệnh ghi đè toàn bộ file từ "ký ức" (Context Window) ở lượt chat trước, nó sẽ **vô tình xóa sạch các thay đổi thủ công của lập trình viên**.
* **Nhiễu Git Diff (Git Diff Pollution):** Ghi đè toàn bộ file làm thay đổi hàng loạt ký tự xuống dòng (CRLF vs LF) và khoảng trắng (spaces/tabs), khiến Git Diff đỏ rực cả file dù chỉ sửa 1 dòng code ➔ Gây mệt mỏi và tốn nhiều năng lượng khi code review.

### ✅ Giải pháp chuẩn: Sửa Phẫu Thuật Cục Bộ (`replace_file_content`)
* **Surgical Precision:** Chỉ nhắm đúng phạm vi dòng bắt đầu và kết thúc (`StartLine` ➔ `EndLine`) cần sửa.
* **Bảo toàn 100% phần còn lại:** Giữ nguyên vẹn toàn bộ cấu trúc, khoảng trắng và các dòng sửa tay của lập trình viên ở những vị trí khác.
* **Git Diff sạch sẽ:** Khi review, Git chỉ hiển thị đúng 1-2 dòng thay đổi trọng tâm, giúp quá trình kiểm duyệt code diễn ra trong vài giây.

---

## 📑 9. Nguyên Tắc Trích Xuất Dữ Liệu: Verbatim Ingestion vs. Creative Copywriting (Tách Biệt 2 Pha)

### ❌ Bản Chất Vì Sao AI Bị "Bệnh Paraphrase / Tóm Tắt Quá Đà":

| Nguyên Nhân | Cơ Chế Bên Dưới | Hậu Quả & Biểu Hiện |
|---|---|---|
| **1. Bản năng nén thông tin (Lossy Compression Bias)** | LLM được pre-train để tổng hợp và diễn đạt tự nhiên theo ngữ cảnh hơn là hoạt động như máy OCR/Transcriber. Khi đọc một đoạn dài 300 từ, LLM mặc định cho rằng *"người dùng cần ý chính ngắn gọn"*. | Tự ý cắt tỉa các câu mô tả cảnh quan, bỏ tính từ, làm mất tính chuyên sâu của tour. |
| **2. Token Economy Bias (Tiết kiệm Token ngầm)** | Các mô hình luôn có cơ chế ngầm tối ưu số lượng output token tạo ra (ngắn hơn = nhanh hơn và tiết kiệm chi phí). | Dễ biến các đoạn văn giàu cảm xúc thành 2-3 gạch đầu dòng khô khan. |
| **3. Thiếu ranh giới lệnh "Zero-Paraphrase"** | Nếu prompt không có quy định nghiêm ngặt: *“Chỉ cắt đoạn văn bản gốc và dán nguyên văn (Verbatim Copy-Paste), cấm đổi dù chỉ 1 chữ”*, AI sẽ coi việc diễn giải lại là hành vi “thông minh”. | Làm biến đổi văn bản pháp lý (chính sách hủy, điều kiện hoàn tiền). |
| **4. Nhầm lẫn giữa 2 giai đoạn (Ingestion vs. Optimization)** | Gộp chung việc **Nhập liệu kỹ thuật (Data Ingestion)** với **Tối ưu hóa bán hàng (Copywriting)** làm một. | Dữ liệu gốc bị biến đổi trước khi Admin kịp kiểm duyệt hoặc lưu trữ. |

### ✅ Giải Pháp Chuẩn Kiến Trúc: Tách Biệt Tuyệt Đối 2 Giai Đoạn
1. **Pha 1: High-Fidelity Verbatim Ingestion (Nhập liệu nguyên văn 100%)**:
   - Sử dụng Parser thô (PHP/Python) trích xuất toàn bộ văn bản và bảng biểu từ `.docx` không qua LLM để không mất một chữ nào.
   - LLM Extraction chỉ làm nhiệm vụ *phân loại khối văn bản (Text Block Categorization)* vào các trường JSON schema tương ứng (`itinerary`, `policies`, `flights`), cấm tuyệt đối việc paraphrase hoặc tóm tắt (`Zero Summarization Rule`).
2. **Pha 2: AI Copy Enhancer (Tối ưu hóa nội dung Landing Page theo yêu cầu)**:
   - Chỉ kích hoạt khi Admin chủ động bấm nút *"✨ Tối ưu nội dung bán hàng bằng AI"*.
   - AI sẽ phân tích văn bản gốc đã nạp, tạo ra phiên bản trình bày cảm xúc, bullet points hấp dẫn cho Landing Page nhưng **vẫn giữ nguyên vẹn dữ liệu kỹ thuật gốc trong CSDL**.

> 📌 **Chi tiết toàn bộ kiến trúc 3 tầng, prompt mẫu chống paraphrase cho model nhỏ và lộ trình kỹ thuật được lưu riêng tại [DOCX_INGESTION_ARCHITECTURE.md](file:///d:/Nghĩa/AI-Agent-Travel/DOCX_INGESTION_ARCHITECTURE.md).**

---

## 🤖 10. Thiết Kế AI Agent: Rào Chắn Nghiệp Vụ (Guardrails) & Quản Trị Bộ Nhớ Hội Thoại (Conversation State)

Trong môi trường vận hành thực tế (Production AI Agent), hai thách thức lớn nhất quyết định sự thành bại của Agent là:
1. **Rào Chắn Nghiệp Vụ (Defense-in-Depth Guardrails)**: Ngăn chặn triệt để hiện tượng AI bị người dùng bẻ khóa (jailbreak), hỏi viết code hoặc trò chuyện ngoài lề làm lãng phí token và tài nguyên doanh nghiệp.
2. **Quản Trị Bộ Nhớ Dài Hạn (Conversation Memory / Sliding Window Context)**: Cân bằng giữa chi phí token khi chỉ nạp 12 tin nhắn gần nhất với việc không làm mất thông tin quan trọng (thành viên đoàn, tuổi trẻ em, ngân sách) từ những tin nhắn cũ.

### 📚 Tài Liệu Tham Khảo Chi Tiết:
> 🔗 **Xem toàn bộ phân tích chuyên sâu, sơ đồ 4 lớp phòng thủ và 3 giải pháp kiến trúc bộ nhớ tại:**  
> 👉 [**LESSONS_LEARNED_AGENT.md**](file:///d:/Nghĩa/AI-Agent-Travel/LESSONS_LEARNED_AGENT.md)

---

## 🛡️ 11. Bài Học Về Kiểm Định Số Liệu (Fidelity) & Phê Duyệt Tức Thời (Human-in-the-Loop)

### 1. Phân biệt Số Liệu Báo Cáo Thống Kê vs. Giá Cả Thương Mại:
* Trong các hệ thống báo cáo marketing (như source GetTime mẫu), phép làm tròn 0.5% (`TOLERANCE = 0.005`) là hợp lý (ví dụ: 109.4 triệu cho 109.400.000đ hoặc tỷ lệ CVR làm tròn).
* Tuy nhiên, trong **Thương mại Du lịch (Travel E-commerce)**, `19.990.000đ` và `20.000.000đ` là hai mức giá niêm yết hoàn toàn khác nhau (chênh lệch 10.000đ tạo hiệu ứng tâm lý giá). Nếu cho phép tolerance rộng kết hợp với phép trừ suy diễn (`25M - 5M = 20M`), mô hình có thể vô tình chấp nhận giá 19.990.000đ dù đó là số bịa.
* **Bài học**: Khi kiểm định giá tiền thương mại, cần kết hợp đối chiếu chặt chẽ với danh mục tour và các đợt khởi hành thực tế trong database.

### 2. Xử Lý Đơn Vị Tiền Tệ & Định Dạng Tiếng Việt Trong NLP Regex:
* Tiếng Việt có đặc thù gõ ký tự tiền tệ ngay sát sau chữ số: `35.990.000đ`, `5.000.000d`, `25tr`, `500k`.
* Nếu dùng regex phân tách từ đơn giản với negative lookahead `(?![\p{L}\d])`, chữ cái `đ` hoặc `d` sẽ khiến bộ parser bỏ qua toàn bộ con số và làm rỗng context nguồn.
* **Giải pháp chuẩn**: Luôn bóc tách suffix tiền tệ (`đ|d|vnd|vnđ|usd|\$`) và các từ chỉ độ lớn (`k|triệu|tr|tỷ`) trước khi kiểm tra ranh giới từ.

### 3. Telegram Human-in-the-Loop & Callback Data Size Limit:
* Telegram giới hạn `callback_data` của Inline Keyboard Button tối đa **64 bytes**.
* Cấu trúc payload cần thiết kế súc tích theo dạng: `{domain}:{action}:{id}` (Ví dụ: `deal:approve:15` chỉ tốn 15 bytes).
* **Idempotency bắt buộc**: Khi Admin đã bấm nút, trạng thái phải chuyển ngay thành `approved`/`rejected` và gọi `editMessageText` để gỡ/đổi nội dung nút, ngăn chặn tuyệt đối lỗi Admin bấm nhiều lần hoặc 2 người cùng bấm.

---

## 🏛️ 12. Tư Duy "Core-First": Xây Nền Tảng Cốt Lõi Trước, Domain Ngành Nghề Xây Sau

### ❌ Sai Lầm Phổ Biến:
* Lao ngay vào viết các tính năng nghiệp vụ bề nổi (như Booking tour, tính cọc khách sạn, chọn đợt bay) khi chưa có hệ thống quản lý danh bạ khách hàng (`crm_contacts`) và đường ống quản trị hội thoại cốt lõi.
* Hậu quả: Dữ liệu khách hàng bị gắn chặt vào bảng Booking du lịch. Khi mở rộng sang ngành nghề khác (bán khóa học, thẩm mỹ, bất động sản) hoặc muốn tái sử dụng AI Agent Engine thì buộc phải đập đi xây lại toàn bộ vì kiến trúc bị "bắt cóc" bởi domain du lịch.

### ✅ Nguyên Tắc Cốt Lõi:
1. **Nền tảng bất biến (Core Invariants)**: Mọi doanh nghiệp đều cần:
   - Danh bạ khách hàng đa kênh (`crm_contacts`): Lưu họ tên, SĐT, email, Telegram Chat ID, Zalo ID.
   - Vòng đời khách hàng chuẩn hóa (`stage`: `prospect` ➔ `warm` ➔ `hot` ➔ `customer` ➔ `lost`).
   - Phiên hội thoại (`conversations` có `contact_id`) và lịch sử tin nhắn (`messages`).
   - Nhật ký quan sát LLM (`llm_calls`, `agent_audit_logs`).
2. **Domain Plugins**: Các bảng nghiệp vụ du lịch (`travels`, `travel_departures`, `offers`, `bookings`) chỉ đóng vai trò là Module nghiệp vụ cắm thêm (Pluggable Domain).
3. **Thứ tự triển khai chuẩn**:
   ```
   Tầng 1: Generic AI Agent Engine & LLM Gateway
            ▼
   Tầng 2: Core CRM Contacts & Unified Inbound Pipeline
            ▼
   Tầng 3: Domain Plugins (Travel / Booking / E-commerce...)
   ```

---

## 🌊 13. Thiết Kế "Unified Inbound Message Pipeline" (Xử Lý Đa Kênh Không Trùng Lặp Code)

### ❌ Vấn đề khi phân tán code theo từng Controller:
* Telegram Controller tự viết code tạo hội thoại, Web Controller tự lưu tin nhắn, Zalo Controller tự xử lý...
* Khi muốn bổ sung logic: "Nếu khách để lại số điện thoại thì tự động chuyển sang stage `hot` và tạo lead CRM", lập trình viên phải sửa ở 3-4 file khác nhau ➔ Dễ sót, khó test và trùng lặp logic nghiêm trọng.

### ✅ Giải Pháp: Đường Ống Tiếp Nhận Duy Nhất (`InboundMessagePipeline`)
Mọi kênh (Telegram Webhook, Web Chat Widget, Zalo Bridge/OA) chỉ làm nhiệm vụ Adapter mỏng: chuyển payload từ raw JSON thành `InboundMessagePayload` rồi đẩy vào **1 đường ống duy nhất**:

```
Inbound Event (Telegram / Web / Zalo)
               │
               ▼
     [Idempotency Guard] ──(Trùng channel_message_id)──> Bỏ qua an toàn (Tránh spam/retry)
               │
               ▼
   [ContactResolverService] ──> Nhận diện theo ChatID/Phone + Auto-detect SĐT VN ➔ Nâng stage 'hot'
               │
               ▼
    [Conversation Linking] ──> Tự động liên kết conversation->contact_id = $contact->id
               │
               ▼
         [AgentLoop] ──> Điều phối suy luận AI + Domain Tools (SaveContact, Escalate...)
               │
               ▼
    [InboundMessageResult] ──> Chuẩn hóa kết quả trả về cho Adapter nguồn gửi phản hồi
```

* **Lợi ích**:
  - Code của các Controllers/Jobs giảm từ 100+ dòng xuống còn 15-20 dòng.
  - Toàn bộ nghiệp vụ CRM, chống spam, bóc tách SĐT, và liên kết hội thoại chỉ nằm tập trung tại đúng 1 nơi.
  - Viết Unit/Feature test cho 1 Pipeline duy nhất là bảo đảm chất lượng cho toàn bộ tất cả các kênh hiện tại và tương lai.

---

## 🚪 14. Kiến Trúc Giao Diện Quản Trị: Tách Route Chuyên Biệt vs. Nhồi Nhét Modal

### ❌ Bài Học Cũ (Anti-Pattern): "Hội Chứng Nhồi Nhét Modal" (Modal Bloat)
* Đưa toàn bộ các tính năng lớn vào chung 1 trang Dashboard `welcome.blade.php` bằng hàng chục `<div class="modal">` (Modal Tour Entry, Modal Offer, Modal Reviews, Modal FAQs, Modal Telegram Diagnostics, Modal Conversations Inspector...).
* **Hậu quả**:
  1. **DOM phình to**: File Blade vượt 3.500 dòng, trình duyệt tải chậm, tốn RAM rendering.
  2. **Xung đột JavaScript**: Các biến toàn cục, sự kiện click, backdrop z-index dễ đè lên nhau.
  3. **Không thể chia sẻ liên kết (No Deep Linking)**: Không thể gửi link trực tiếp cho đồng nghiệp xem trang Khách hàng CRM hay trang Zalo Bridge vì tất cả đều ẩn sau các nút mở popup.

### ✅ Nguyên Tắc Thiết Kế:
1. **Tách Route Độc Lập cho Phân Hệ Lớn**:
   - Phân hệ có luồng nghiệp vụ riêng biệt, bảng dữ liệu lớn hoặc cấu hình phức tạp (như Quản lý Khách hàng CRM `/admin/contacts`, Cấu hình Zalo Hub `/admin/zalo`) phải có **Route, Controller và View Blade riêng**.
2. **Dashboard Chỉ Làm Hub Tổng Quan**:
   - Trang `/admin` chỉ đóng vai trò Dashboard tổng quan (Overview), hiển thị bảng số liệu tóm tắt và các nút điều hướng rõ ràng sang các trang con chuyên biệt.
3. **Khi Nào Mới Dùng Modal?**:
   - Chỉ dùng Modal cho các tác vụ vi mô, tức thời: Xác nhận xóa (Confirm dialog), xem nhanh 1 bức ảnh/lịch trình ngắn, hoặc chỉnh sửa nhanh 1 trường đơn lẻ. Không dùng Modal để làm nguyên cả một màn hình quản trị nghiệp vụ.

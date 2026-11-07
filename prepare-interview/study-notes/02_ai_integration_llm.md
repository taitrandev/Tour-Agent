# 🤖 NHÓM 3: AI Integration & LLM Patterns

> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

## 1. Em hãy mô tả về chức năng chat với Agent

hệ thống giống như là 1 nhân viên ảo tư vấn tour, sẽ trả lời câu hỏi về tour, và chuyển cho nhân viên thật khi cần. Mỗi tin nhắn sẽ đi qua 4 giai đoạn sau

- giai đoạn 1 là chuẩn bị
  hệ thống sẽ ghi lại thông tin khách như là sdt, số người vào 1 quyển sổ ghi chú. đưa cho AI 12 tin nhắn gần nhất cùng những bộ quy tắc làm việc
- giai đoạn 2 là vòng lặp suy luận và gọi công cụ
  AI sẽ có 1 tờ giấy note dán trước mặt để AI ko bị mất trí nhớ, trong tờ giấy sẽ có thông tin khách. AI ko tự nhớ thông tin tour mà phải dùng công cụ để lấy thông tin thật. Mỗi lần AI yêu cầu dùng công cụ gì thì hệ thống sẽ kiểm tra xem có an toàn không. vòng lặp sẽ bị giới hạn 5 lần để để tránh trường hợp AI lặp vô tận và tốn token
- giai đoạn 3 là kiểm tra chống ảo giác
  trước khi gửi câu trả lời cho khách, hệ thống sẽ kiểm tra AI hứa đưa yêu cầu cho nhân viên mà thực ra chưa làm hay ko? nếu chưa làm thì hệ thống sẽ tự làm thay
- giai đoạn 4 là lưu lại tin nhắn, thông tin khách và trả lời
  AI sẽ lo phần nói chuyện, còn những việc quan trọng thì code sẽ kiểm soát

### 1.1 làm cách nào để hệ thống lấy và ghi thông tin khách hàng?

- dùng regex expression để quét tin nhắn và lấy thông tin như là sdt, số người, ngày khởi hành

### 1.2 quyển sổ ghi chú là gì?

* đây gọi là Agent State Store, là 1 phần của harness, được xem là lớp code bao quanh AI. trong đây sẽ lưu các thông tin của khách như là sdt, số người, quyết định. những thông tin này được tách ra từ lịch sử chat, vì vậy cắt bớt tin nhắn cũng không làm mất thông tin quan trọng. các thông tin này được lưu trong database

### 1.3 tại sao là 12 tin nhắn?

* 12 tin là khoảng 6 lượt hỏi đáp giữa khách và AI. những thông tin quan trọng không được mất thì đã lưu trong sổ ghi chú rồi, nên cắt bớt lịch sử cũng không ảnh hưởng. con số này ko cố định, nhưng cần phải có 1 con số giới hạn để tránh việc context phình to dẫn đến tốn token, AI cũng đỡ bị nhiễu

### 1.4 quy tắc làm việc gồm có gì?

- quy tắc ép buộc lấy dữ liệu từ công cụ
- quy tắc hướng dẫn sử dụng công cụ. Như là muốn đặt tour, muốn nói chuyện với nhân viên thì sẽ dùng công cụ escalate-to-human để tạo escalation ticket
- quy tắc trong giao tiếp như là xưng hô lịch sự, thân thiện

### 1.5 tờ giấy note là gì?

- là 1 phần của harness tên Agent Status Bar. Thay vì bắt AI đọc lại lịch sử dễ bị hiện tượng Lost in the middle
- mỗi lượt hệ thống sẽ lấy thông tin trong Agent State Store để build ra Agent Status Bar và nhúng vào cuối message để AI luôn có thông tin quan trọng của khách
- ví dụ đầu chat khách nói muốn đặt tour cho 3 người, nhưng sau 1 khoảng thời gian chat thì đoạn chat đầu bị trôi, context loãng làm AI ko nhớ. đến lúc sau khách nói "book tour cho tôi" thì AI lại hỏi khách là book cho mấy người. việc này dẫn đến trải nghiệm khó chịu đối với khách

### 1.6 kiểm tra hợp lệ là sẽ làm gì?

- đây gọi là Tool Guardrail Pipeline, là 1 phần của harness, được xem là lớp code bao quanh AI. Những từ khóa cần nhớ là constrain - execute - verify & correct.
- constrain sẽ kiểm tra có tool ko? param có đủ và đúng ko? execute sẽ thực thi công cụ như là gọi db hoặc service. Verify và Correct sẽ kiểm tra tool chạy thành công ko? nếu sai thì correct bằng cách giấu lỗi thô với khách và tạo 1 hướng dẫn cho AI để trả lời với khách

### 1.7 tại sao lặp 5 lần?

- con số này cũng không phải cố định. nhưng cần phải có 1 con số để giới hạn vòng lặp
- sẽ có trường hợp gọi tool có vấn đề thì AI sẽ qua turn tiếp theo và thử sử dụng tool khác, hành động sai này nếu rơi vào vòng lặp vô tận thì sẽ tốn chi phí và khách phải chờ lâu

### 1.8 kiểm tra chống ảo giác là sao?

- đây là lớp phòng thủ cuối mà e đã gặp, AI nói đã đưa yêu cầu cho nhân viên nhưng thật sự là chưa. Hiện tại e đang dùng regex expression để quét câu trả lời, nếu phát hiện có escalation id nhưng chưa gọi công cụ tạo escaltion ticket thì hệ thống sẽ chủ động kích hoạt công cụ để tạo ticket
- để hiểu được vấn đề thì có thể tưởng tượng như sau: đây là hiện tượng "role hallucination". vì trong prompt hướng dẫn là "nếu khách muốn gặp nhân viên thì hãy tạo ticket có dạng ESC-...". thay vì trả về tool call để backend tạo ticket thì AI nhập luôn vai và tự sinh ra Id giống mẫu trong prompt

## 2. AI rất hay bị ảo giác dẫn đến bịa giá, thông tin. làm sao để bảo đảm AI đưa đúng thông tin thật từ trong db?

Nguyên tắc cốt lõi là: AI chỉ làm việc liên quan ngôn ngữ, mọi dữ liệu tour đều phải được lấy từ tool call

Để giảm vấn đề ảo giác của AI thấp nhất có thể thì e dùng những tầng sau:

* Tầng 1 là hạ temperature và thiết kế system prompt

  * hạ temperature xuống 0.2 để giảm tính ngẫu nhiên và sáng tạo của AI
  * thêm các quy tắc trong system prompt như là chỉ lấy dữ liệu từ tool call, nếu tool không trả dữ liệu thì phải thông báo với khách là không có dữ liệu. khi có yêu cầu đặt tour hoặc tư vấn kỹ thì dùng escalation tool
* Tầng 2, nguồn dữ liệu thật duy nhất là tool call

  * AI không trực tiếp ghi nhớ hay đoán tour trong dữ liệu huấn luyện. AI chỉ đóng vai trò trích xuất tham số từ message và tạo tool call. Backend sẽ thực thi tool call để lấy dữ liệu từ database
* Tầng 3 là chống trôi context bằng Dynamic Context Injection

  * khi chat lâu dẫn đến hiện tượng trôi context hoặc lost in the middle. ví dụ ở tin nhắn 1, khách nói đi 3 người, nhưng đến tin nhắn 20 khách yêu cầu book tour thì AI quên và hỏi lại bao nhiêu người
  * cách giải quyết là lưu những thông tin quan trọng như là sdt, số người vào Agent State Store. Mỗi lượt chat thì backend sẽ lấy những thông tin này build thành thanh trạng thái tên là Agent Status Bar và nhúng vào cuối message, giúp AI luôn nắm chắc những thông tin quan trọng
* Tầng 4 là hàng rào kiểm soát đầu ra (output guardrail)

  * đây là chốt chặn cuối bằng code backend trước khi gửi tin nhắn cho khách. Để xử lý vấn đề role hallucination. vì trong prompt hướng dẫn là "nếu khách muốn gặp nhân viên thì hãy tạo ticket có dạng ESC-...". thay vì trả về tool call để backend tạo ticket thì AI nhập luôn vai và tự sinh ra Id giống mẫu trong prompt
  * Cách giải quyết là backend sẽ dùng regex expression quét nội dung tin nhắn trước khi trả về cho khách, nếu phát hiện Id mà ko thấy AI yêu cầu tool call escalate to human thì backend sẽ chặn lại, kích hoạt tool và tạo ticket thật, lấy id thật đè vào câu trả lời trước khi trả về cho khách
* Ngoài ra e còn được AI gợi ý thêm về 2 cách, nhưng e chưa apply

  * thứ nhất là kiểm tra message so với tool call
    * Sau khi AI sinh ra câu trả lời, trước khi trả về cho khách thì backend nên đối chiếu các con số trong tin nhắn với tool result. với giá thì nên render bằng template thay vì để AI tự viết số
  * thứ 2 là đo lường
    * cần có cách để đo lường xem độ hallucination và độ chính xác của tool đang cao hay thấp. từ đó điều chỉnh để tốt hơn

## Em định nghĩa harness engineering như thế nào?

bản chất của harness engineering là thiết kế lớp code bao quanh LLM để chuyển năng lực xác suất của nó thành kết quả đáng tin. harnesss có thể xem như bộ dây cương, không phải để kìm con ngựa mà là để dẫn dắt sức mạnh của nó đi đúng hướng

Tiếp theo là tại sao cần harness. LLM có 3 vấn đề cần harness giải quyết:

* thứ nhất là vì kết quả mang tính xác suất nên dễ bịa số liệu, hành động
* thứ 2 là ko biết gì về hệ thống của mình nên ko trả lời đc đúng thông tin
* thứ 3 là ko có khái niệm về quyền ko biết ai đang nói và được phép gì. nên lệnh của mình, câu của khách đều ngang nhau, nên khách có thể ra lệnh cho nó bằng cách injection. ranh giới thật phải nằm ở code

harness gồm ba checkpoint quanh model: **Constrain** quyết định model thấy gì, được dùng tool gì. **Verify** sẽ đối chiếu output có khớp tool result ko. **Correct** sẽ retry có giới hạn, trả template hoặc escalate đến người

có câu nói cũng hay: Harness đổi tốc độ lấy độ chính xác, và nếu siết sai chỗ thì đổi luôn cả khách hàng.

## Em biết ưu điểm và nhược điểm của harness ko?

ưu điểm của harness:

* thứ nhất là tách rủi ro khỏi năng lực: model là phần thay được, harness là phần giữ lại
* thứ 2 là có thể kết hợp với model yếu: vì harness đã chặn lỗi lan sang hành động như là dùng tool sửa giá nên model sai cũng chỉ sai lời nói
* thứ 3 test được và đo lường được: harness là code nên sẽ test được 3 tầng là unit test ở từng checkpoint, chạy eval set, đo regression
* * tầng unit test: cho 3 lớp constrain, verify, correct. tầng này chỉ có code, ko có model. ví dụ kiểm tra param ngày 32/1/2026 thì phải fail. kiểm tra output so với tool call, vd output message là 3tr, nhưng tool call từ db trả về 2tr5 thì phải fail.
  * tầng eval set: đây là bộ hội thoại cố định, bộ đề thi có sẵn đáp án cho agent
    * trong bộ này sẽ gồm có các cột: đề bài, đáp án, kết quả mong đợi giống với đáp án. sau khi chạy xong thì đếm có bao nhiêu câu đúng để ra điểm. ví dụ chạy 100 câu, đúng 90 câu thì điểm là 90%.
  * tầng đo regression: sẽ so sánh kết quả eval set giữa hôm nay và hôm qua
    * xem đổi model có làm điểm giảm ko. có số điểm thì sẽ so sánh được chứ không phải là "cảm giác"

nhược điểm là:

* latency và chi phí tăng: mỗi retry thường sẽ là 1 lần gọi model dẫn đến tăng thời gian khách chờ, tăng chi phí token
  * ví dụ: khách hỏi tour Nhật bao nhiêu, thì tool call trả về 20tr, nhưng AI response message là 2tr. đến bước verify khi so sánh thấy lệch số nên sẽ retry. việc retry này thường sẽ gửi message báo cho model biết là "số sai rồi, tool trả 20tr" thì model sẽ đưa lại câu trả lời mới. nhưng nếu sai nữa thì phải retry tiếp. việc này dẫn đến tăng thời gian khách phải chờ và chi phí token
* harness quá chặt làm agent vô dụng: siết quá khiến agent trả về template liên tục, khách như nói chuyện với tổng đài tự động
  * ví dụ hỏi tour Nhật có phù hợp với người lớn tuổi ko? nếu harness quá chặt, quy định câu hỏi phải bám tool result, nhưng tool không trả về là "phù hợp với người lớn tuổi", dẫn đến fail và trả template "nhân viên sẽ liên hệ với bạn". khách sẽ bỏ đi. thật ra câu này vô hại, model có thể trả lời theo mô tả tour là "đi bộ ít, có xe hỗ trợ" là đủ
  * verify chặt chỉ nên áp dụng khi có hậu quả cao liên quan đến tiền, booking. nếu sai ở những chỗ vô hại thì vân chấp nhận được
* có câu nói cũng hay: Harness đổi tốc độ lấy độ chính xác, và nếu siết sai chỗ thì đổi luôn cả khách hàng.

Thông tin thêm:

* 3 từ ko phải là ba khái niệm rời - đây là 3 thời điểm quanh 1 lần model chạy. Nhớ trục thời gian sẽ dễ nhớ hơn
  * Trước > Sau > Khi sai = Constrain > Verify > Correct
* Cách nhớ nhanh:
  * "Constrain" = "nó làm được gì?". "Verify" = "nó làm đúng chưa?". "Correct" = "sai thì sao"
* Unit test trả lời "Verify có bắt được số sai không?". Eval set trả lời "Trên 100 tình huống thật, agent sai mấy lần?"
* khác biệt giữa Unit test và eval set
  * Unit test trả lời "Verify có bắt được số sai không?". Eval set trả lời "Trên 100 tình huống thật, agent sai mấy lần?"

|               | Unit test                               | Eval set                                 |
| ------------- | --------------------------------------- | ---------------------------------------- |
| Test cái gì | Một hàm code (ví dụ`VerifyPrice`) | Toàn hệ thống, có gọi model thật   |
| Kết quả     | Pass/fail tuyệt đối                  | Điểm phần trăm, vì model xác suất |
| Mục đích   | Code harness đúng chưa               | Agent *tổng thể* đúng bao nhiêu   |

* bộ eval set lấy từ đâu?

  * theo thứ tự tốt dần: tự nghĩ ra các tình huống > lấy từ hội thoại thật của khách > ưu tiên mỗi lần agent sai thật ở production thì thêm case đó vào
  * eval set lớn lên từ chính lỗi đã gặp, giống như mình thêm regression test sau mỗi bug
* tại sao cần eval set?

  * có số liệu thay vì cảm giác: không có "hình như ổn" mà là 90%
  * phát hiện sự thụt lùi: đổi model chạy eval set thấy giảm xuống 80%
  * biết có giao tiếp được với khách không? tỷ lệ đúng 99%, ticket giả 0/100 là sử dụng được hoặc đem bán được



# Em biết gì về MCP?

MCP là 1 giao thức chuẩn để Agent dùng công cụ và dữ liệu bên ngoài. Có thể tưởng tượng MCP như web api kèm swagger để cho LLM đọc chứ ko phải dev đọc. Mình có thể tạo 1 MCP server để các app AI khác truy cập vào lấy dữ liệu. vd: mcp để tra cứu tour, xem giá

MCP giải quyết các vấn đề sau

mục đích chính của MCP là:

* 
* thứ nhất là tái sử dụng: viết tool 1 lần, mọi client hỗ trợ MCP đều có thể dùng được
* AI sẽ có dư liệu thât và mới nhất
* 



2. Tại sao dùng? (3 lý do)
   Giải bài toán tích hợp N×M: N ứng dụng AI nhân M hệ thống sẽ thành N+M. Viết một MCP server cho hệ thống nội bộ (ERP, CRM, DB) thì mọi client tương thích MCP đều dùng được.
   Tách AI app khỏi integration: Đội backend sở hữu server (domain logic, auth), đội AI sở hữu agent. Hai bên deploy và version độc lập, giống tư duy microservice.
   Hệ sinh thái và tính trung lập vendor: Các vendor lớn (Anthropic, OpenAI, Microsoft, Google) đều hỗ trợ, nên đổi LLM provider không phải viết lại phần integration.
3. Mục đích (3 ý)
   Cung cấp context đúng và cập nhật cho LLM, thay vì nhồi tất cả vào prompt.
   Cho phép LLM thực hiện hành động trên hệ thống thật một cách có kiểm soát.
   Chuẩn hóa để tool tái sử dụng được giữa nhiều agent hoặc nhiều sản phẩm.
4. Ưu điểm (3 ý)
   Tái sử dụng và tương thích: Viết một lần, chạy được trên nhiều host.
   Dynamic discovery: Client gọi tools/list lúc runtime. Server thêm tool thì client không cần redeploy.
   Ranh giới bảo mật rõ ràng: Credential nằm ở server, không lộ cho model. Spec cho remote server có OAuth 2.1.


## Những concept quan trọng và ưu tiên nên biết?

mcp, context engineering, prompt engineering

## Em làm cách nào để kiểm soát việc tool calling mà bị lặp vô tận? e hãy cho 2 vấn đề dẫn đến việc vòng lặp vô tận?

## Khi nào agent quyết định dừng lại và chuyển chat cho nhân viên?

AI chỉ nên làm nhiệm vụ lấy thông tin từ tool và cung cấp thông tin về sản phẩm, dịch vụ cho user. lấy thông tin và yêu cầu của user. Khi chạm đến ngưỡng chốt sale, khiếu nại để bảo đảm tỷ lệ chuyển đổi cao và trải nghiệm của khách hàng

Agent sẽ gọi tool escalate trong các trường hợp sau, những trường hợp này sẽ tùy vào business để xác định. thứ nhất là khi khách hàng cung cấp sdt. thứ 2 là yêu cầu cọc, giữ chỗ. thứ 3 hỏi về việc khuyến mãi, giảm giá. thứ 4 là có dấu hiệu không hài lòng

Khi nhân viên mở màn hình quản lý lên thì mọi thông tin về khách hàng đã có đầy đủ từ tên, sdt, tour nào, số người, ngày khởi hành. Nhân viên không cần tốn thời gian đọc lại toàn bộ lịch sử chat để nắm thông tin

## E có dùng library nào của .NET để làm việc với LLM ko?

## Em có biết tool gồm có những thành phần gì ko? cách hoạt động của tool?

name, description, parameter schema, execute method, ví dụ tool sẽ làm

sẽ implement IAgentTool, đăng ký trong DI

## Tại sao phải inject toàn bộ history?

LLM là **stateless** — mỗi request là independent. Nó không "nhớ" conversation trước. Phải truyền toàn bộ context vào mỗi request để nó "nhớ" được. Đây là **context window** concept.

## Em có thể mô tả flow tool calling giữa backend và LLM ko?

```
INPUT: User message
  ↓
LLM nhận message + danh sách Tool definitions
  ↓
LLM quyết định: Cần gọi Tool nào? (tool_calls trong response)
  ↓
Code thực thi Tool → query DB → trả về kết quả
  ↓
Inject kết quả Tool vào conversation history
  ↓
LLM nhận lịch sử + kết quả Tool → tạo final response
  ↓
OUTPUT: Response có dữ liệu chính xác từ DB
```

---

## 6️⃣ Omnichannel Queue Pattern

### Vấn đề: Telegram Webhook Timeout

```
❌ Xử lý đồng bộ — TIMEOUT:
Telegram → POST /webhook → Handler gọi LLM (5-15s) → Telegram timeout 60s
→ Telegram retry → loop vô tận

✅ Async Queue Pattern — ZERO LATENCY:
Telegram → POST /webhook → Push vào Channel → 200 OK ngay (< 50ms)
                                  ↓
                        BackgroundService dequeue
                                  ↓
                         Gọi LLM Agent Loop
                                  ↓
                         TelegramAdapter gửi response
```

### System.Threading.Channels

```csharp
// Application/Channels/MessageChannel.cs
public class IncomingMessageChannel
{
    // Bounded channel: nếu queue đầy → back-pressure (caller phải chờ)
    private readonly Channel<IncomingMessage> _channel =
        Channel.CreateBounded<IncomingMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public ChannelWriter<IncomingMessage> Writer => _channel.Writer;
    public ChannelReader<IncomingMessage> Reader => _channel.Reader;
}
```

**Q: Channels vs RabbitMQ/Azure Service Bus — trade-off?**

|            | System.Threading.Channels | Message Queue      |
| ---------- | ------------------------- | ------------------ |
| Latency    | Cực thấp (in-memory)    | Network overhead   |
| Durability | Mất khi app restart      | Persist trên disk |
| Scale      | Single instance           | Multi-instance     |
| Setup      | Zero infra                | Cần broker server |
| Dùng khi  | SME, single server        | Microservice, HA   |

---


<!-- Thêm kiến thức/câu hỏi mới vào đây -->

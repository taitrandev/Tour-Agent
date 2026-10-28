# 🎙️ Kịch Bản Trình Bày 3 Phút (Pitching Dự Án & Clean Architecture)

E đang xây dự án Agent tư vấn tour du lịch, viết trên nền **.NET 10, Clean Architecture và CQRS**
Dự án được thiết kế với tiêu chí tinh gọn, tường minh và dễ bảo trì. Mục tiêu lớn nhất là bảo vệ business ổn định ra khỏi phần công nghệ, đặc biệt là AI đang thay đổi liên tục
Hệ thống được chia làm 4 projects tương ứng với 4 layers theo đúng nguyên tắc Dependency Inversion:

1. **Domain**: chỉ chứa Entities, Domain Rules. là file c# thuần, ko có third party. entity có áp dụng Rich Domain Model để tự đảm bảo tính đúng đắn khi tạo và cập nhật dữ liệu
2. **Application:** là nơi định nghĩa các business logic và interface. trong đây e tổ chức theo vertical feature folder, tức là e sẽ gom command, query, handler, vaidator của 1 chức năng vào 1 folder để tránh bị context switching
3. **Infrastructure**: là nơi thiết lập kỹ thuật cho dự án như là EF Core, database, setup channel như telegram, và AI
4. **Api**: sẽ tiếp nhận request, validate, gọi handler, trong đây hoàn toàn ko chứa business logic

Trong lần này, có 2 lựa chọn khác với những lần trước e build dự án là:

1. **Không dùng MediatR**:

   - Thay vì dùng MediatR theo các tutorial thì e áp dụng CQRS kết hợp tự viết handler. điều này giúp code minh bạch, dễ trace debug, và cũng loại bỏ rủi ro về chi phí bản quyền
2. **Không dùng AutoMapper**:

   - Toàn bộ mapping giữa Entity và DTO dùng **Extension Methods**. có thể kiểm soát 100% compile-time, ko có reflection, performance cao nhất và tránh rủi ro về license.

**Đồng thời để giải quyết vấn đề xói mòn kiến trúc**: Đôi khi dev sẽ đi đường tắt, tiện tay using class của tầng khác cho nhanh, ví dụ Controller using code của Infra, hay Domain using EF Core, việc review bằng mắt dễ bỏ sót. Nên em dựng 1 unit test dùng thư viện NetArchTest như là hàng rào bảo vệ tự động thay vì dùng kỷ luật để chặn các vi phạm. Mọi vi phạm về ranh giới tầng sẽ bị phát hiện khi chạy unit test, giúp bảo vệ đc kiến trúc

* Thông tin thêm:
  * CQRS là 1 architectural pattern
  * MediatR
    * Mục đích chính của MediatR là giảm sự phụ thuộc giữa caller và handler
    * ưu điểm dùng mediatR là giảm sự phụ thuộc, xử lý cross cutting mạnh và tiện
    * nhược điểm là khả năng trace debug bị kém, rủi ro về license
    * nên dùng khi hệ thống lớn, có quá nhiều use case và nhiều cross cutting phức tạp. lúc đó chi phí dùng thư viện sẽ rẻ hơn phí tự viết và bào trì pipeline riêng
    * ko nên dùng khi dự án vừa và nhỏ, ưu tiên tính tường minh, dễ trace code, loại bỏ rủi ro license
    * quan điểm mediatR là công cụ tốt, nhưng ko phải điều kiện bắt buộc đi chung với CQRS. mỗi khi chọn kiến trúc đều có trade-off cần đánh đổi.

---

# 💡 Các Câu Hỏi Phản Biện Interviewer Có Thể "Bắt Bẻ" & Cách Trả Lời

## 1. khi ko chọn MediatR mà tự viết thì cần có sự đánh đổi lớn gì?

sự đánh đổi lớn nhất khi ko dùng MediatR là chi phí tự phát triển và gánh nặng kiểm tra độ ổn định bị dồn lên vai của team

đầu tiên là cần nhận thức được có những core feature gì mà MediatR có mà mình cũng cần có, như là:

* request / response: 1 request gửi đi thì có 1 handler xử lý
* publish/subcribe: 1 event bắn ra thì có nhiều handler xử lý
* pipeline behavior: giống như 1 middleware, cho phép chèn code chạy trước và sau handler như là validation, logging, mở/đóng db transaction

thứ hai, sau khi tạo xong những core feature thì cần có tư duy verification, code chạy được nhưng phải luôn kiểm tra để tăng tính ổn định, tính chính xác cho Production. Trong thời AI thì việc kiểm tra này sẽ không khó như lúc trước nữa, chủ yếu cần có đủ nhận thức về những vấn đề sẽ xảy ra

Có thể chia thành 3 nhóm chính và kiểm tra theo thứ tự ưu tiên

* nhóm 1: là kiểm tra độ chính xác và an toàn - cần test exception, edge case, concurrency, race condition
* nhóm 2: là kiểm tra tính toàn vẹn, app ko bị xảy ra vấn đề mới khi dùng handler tự viết - cần regression test
* nhóm 3: là kiểm tra sức chịu tải - cần stress test, benchmark, shadow testing trên môi trường uat/staging

Thông tin thêm:

* stress testing / concurrency testing (kiểm tra rò rỉ bộ nhớ và đa luồng)
  * khi 1 thư viện ra mắt thì đã đc test với hàng triệu request đồng thời, lỗi thường sẽ xảy ra khi chạy với số lượng request lớn
  * cách kiểm chứng là viết unit test giả lập nhiều task chạy song song có bị exception gì ko. ví dụ như xung đột DbContext
* exception handling
  * trường hợp 3 handler cùng lắng nghe 1 sự kiện, handler 1 thành công, nhưng 2 handler khác bị thất bại và quăng lỗi thì hệ thống sẽ xử lý như thế nào
  * cách kiểm chứng là phải suy nghĩ nhiều kịch bản exception để chứng minh code xử lý an toàn
* benchmark testing
  * kiểm tra hiệu năng thực tế bằng BenchmarkDotNet để so sánh thời gian và lượng RAM được cấp phát để chứng minh code tự chế chạy nhanh hơn và tốn ít RAM hơn

## 4. Dependency Inversion Principle (DIP) là gì? Em áp dụng nó như thế nào trong dự án?

- **Bản chất**: Dependency Inversion là kỹ thuật giúp giảm sự phụ thuộc của module cấp cao vào module cấp thấp, cả 2 sẽ phụ thuộc vào interface.
- **Tiêu chí phân biệt cấp cao và cấp thấp**:

  - **Module cấp cao**: sẽ gắn liền với business logic, độc lập với công nghệ, có tính ổn định và ít thay đổi. Trong Clean Architecture, đó là **Domain** và **Application**.
  - **Module cấp thấp**: sẽ gắn liền với công nghệ như là (Database, Third-party), thường xuyên thay đổi. Trong Clean Architecture, đó là **API** và **Infrastructure**.
- **Quy tắc cần nhớ là**: Module cấp cao cần gì thì chỉ cần định nghĩa interface (hợp đồng). Còn module cấp thấp đóng vai trò là "người phục vụ" sẽ implement interface đó (đến và ký hợp đồng đó).
- **Ví dụ trong dự án có tính năng chat AI:**

  - Nếu không nhận thức về DIP: thì ở tầng Infra sẽ tạo class DeepseekClient để gọi API deepseek. Code trong App sẽ dùng trực tiếp class `DeepSeekClient` trong Infra. Luồng reference cũng từ App tới Infra. Hai luồng này đi cùng chiều dẫn đến tầng Business bị phụ thuộc hoàn toàn vào DeepSeek. Sau này nếu muốn đổi từ DeepSeek sang OpenAI thì phải mở tầng App ra sửa code, khả năng vỡ bussiness cao. Muốn viết Unit Test để chạy được thì cần internet, vì bị dính chặt vào `DeepSeekClient`.
  - Nếu áp dụng DIP: thì tầng App hoàn toàn không cần biết dùng DeepSeek hay OpenAI. App sẽ định nghĩa interface `ILlmClient`. Tầng Infra đóng vai trò là người phục vụ, phải reference đến App để implement interface này. Lúc này luồng code (Flow of Control) vẫn từ App sang Infra để request API. Nhưng luồng reference (Source Code Dependency) đã bị bẻ ngược, Infra bắt buộc phải reference App để implement interface. Lợi ích là sau này khi muốn đổi sang OpenAI thì chỉ cần thêm 1 class mới trong Infra, sau đó đổi cấu hình DI trong `Program.cs`. Tầng App không cần thay đổi bất kỳ dòng code nào.

## 5. Tại sao em chọn Clean Architecture cho dự án này?

Vì trước khi nghỉ thì dự án cũ của e dùng, công ty cũng quan tâm về mô hình này, e cũng từng tự setup vài lần rồi nhưng thật sự chưa hiểu bản chất sâu xa của nó, e chỉ suy nghĩ hướng kỹ thuật và bắt chước template có sẵn. Nhưng khi đưa thêm góc nhìn business vào thì giống như mở được thêm 1 góc nhìn mới và thấy mô hình hay hơn. E cũng tận dụng đòn bẩy AI để đào sâu thêm

E nhận ra Clean Architecture không đơn thuần là việc chia project ra 4 layers, mà là cơ chế bảo vệ business ổn định ra khỏi phần công nghệ thông qua nguyên tắc dependency inversion. Mũi tên phụ thuộc chỉ được phép đi 1 chiều từ ngoài vào trong, lớp trong không được biết lớp ngoài

**Những lý do áp dụng là**:

- **Tách phần ổn định ra khỏi phần biến động**: Domain, Application là phần cốt lõi tạo nên giá trị doanh nghiệp nên cần phải ổn định. Còn phần kỹ thuật như AI, DB, EF Core là phần biến động liên tục. Cần cô lập kỹ thuật ở tầng Infra giúp ta đổi công nghệ mà không làm vỡ business
- **Cho phép trì hoãn quyết định kỹ thuật**: Tập trung hoàn thiện nghiệp vụ trước, còn DB hay AI cụ thể sẽ quyết định và cắm vào sau.
- **Dễ dàng unit test**: Vì chỉ cần mock interface là test được logic, chạy trong vài mili-giây

**Còn trade-off cần nhận thức là**:

- Chi phí setup ban đầu sẽ tốn công hơn, nhiều project hơn, tính năng đơn giản cũng phải đi qua 4 layer dễ dẫn đến over engineering.

**Đồng thời để giải quyết vấn đề xói mòn kiến trúc**: Đôi khi dev sẽ đi đường tắt, tiện tay using class của tầng khác cho nhanh, ví dụ Controller using code Infra, hay Domain using EF Core, việc review bằng mắt thường dễ bỏ sót. Nên em dựng 1 unit test với thư viện NetArchTest như là hàng rào tự động để chặn các using vi phạm

## 6.  Tại sao Clean Architecture thấy hay khi đưa trục thời gian vào?

E thấy điểm hay và giá trị thật sự của Clean Architecture sẽ thấy rõ khi đưa trục thời gian vào. sau 3-5 năm, khi công nghệ bị lỗi thời hoặc doanh nghiệp muốn chuyển đổi hạ tầng. có 2 ví dụ giúp mình thấy rõ là:

- ví dụ thứ nhất là dự án có lượng business cực kỳ nhiều, đang dùng .net framework mà muốn nâng cấp lên .net core

  - vấn đề là nếu ko có clean architecture thì code business bị dính chặt vào các thư viện cũ của .net framework, như là khởi tạo cứng DBContext, ko có DI. khi chuyển sang .net core thì gần như phải đập đi xây lại toàn bộ từ đầu, rủi ro vỡ business cực kỳ cao
  - còn nếu có clean architecture, thì tầng domain và app chỉ là những file c# thuần, ít bị ràng buộc vào third party. khi nâng cấp thì app ko cần update nhiều, re-target framework sang .net core., còn lại chủ yếu viết lại tầng API và Infra
- ví dụ thứ 2 là di chuyển db sql server từ on-premise lên cloud azure với postgresql

  - tầng domain và app hầu như giữ nguyên vì chỉ phụ thuộc ORM abstraction, ko reference đến sql server hay postgresql, vì db engine thực tế được setup ở tầng Infra
  - về mặt kỹ thuật thì cần generate migration, xử lý khác dữ liệu, kiểm tra lại raw sql

Tóm lại là Clean Architecture biến các framework, db, third-party thành chi tiết kỹ thuật cắm rút được. doanh nghiệp có thể đổi hạ tầng mà business kinh doanh tích lũy 5 năm vẫn an toàn tuyệt đối

## 7. Tại sao nên đưa Domain Rule vào Entity (Rich domain model)?

Mục đích chính của Rich Domain Model là ngăn chặn việc phân tán logic và hỏng dữ liệu. Thử tưởng tượng có logic kiểm tra Giá > 0. trong Handler A tạo tour thì nhớ check. Handler B có 1 dev mới vào làm feature import từ excel quên check. Handler C làm feature sync từ bên thứ 3 cũng ko check. Hậu quả là trong table có những record sai, giá = 0, hoặc thậm chí là âm

- Giải quyết vấn đề này bằng cách đưa logic vào Entity thông qua Factory Method như là Create, Update, bất kỳ ai muốn tạo entity thì đều bắt buộc đi qua cái cổng này, không thể nào lách luật (vì constructor đã set là private nên không thể khởi tạo instance như bình thường được)
- Ưu điểm lớn khác là dễ unit test
- còn nhược điểm là ko thể thực hiện các check cần truy vấn db, vì Domain không phụ thuộc Infra, nên sẽ phải check ở tầng Application

## 8. Làm sao phân biệt khi nào logic nên để trong Domain hay Application

để xác định được vị trí đặt logic thì có thể dùng 2 câu hỏi sau. câu đầu tiên là logic này có cần truy xuất db ko? nếu có thì đặt trong app, nếu không thì hỏi câu thứ 2. logic này xoay quanh 1 entity hay nhiều entity. Nếu 1 thì đặt trong entity, nếu nhiều thì đặt trong app

ví dụ: cần db thì check trong app check như là email user tồn tại. 1 entity thì check trong entity như là giá tour > 0. nhiều entity thì check trong app như là mã voucher có áp dụng cho tour được không?

## 9. Tại sao cần private constructor trong entity?

Đối với dev thì mục đích là ẩn public constructor, ngăn dùng new instance, khi muốn tạo entity thì đều cần phải gọi hàm Create, Update để kiểm tra logic. Đối với EF Core thì cần 1 constructor ko tham số để tạo instance và truyền data vào

## 10. Backing field là gì? tại sao cần?

backing field là 1 private field nằm phía sau public readonly property và đóng vai trò là vùng nhớ. đối với public property sẽ chỉ có thể đọc, chứ không được phép gán dữ liệu, muốn gán dữ liệu thì cần thông qua method trong entity, method này sẽ gán dữ liệu cho backing field

## 11. CancellationToken là gì? Tại sao cần dùng (WHY)?

Là cơ chế giúp hủy tác vụ đang chạy trong .NET, giống như một "Lá cờ". Các tác vụ thấy cờ bật lên sẽ chủ động dừng lại và ném exception

- Đối với server thì sẽ tiết kiệm tài nguyên, đối với AI thì sẽ tiết kiệm token
- Hệ thống sẽ dừng theo nguyên tắc khi code chạm các hàm async await và được truyền cancellation token. còn code sync hoặc async await không truyền cancellation token sẽ không tự dừng. dev sẽ phải tự kiểm tra cờ và dừng.
- đối với database driver thì có cơ chế dừng riêng, db driver sẽ lập tức gửi CancelRequest xuống db để dừng lập tức query đang chạy

Nguyên lý cốt lõi ngắt là:

* khi user F5 web hoặc bấm Cancel postman thì sẽ gửi gửi 1 gói tin TCP segment có gắn cờ FIN hoặc RST để phát tín hiệu đóng TCP socket. Kestrel nhận được cờ này sẽ biết là socket đã bị ngắt, Kestrel sẽ update CancellationToken thành true
* thêm thông tin chi tiết:

  * đối với ReactJs gõ tìm kiếm, muốn ngắt thì phải dùng AbortController để chủ động ngắt TCP socket
  * đôi với postman nếu send 2 request thì xem như 2 lần độc lập, không ngắt socket. muốn ngắt thì phải bấm Cancel

## 14. phân biệt tcp socket và websocket

TCP socket là khái niệm gốc của hệ điều hành. bất cứ kết nối mạng nào trên đời từ HTTP, database connection, SSH thì bên dưới đều là TCP Socket. Hầu hết các giao thức ứng dụng phổ biến chạy trên TCP socket. Còn Websocket là 1 giao thức cấp application (application protocol) chạy trên nền TCP socket, dùng để làm chat real time 2 chiều

TCP socket đầy đủ sẽ bao gồm IP Address + Port. còn TCP Connection sẽ cần 1 cặp TCP socket để có được nguồn và đích, có thể xem như 1 đường ống dẫn nước

## 12. Thuật ngữ Native là gì? Tại sao nên dùng Native hơn third-party khi có thể?

**Thuật ngữ Native**: Là những tính năng do chính Microsoft / .NET cung cấp sẵn trong bộ thư viện chuẩn mà không cần cài thêm bất kỳ package của bên thứ ba nào.

**Tại sao nên dùng Native**:

- **Zero Dependency**: Không sợ xung đột version package khi nâng cấp các bản .NET mới.
- **No License Risk**: Không lo một ngày đẹp trời tác giả đổi license thu phí (như AutoMapper) hoặc bỏ rơi dự án.
- **Hiệu năng cao nhất (Cụ thể)**:
  - *Zero Reflection*: Gọi hàm trực tiếp, không phải quét qua reflection runtime như MediatR.

## 13. Tại sao dùng vertical feature folder (chia theo tính năng) mà ko dùng horizontal layer (chia theo tầng kỹ thuật)

- dự án chia theo kỹ thuật sẽ có kiểu là 1 folder command sẽ chứa tất cả file command, folder query sẽ chứa tất cả file query

  - việc này dẫn đến vấn đề khi muốn sửa 1 tính năng như là SearchTour thì phải làm những việc như: mở folder query tìm file. mở folder handler tìm file. mở folder dto tìm file. mở folder validator tìm file. mắt mình sẽ phải nhảy qua nhảy lại 4-5 folders khác nhau, hiện tượng context switching sẽ khiến mình rất mệt
- còn nếu dự án chia theo tính năng, thì toàn bộ 1 tính năng sẽ gom vào 1 folder duy nhất.

  - ưu điểm là có tính gắn kết cao, cần sửa 1 tính năng thì mở đúng folder của tính năng đó. dễ xóa hoặc dọn dẹp tính năng cũ. làm việc nhóm ko bị xung đột, 2 người làm trên 2 folder khác nhau

  TourAgent.Application/ (chia theo kỹ thuật)
  ├── Commands/                    <--- Chứa hàng trăm Command của toàn bộ dự án
  │   ├── CreateTourCommand.cs
  ├── Queries/                     <--- Chứa hàng trăm Query của toàn bộ dự án
  │   ├── SearchToursQuery.cs
  ├── Handlers/                    <--- Chứa hàng trăm Handler lẫn lộn
  │   ├── SearchToursHandler.cs
  ├── DTOs/                        <--- Chứa hàng trăm DTO
  │   └── OrderDto.cs

  TourAgent.Application/ (chia theo tính năng)
  └── Features/
  ├── Tours/
  │   ├── SearchTours/             <--- TẤT CẢ FILE CỦA TÍNH NĂNG NẰM Ở ĐÂY!
  │   │   ├── SearchToursQuery.cs
  │   │   ├── SearchToursHandler.cs
  │   │   ├── SearchToursResponse.cs
  │   │   └── SearchToursValidator.cs



## 14. phân biệt các cấp độ

| Cấp độ                         | Là gì                                                       | Phạm vi                 | Ví dụ                                 |
| --------------------------------- | ------------------------------------------------------------- | ------------------------ | --------------------------------------- |
| **Principle**(nguyên tắc) | Quy tắc tư duy                                              | Method/class             | CQS, SRP                                |
| **Design pattern**          | Lời giải lặp lại cho cách các class/object tương tác | Vài class               | **Mediator**, Decorator, Strategy |
| **Architectural pattern**   | Cách tổ chức cả ứng dụng hoặc hệ thống               | Module, tầng, DB        | **CQRS**, Layered, Microservices  |
| **Library**(thư viện)     | Code có sẵn hiện thực một pattern                        | Dependency trong project | **MediatR**, Wolverine            |

## 📝 Ghi Chú Ôn Luyện:

Nội dung này được đồng bộ làm Source of Truth với [01_clean_architecture.md](01_clean_architecture.md).

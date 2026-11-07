# 📐 NHÓM 1: Clean Architecture & Design Principles

> **Source of Truth — Cập nhật liên tục khi có câu hỏi/kiến thức mới**
> Dự án tham chiếu: `dotnet-backend-core-main` (TourAgent — .NET 10)

---

# Kịch Bản Trình Bày 3 Phút (Pitching Dự Án & Clean Architecture) - update 5/11

E đang xây dự án Agent tư vấn tour du lịch trên nền **.NET 10, Clean Architecture và CQRS**
Tiêu chí của dự án là tinh gọn, tường minh và dễ bảo trì. Mục tiêu lớn nhất là bảo vệ business ổn định ra khỏi phần biến động của công nghệ, nhất là với sự thay đổi nhanh chóng của AI thì việc bảo vệ này thật sự quan trọng
Hệ thống được chia làm 4 projects tương ứng với 4 layers theo đúng nguyên tắc Dependency Inversion:

1. **Domain**: chỉ chứa Entities, Domain Rules. là file c# thuần, ko có third party. Trong entity e có dùng Rich Domain Model để tự bảo vệ tính đúng đắn khi tạo và cập nhật dữ liệu
2. **Application:** là nơi định nghĩa các business logic và interface. trong đây e tổ chức theo vertical feature folder, tức là e sẽ gom command, query, handler, validator của 1 chức năng vào 1 folder để tránh bị context switching
3. **Infrastructure**: là nơi thiết lập kỹ thuật cho dự án như là EF Core, database, setup telegram, và AI
4. **Api**: sẽ tiếp nhận request, validate, gọi handler, trong đây hoàn toàn ko chứa business logic

Trong lần này, có 2 lựa chọn khác với những lần trước e build dự án là:

1. **Không dùng MediatR**:

   - Thay vì dùng MediatR theo các tutorial thì e áp dụng CQRS kết hợp tự viết handler. điều này giúp code minh bạch, dễ trace code, và tránh rủi ro về license
2. **Không dùng AutoMapper**:

   - Toàn bộ mapping giữa Entity và DTO dùng **Extension Methods**. Dễ đọc code trong phần business, ko cần phải qua chỗ khác đọc. Lỗi compile time rõ ràng. không có reflection nên sẽ có performance cao nhất. Tránh rủi ro về license

**E cũng được AI cho biết về việc xói mòn kiến trúc:** Vấn đề gặp phải là đôi khi dev sẽ đi đường tắt, tiện tay using class của tầng khác cho nhanh, ví dụ Controller using code của Infra, hay Domain using EF Core, việc review bằng mắt dễ bị thiếu. Nên em giải quyết bằng cách dựng 1 unit test dùng thư viện NetArchTest như là hàng rào bảo vệ tự động thay vì dùng kỷ luật để chặn các vi phạm. Mọi vi phạm về ranh giới tầng sẽ bị phát hiện khi chạy unit test, giúp bảo vệ đc kiến trúc

## 1.1 Tại sao em chọn Clean Architecture cho dự án này?

Vì trước khi nghỉ thì dự án cũ của e dùng, e thấy công ty cũng quan tâm về mô hình này, và e cũng từng tự setup vài lần rồi nhưng thật sự chưa hiểu bản chất của nó, e chỉ suy nghĩ hướng kỹ thuật và bắt chước tutorial có sẵn. Nhưng đợt này khi e đưa thêm góc nhìn business vào thì giống như mở được thêm 1 góc nhìn mới và thấy mô hình hay hơn. E cũng tận dụng đòn bẩy AI để đào sâu thêm

E nhận ra Clean Architecture không đơn thuần là việc chia project ra 4 layers, mà là cơ chế bảo vệ business ổn định ra khỏi phần công nghệ đầy biến động thông qua nguyên tắc dependency inversion. Mũi tên phụ thuộc chỉ được phép đi 1 chiều từ ngoài vào trong, lớp trong không được biết lớp ngoài

**Những lý do áp dụng là**:

- **Tách phần ổn định ra khỏi phần biến động**: Domain, Application là phần cốt lõi tạo nên giá trị doanh nghiệp nên cần phải ổn định. Còn phần kỹ thuật như AI, DB, EF Core là phần biến động liên tục. Cần cô lập kỹ thuật ở tầng Infra giúp việc đổi công nghệ mà không làm vỡ business
- **Cho phép trì hoãn quyết định kỹ thuật**: Tập trung hoàn thiện nghiệp vụ trước, còn DB hay AI cụ thể sẽ quyết định và cắm vào sau.
- **Dễ dàng unit test**: Vì chỉ cần mock interface là test được logic, chạy cực nhanh

**Còn trade-off cần nhận thức là**:

- Chi phí setup ban đầu sẽ tốn công hơn, nhiều project hơn, tính năng đơn giản cũng phải đi qua 4 layer dễ dẫn đến over engineering.

## 1.2  Tại sao Clean Architecture thấy hay khi đưa trục thời gian vào?

E thấy điểm hay và giá trị thật sự của Clean Architecture là khi đưa trục thời gian vào. sau vài năm, khi công nghệ bị lỗi thời hoặc doanh nghiệp muốn chuyển đổi hạ tầng. có 2 ví dụ giúp mình thấy rõ là:

- ví dụ thứ nhất là dự án có lượng business cực kỳ nhiều, đang dùng .net framework mà muốn nâng cấp lên .net core

  - vấn đề là nếu ko có clean architecture thì code business bị dính chặt vào các thư viện cũ của .net framework, như là khởi tạo cứng DBContext, ko có DI. khi chuyển sang .net core thì gần như phải đập đi xây lại toàn bộ từ đầu, rủi ro vỡ business cực kỳ cao
  - còn nếu có clean architecture, thì tầng domain và app chỉ là những file c# thuần, ít bị ràng buộc vào third party. khi nâng cấp thì app ko cần update nhiều, re-target framework sang .net core., còn lại chủ yếu viết lại tầng API và Infra
- ví dụ thứ 2 là di chuyển db sql server từ on-premise lên cloud azure với postgresql

  - tầng domain và app hầu như giữ nguyên vì chỉ phụ thuộc ORM abstraction, ko reference đến sql server hay postgresql, vì db engine thực tế được setup ở tầng Infra
  - về mặt kỹ thuật thì cần generate migration, xử lý khác dữ liệu, kiểm tra lại raw sql

Tóm lại là Clean Architecture biến các framework, db, third-party thành chi tiết kỹ thuật có thể cắm rút được. doanh nghiệp có thể thay đổi hạ tầng mà business vẫn an toàn

## 1.3 Tại sao dùng feature folder (vertical folder - chia theo tính năng) mà ko dùng layer folder (horizontal layer - chia theo tầng kỹ thuật) - update 5/11

* dự án chia theo layer folder thì sẽ có những folder tên query, command, handler. ví dụ folder query sẽ thấy query của tất cả feature
  * việc này dẫn đến vấn đề khi muốn sửa 1 tính năng như là SearchTour thì phải làm những việc như: mở folder query tìm file. mở folder handler tìm file. mở folder validator tìm file. mắt mình sẽ phải nhảy qua nhiều folders khác nhau, đây là hiện tượng context switching sẽ khiến mình rất mệt

- còn nếu dự án chia theo feature folder, thì toàn bộ file như là query, handler, validator của 1 tính năng sẽ gom vào 1 folder duy nhất. Ưu điểm là

  - Thứ nhất là khi maintain 1 tính năng, thì chỉ cần mở 1 folder, không cần nhảy nhiều folder
  - thứ 2 là làm việc nhóm ít bị xung đột vì 2 người làm trên 2 folder khác nhau
  - thứ 3 là dễ xóa hoặc dọn dẹp tính năng cũ

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

## 1.4 Dependency Inversion Principle (DIP) là gì? Em áp dụng nó như thế nào trong dự án?

- **Bản chất**: Dependency Inversion là kỹ thuật giúp giảm sự phụ thuộc của module cấp cao vào module cấp thấp, cả 2 sẽ phụ thuộc vào interface.
- **Tiêu chí để phân biệt cấp cao và cấp thấp là**:

  - **Đối với cấp cao**: thì sẽ gắn liền với business logic, độc lập với công nghệ, có tính ổn định và ít thay đổi. Trong Clean Architecture, đó là **Domain** và **Application**.
  - **Còn Module cấp thấp**: sẽ gắn liền với công nghệ, thường xuyên thay đổi như là (Database, AI, Third-party). Trong Clean Architecture, đó là **API** và **Infrastructure**.
- **Quy tắc cần nhớ là**: Module cấp cao cần gì thì sẽ định nghĩa interface (hợp đồng). Còn module cấp thấp đóng vai trò là "người phục vụ" sẽ implement interface đó (đến và ký hợp đồng đó).
- **Ví dụ trong dự án có tính năng chat với AI:**

  - Nếu không nhận thức về DIP: thì ở tầng Infra sẽ tạo class DeepseekClient để gọi API deepseek. Code trong App sẽ dùng trực tiếp class `DeepSeekClient` trong Infra. Luồng reference cũng từ App tới Infra. Hai luồng này đi cùng chiều dẫn đến tầng Business bị phụ thuộc hoàn toàn vào DeepSeek. Sau này nếu muốn đổi từ DeepSeek sang OpenAI thì phải mở tầng App ra sửa code, khả năng vỡ bussiness cao. Muốn viết Unit Test để chạy được thì cần internet, vì bị dính chặt vào `DeepSeekClient`.
  - Nếu áp dụng DIP: thì tầng App hoàn toàn không cần biết dùng DeepSeek hay OpenAI. App sẽ định nghĩa interface `ILlmClient`. Tầng Infra đóng vai trò là người phục vụ, phải reference đến App để implement interface này. Lúc này luồng code (Flow of Control) vẫn từ App sang Infra để request API. Nhưng luồng reference (Source Code Dependency) đã bị bẻ ngược, Infra bắt buộc phải reference App để implement interface. Lợi ích là sau này khi muốn đổi sang OpenAI thì chỉ cần thêm 1 class mới trong Infra, sau đó đổi cấu hình DI trong `Program.cs`. Tầng App không cần thay đổi bất kỳ dòng code nào.

---

## 2.1 Em biết gì về MediatR? - update 5/11

Bản chất của MediatR là 1 thư viện giúp giảm sự phụ thuộc giữa caller và handler. Controller ko cần biết handler, controller chỉ cần gửi request đến MediatR, còn MediatR sẽ dùng reflection tìm và điều phối request đến đúng handler lúc runtime.

ưu điểm là:

* xử lý Pipeline behavior mạnh và tiện. Nên dùng với hệ thống lớn có nhiều usecase và nhiều cross cutting phức tạp. Lúc đó thì chi phí dùng thư viện sẽ rẻ hơn chi chí tự viết và bảo trì pipeline riêng

nhược điểm là

* thứ nhất khó trace code: khó theo luồng code khi đọc, vì ko đi thẳng từ controller tới handler đc
* thứ 2 là dễ bị dev lạm dụng pub/sub để làm message broker trong bộ nhớ, gây mất dữ liệu khi server có vấn đề
* thứ 3 là ko nên dùng khi dự án vừa và nhỏ, ưu tiên tính tường minh, dễ trace code, loại bỏ rủi ro license
* lỗi sẽ dời về runtime: quên đăng ký handler thì lúc chạy app và request gọi đến thì mới văng exception, compile time ko phát hiện được

quan điểm của e thì mediatR là công cụ tốt, nhưng ko phải điều kiện bắt buộc đi chung với CQRS. mỗi lựa chọn liên quan đến kiến trúc, kỹ thuật đều có trade-off cần đánh đổi.

## 2.2 Tại sao e không dùng MediatR? - update 5/11

Nguyên nhân chính e không dùng là vì nghe nói bắt đầu apply license. Nhưng vì không dùng nên cũng giúp e có thêm góc nhìn và kiến thức mới, bỏ được ngộ nhận là khi dùng CQRS cần phải dùng MediatR. trong giai đoạn system design thì e áp dụng tư duy AI-First để debate về những ưu điểm, nhược điểm, trade-off giữa việc dùng MediatR và tự viết handler

Những ưu điểm khi tự viết handler là:

* thứ nhất là dễ debug & trace code: có thể dùng tính năng go to implementation để từ controller vào handler
* thứ 2 là controller thấy rõ ràng các handler, không bị che giấu, tránh biến controller thành 1 túi mù
* thứ 3 là dự án của e nhỏ, nếu dùng MediatR thì dễ bị over engineering, gây phức tạp hóa

## 2.3 khi ko chọn MediatR mà tự viết thì cần có sự đánh đổi lớn gì ko?

sự đánh đổi lớn nhất khi ko dùng MediatR là chi phí tự phát triển và gánh nặng kiểm tra độ ổn định bị dồn lên vai của team

đầu tiên là cần nhận thức được có những core feature gì mà MediatR có mà mình cũng cần có, như là:

* request / response: 1 request gửi đi thì có 1 handler xử lý
* publish/subcribe: 1 event bắn ra thì có nhiều handler xử lý
* pipeline behavior: giống như 1 middleware, cho phép chèn code chạy trước và sau handler như là validation, logging, mở/đóng db transaction

thứ hai là sau khi tạo xong những core feature thì cần có tư duy verification, code chạy được nhưng phải luôn kiểm tra để tăng tính ổn định, tính chính xác cho Production. Trong thời AI thì việc kiểm tra này sẽ không khó như lúc trước nữa, chủ yếu cần có đủ nhận thức về những vấn đề sẽ xảy ra hay ko

Thì có thể chia thành 3 nhóm chính và kiểm tra theo thứ tự ưu tiên

* nhóm 1: là kiểm tra độ chính xác và an toàn - cần test exception, edge case, concurrency, race condition
* nhóm 2: là kiểm tra tính toàn vẹn, khi thay handler tự viết cho MediatR thì app có xảy ra vấn đề gì ko - cần regression test
* nhóm 3: là kiểm tra sức chịu tải - cần stress test, benchmark, shadow testing trên môi trường staging

Thông tin thêm:

* stress testing / concurrency testing (kiểm tra rò rỉ bộ nhớ và đa luồng)
  * khi 1 thư viện ra mắt thì đã đc test với hàng triệu request đồng thời, lỗi thường sẽ xảy ra khi chạy với số lượng request lớn
  * cách kiểm chứng là viết unit test giả lập nhiều task chạy song song có bị exception gì ko. ví dụ như xung đột DbContext
* exception handling
  * trường hợp 3 handler cùng lắng nghe 1 sự kiện, handler 1 thành công, nhưng 2 handler khác bị thất bại và quăng lỗi thì hệ thống sẽ xử lý như thế nào
  * cách kiểm chứng là phải suy nghĩ nhiều kịch bản exception để chứng minh code xử lý an toàn
* benchmark testing
  * kiểm tra hiệu năng thực tế bằng BenchmarkDotNet để so sánh thời gian và lượng RAM được cấp phát để chứng minh code tự chế chạy nhanh hơn và tốn ít RAM hơn

---

## 3.1 Tại sao nên dùng Rich Domain Model?

Mục đích chính của Rich Domain Model là chặn việc business logic bị phân tán và hỏng dữ liệu

Trong Rich Domain Model có 2 thành phần quan trọng là: property với private setter và Factory Method. 2 thành phần này là hàng rào bảo vệ tính đúng đắn của entity, ko bị rơi vào trạng thái invalid

Thử tưởng tượng có logic kiểm tra Giá > 0. trong Handler A tạo tour thì nhớ check. Handler B có 1 dev mới vào làm feature import từ excel quên check. Handler C làm feature sync từ bên thứ 3 cũng ko check. Hậu quả là trong table có những record sai, giá = 0, hoặc thậm chí là âm

- Ưu điểm
  - Thứ nhất là Rich Domain Model sẽ giải quyết vấn đề này bằng cách đưa logic vào Entity thông qua Factory Method như là Create, Update, bất kỳ ai muốn tạo, cập nhật entity đều bắt buộc đi qua cổng này (vì constructor đã set là private nên không thể khởi tạo instance như bình thường được)
  - thứ 2 là dễ unit test
- còn nhược điểm là ko thể gọi db, vì Domain không phụ thuộc Infra, nên sẽ phải check ở tầng Application

## 3.2 Làm sao phân biệt khi nào logic nên để trong Domain hay Application

Có 2 câu hỏi dùng để xác định vị trí đặt logic. câu đầu tiên là logic này có cần gọi db ko? nếu có thì đặt trong app, nếu không thì hỏi câu thứ 2. logic này xoay quanh 1 entity hay nhiều entity. Nếu 1 thì đặt trong entity, nếu nhiều thì đặt trong app

ví dụ: cần check db thì trong app như là email user tồn tại. Nếu 1 entity thì check trong entity như là giá tour > 0. Nếu nhiều entity thì check trong app như là mã voucher có áp dụng cho tour được không?

## 3.3 Tại sao cần private constructor trong entity?

Đối với dev thì mục đích là ẩn public constructor, ngăn dùng new instance, khi muốn tạo, cập nhật entity thì đều cần phải gọi Factory Method để kiểm tra logic
Còn đối với EF Core thì cần 1 constructor ko tham số để tạo instance và truyền data vào

## 3.4 Backing field là gì? tại sao cần?

backing field là 1 private field nằm phía sau public readonly property và đóng vai trò là vùng nhớ. đối với public property sẽ chỉ có thể đọc, chứ không được phép gán dữ liệu, muốn gán dữ liệu thì cần thông qua factory method trong entity, method này sẽ gán dữ liệu cho backing field

## 3.5 Tại sao dùng Result thay vì Exception?

E tách theo bản chất của lỗi. Những lỗi business đoán được như là not found, validation fail thì e sẽ dùng Result. Còn Exception sẽ chỉ dành cho những tình huống bất thường như mất kết nối db, timeout sẽ bắt ở global exception và trả 500. cả 2 đường đều ra cùng 1 format là ProblemDetails

còn lý do phụ là performance. có 1 benchmark trên .NET cho thấy khi có lỗi thì Result chạy nhanh hơn Exception 40 lần, còn cấp phát bộ nhớ thì ít hơn khoảng 26 lần. Khi không có lỗi thì hai cách gần như ngang nhau. Nên chênh lệch chỉ đáng kể khi lỗi xảy ra thường xuyên, ví dụ validation fail nhiều hoặc bị tấn công bằng cách spam request sai

* trade-off khi dùng Result là
  * Code dài hơn: vì Result phải truyền ngược qua từng tầng như là Domain lên Handler
  * Tầng controller phải check và map status code: phải tạo extension method để map các error, status code theo chuẩn ProblemsDetails
  * Dù dùng Result nhưng vẫn cần phải có middleware để bắt exception và trả 500. Tức là hệ thống phải dùng cả 2
* Thông tin thêm
  * đối với authentication và authorization (401) thì sẽ bắt ở middleware trước khi đến controller, handler. result chỉ nên chứa lỗi bussiness như Validation (400), Not Found (404)

## 3.6 Các sai lầm thường gặp khi throw exception trong .NET

Top 3 lỗi gây hại nhiều nhất là:

* throw ex thay vì throw: throw ex sẽ làm mất stack trace, dấu vết chỉ bắt đầu từ dòng throw ex, những dấu vết trước bị reset
* wrap exception nhưng không truyền innerException: cần luôn truyền innerException
* Nuốt exception: catch rỗng ko làm gì, hoặc catch xong return null

3 lỗi nhỏ hơn

* throw exception chung chung: nên throw cụ thể, có ngữ nghĩa như là ArgumentException, hoặc custom exception
* message thiếu ngữ cảnh hoặc làm lộ dữ liệu nhạy cảm
* log rồi rethrow nhiều tầng: việc này dẫn đến lỗi xuất hiện nhiều lần trong log, trong lúc đọc log dễ bị nhiễu. chỉ log ở những nơi thực sự xử lý lỗi. các tầng giữa nếu cần context thì thêm bằng cách wrap exception, không log lại

## 3.7 CancellationToken là gì? Tại sao cần dùng (WHY)?

Là cơ chế giúp hủy tác vụ đang chạy trong .NET, giống như một "Lá cờ". Các tác vụ thấy cờ bật lên sẽ chủ động dừng lại và ném exception

- Đối với server thì sẽ tiết kiệm tài nguyên, đối với AI thì sẽ tiết kiệm token
- Hệ thống sẽ dừng khi code chạm các hàm async await và được truyền cancellation token. còn code sync hoặc async await không truyền cancellation token sẽ không tự dừng. dev sẽ phải tự kiểm tra cờ và dừng.
- đối với database driver thì có cơ chế dừng riêng, db driver sẽ lập tức gửi CancelRequest xuống db để dừng lập tức query đang chạy

Thông tin thêm

* ví dụ khi user F5 web hoặc bấm Cancel postman thì sẽ gửi gửi 1 gói tin TCP segment có gắn cờ FIN hoặc RST để phát tín hiệu đóng TCP socket. Kestrel nhận được cờ này sẽ biết là socket đã bị ngắt, Kestrel sẽ update CancellationToken thành true
* thêm thông tin chi tiết:
  * đối với ReactJs gõ tìm kiếm, muốn ngắt thì phải dùng AbortController để chủ động ngắt TCP socket
  * đối với postman nếu send 2 request thì xem như 2 lần độc lập, không ngắt socket. muốn ngắt thì phải bấm Cancel

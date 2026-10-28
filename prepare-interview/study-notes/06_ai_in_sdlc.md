## 1. Khi nhận được 1 task mới thì e làm gì? và nếu task mơ hồ thì sao?

với task mới thì e nghĩ đến quy trình sau:

* understand requirement > plan and debate > decide option > break task > set timebox > implement > test > review

đầu tiên sẽ là hiểu rõ requirement, kiểm tra AC. nếu task mơ hồ thì cần xác định những điểm mơ hồ, chuẩn bị sẵn câu hỏi kèm theo giả định để làm việc với BA, TA. Plan và debate với AI để có được action plan, có thể tìm những nguy hiểm tiềm ẩn bằng tư duy inversion thinking > mỗi khi có nhiều lựa chọn liên quan đến kỹ thuật thì mỗi lựa chọn nên biết được ưu điểm, nhược điểm, tại sao dùng, có impact gì ko, có phù hợp với dự án ko > dựa vào những thông tin có được thì mình sẽ là người ra quyết định phương pháp nào, nếu khó quyết định và thấy impact lớn thì đưa TA review thêm > khi mọi thứ đã đầy đủ thì bắt đầu break task > set timebox > implement > testing đã đúng AC chưa > review

## 2. Khi nhận 1 bug ticket thì e làm gì?

với bug thì e nghĩ đến quy trình sau:

* reproduce and isolate > root cause > list out solution > decide solution > break task > set timebox > fixing > test > review

đầu tiên e sẽ reproduce và khoanh vùng bug > xác định root cause thật sự > tìm những giải pháp để fix. mỗi giải pháp nên biết ưu điểm, nhược điểm. tại sao dùng? có impact gì không? có phù hợp với dự án không? > dựa vào những thông tin có được thì e sẽ là người quyết định giải pháp nào > khi mọi thứ đã đầy đủ thì bắt đầu break task > set timebox >  fixing > testing đúng AC chưa > review

e nghĩ là dev nên khắc được tư duy solution bias vào đầu. nếu không nhận thức được thì dev thường sẽ hành động theo cảm tính, lúc nhớ lúc quên. lúc quên sẽ yêu cầu AI fix lỗi, lúc nhớ thì yêu cầu tìm root cause. Việc này dẫn đến việc fix bug có kết quả không được ổn định

sau khi fix xong thì sẽ cập nhật nguyên nhân và cách fix vào lesson learned để team tránh mắc lại lỗi tương tự

## 3. Làm sao để tiết kiệm token?

đối với dự án nhỏ có thể ít tốn, nhưng nếu dự án lớn ko biết biết cách kiểm soát việc đọc của AI thì sẽ tốn rất nhiều, có những kỹ thuật nên biết là:

* thứ nhất là plan và debate kỹ để ra được file action plan trước khi quyết định code. vì trước khi thấy được bức tranh toàn cảnh mà nhảy vào code liền thì sẽ dễ bị sai, mà chi phí code sai sẽ luôn đắt hơn chi phí plan sai
* thứ 2 là Model routing: dùng model cho đúng việc. thường thì những model reasoning mạnh thì e sẽ chỉ dùng để plan và debate, system design, debug những logic khó, audit security. còn model nhanh/rẻ thì e sẽ dùng cho việc code theo action plan
* thứ 3 là không nên giữ 1 chat sesion quá lâu: context càng lớn thì càng tốn tiền lại còn bị loãng, dẫn đến AI dễ bị ảo giác. Nếu task quá lớn thì nên chia scope để implement, khi xong thì qua chat session khác để làm scope tiếp theo. trong lúc làm thì nên giữ context quan trọng như là những quyết định, những gì ko có trong code. Nhờ context này mà giúp AI thấy và hiểu được những gì đã làm trước đó
* thứ 4 là tối ưu output token: bản chất output token mắc gấp nhiều lần so với input token. nên yêu cầu AI chỉ trả về những gì cần update thay vì viết lại toàn bộ
* thứ 5 là dùng git diff khi review code: chỉ review những phần change thay vì review toàn bộ file
* thứ 6 là tạo specialized prompt: đây là việc thiết kế prompt cho từng loại công việc, không cố gắng nhồi nhét nhiều công việc vào cùng 1 prompt. ví dụ khi implement feature xong thì nên có skill hoặc sub agent riêng để review lại code và AC? skill riêng để soi security. skill riêng để tạo unit test

## 4. Thường e sẽ làm gì khi code review?

đây là giai đoạn self review trước khi tạo PR thì e có quy trình sau:

* scan code and create action plan > review plan > fix > regression test > update lesson learned
* bước 1: là cho AI quét và tạo action plan. trong plan nên có 1 danh sách review summary về những gì cần fix để dễ quan sát. AI sẽ quét 3 góc nhìn sau

  * thứ nhất là code quality: như là coding convention, code smell
  * thứ 2 là business logic có đúng với AC chưa?
  * thứ 3 là impact analysis: code này có gây ra side effect cho module khác không? có bị security, performance, critical bug ko?
* bước 2: là Review. e sẽ đọc hiểu plan. phân tích và đánh giá những đề xuất thực sự cần fix
* bước 3: là Fix. để AI fix theo những gì đã duyệt
* bước 4: là regression test. Chạy lại unit test để bảo đảm ko bị regression bug. yêu cầu AI review lại những gì vừa sửa xem có phát sinh lỗi mới ko. e cũng sẽ tự check xem AI có fix đúng với những gì đã duyệt trong action plan chưa, để tránh trường hợp AI fix thêm hoặc thiếu
* bước 5 là: Update lesson learned. với bất cứ quyết định hoặc lỗi có khả năng xảy ra lại thì nên có tư duy đặt câu hỏi sau "lỗi này có thể khóa lại bằng unit test ko?" nếu được thì sẽ ưu tiên viết regression test để đưa vào pipeline CI. bởi vì unit test mang tính chắc chắn, đỏ là chặn PR ngay. AI hoặc dev không thể nào gặp lại lỗi đó. với những thứ unit test không bắt được thì sẽ đúc kết đưa vào lesson learned để làm bộ nhớ dài hạn cho AI ở chat session khác

  * có ví dụ về việc tạo quy tắc thay vì tạo unit test là: liên quan đến AsNoTracking ki query, unit test chỉ có thể test được là lấy được data hay không, test chạy xanh, nhưng vấn đề xảy ra khi chạy production có quá nhiều request đến mà trong code không có AsNoTracking, dẫn đến tốn tài nguyên, giảm tốc độ. cho nên cần tạo quy tắc để AI scan trong query bắt buộc phải có AsNoTracking

## 5. Làm sao để review PR của junior do AI gen 100%?

Đối với phase review PR thì e nghĩ cần phải qua được chốt chặn CI pipeline trước, CI phải xanh thì mới review

Sau đó thì đến quy trình sau

* understand requirement and impact analysis > review test and output > read code > explain code
* Bước 1: thì em cần hiểu yêu cầu và AC để khoanh vùng sự thay đổi. Kiểm tra xem có sửa gì ngoài task không, có impact đến những module khác không
* Bước 2: Sẽ review unit test có bám AC không? Đặc biệt là soi assert có chặt chẽ không? Có bị over-mocking dẫn đến che mất business cần test không? Review output thông qua hình ảnh, video, log được đính kèm trong PR description.
* Bước 3: Là đọc code theo luồng input, process, output. Tập trung vào các đoạn business phức tạp và kỹ thuật mới thay vì soi style của từng dòng.
* Bước 4: Là yêu cầu dev giải thích được những business phức tạp, kỹ thuật mới, cơ chế hoạt động. Mục đích của việc này là trong team phải có người hiểu được những đoạn code đó, không để nợ vibe coding lọt vào production.

Thông tin thêm:

* tư duy gatekeeper: đưa chốt chặn CI pipeline lên đầu, điều này chứng minh bạn quý thời gian của bản thân và tôn trọng quy trình automation
* ở bước 3, nếu người review mà manual test thì sẽ biến thành manual QA, biến thành bottleneck của cả team, junior sẽ bị ỷ lại vì có người review test

## 6. E định nghĩa DONE như thế nào?

1 task đc xem là done khi qua được 3 cổng, có thể tưởng tượng các cổng này sẽ trải dài từ local đến staging

* đầu tiên là cổng dev ở local

  * cần đáp ứng đủ AC bao gồm happy case và edge case
  * self-review lại git diff để bao đảm ko vi phạm coding convention, security, đặc biệt là nợ vibe coding, dev nên hiểu mọi dòng code AI sinh ra, giải thích được lý do kỹ thuật và business phức tạp
  * pass toàn bộ unit test, integration test
* thứ 2 là cổng PR

  * PR cần có description rõ ràng, có ghi giải pháp kèm ưu điểm, nhược điểm, tại sao dùng. output có hình ảnh, video hoặc log
  * chạy CI không có warning. cần pass build, unit test, sonar, secret, security
  * cần được review bởi 1-2 người, giải quyết các comment và approve
* thứ 3 là cổng staging

  * merge code vào develop, deploy lên staging và test thành công
  * update document như là lesson learned, technical doc, api doc

## 7. Dev nhận thức về việc nợ vibe coding và cách trả nợ bằng AI?

việc nợ vibe coding thực chất là nợ kỹ thuật bị phóng đại bởi tốc độ của AI, dẫn đến việc dev ko thực sự hiểu cơ chế bên dưới. hậu quả là khi gặp bug thì dev sẽ gặp khó khăn khi muốn xử lý. cách để trả nợ là:

* mọi dòng code AI sinh ra đều phải đọc hiểu tường tận
* dùng unit test để ép não đọc hiểu bussiness thêm 1 lần nữa: khi tạo unit test thì dev nên đưa danh sách test case dựa vào AC để AI gen unit test. sau đó dev cần soi kỹ phần Assert xem có chặt ko?
* tự tay refactor code, ý nghĩa của "tự tay" ở đây ko phải tự gõ bằng cơm mà là biết mình muốn gì và tại sao

  * dev sẽ là người cầm lái, đưa ra suy nghĩ và ý định. AI sẽ đánh giá ý định. Dev thấy hợp lý sẽ yêu cầu AI thực hiện
  * sau khi refactor xong thì bắt buộc phải chạy lại unit test ngay để bảo đảm code ko thay đổi business logic, ko phát sinh regression bug

## 8. Trong dự án nên tổ chức các file context như thế nào?

context engineering rất quan trọng, vì context quyết định AI nhìn thấy được gì. trong dự án e thường có những file như là:

* project architecture: để nắm overview, tech stack
* coding rule và convention: để chứa những ví dụ code mẫu để AI follow, dùng ví dụ sẽ mạnh hơn là dùng lời nói
* security: để tránh những vấn đề liên quan security
* lesson learned: để chưng cất lại những gì tốt đã làm đã quyết định. thường có thể chia những nhóm như là:

  * core: dùng cho toàn dự án để lưu các quy tắc chung --- domain, feature để chuyên cho module --- review checklist: tổng hợp các lỗi dev trong team hay mắc phải khi tạo PR
* prompt: chứa những prompt template để chuẩn hóa các quy trình hằng ngày như là start chat session mới, implement, fix bug, review, onboarding dev mới
* Thông tin thêm:

  * Bản chất của lesson learned là bộ nhớ dài hạn giúp chống lại bệnh mất trí nhớ của AI. mỗi khi mở 1 chat session mới thì AI sẽ mất trí nhớ hoàn toàn về những sai lầm trong quá khứ. Nếu không có lesson learned thì AI sẽ tiếp tục lặp lại đúng những sai lầm mà lần trước đã mắc phải
  * file lesson learned nên có cấu trúc, đừng viết kiểu văn xuôi:

    > ### [Tên lỗi/Vấn đề]:
    >
    > * **Vấn đề (Symptom)**:
    >
    > - **Root Cause**:
    > - **Quy tắc giải pháp (Rule)**:
    > - **Anti-pattern cần tránh**:
    >

## 9. Bên trên muốn bảo đảm chất lượng nhưng deadline bị ép thì e làm gì?

e sẽ ưu tiên giảm scope, chứ ko giảm quality và test. sau đó công khai với mọi người và khách hàng

việc giảm cần có chiến lược đúng đắn. mọi người cần biết giảm gì? phần còn lại sẽ lùi đến khi nào làm tiếp? có rủi ro gì cần nhận thức không? quyết định sẽ nằm ở khách hoặc PM

thứ thường có thể cắt là edge case, tối ưu performance. cần phải tạo ticket đưa vào backlog ngay, tránh việc hoãn biến thành quên luôn và sinh ra bug sau này

## 10. Nhận thức rủi ro bảo mật code đối với AI

E nghĩ cần nhận thức được những rủi ro sau

* thứ nhất là package hallucinate: AI có thể gợi ý package ko tồn tại hoặc gần giống package thật, hacker có thể giả mạo lợi dụng tạo package chứa mã độc. trước khi dùng cần kiểm tra kỹ tuổi đời, nguồn gốc, tác giả, số download, scan với lệnh dotnet
* thứ 2 là AI hardcode secret key hoặc log thông tin nhạy cảm: cần scan secret trong CI và tránh dùng thông tin thật của khách hàng
* thứ 3 là AI dùng những kiến thức, api, thuật toán mã hóa đã lỗi thời. nên có file system rule để chặn AI dùng những hàm, thư viện lỗi thời
* thứ 4 là Gen code không bảo mật: code AI trông rất hợp lý nhưng có thể dùng các pattern thiếu an toàn như sql injection, redos, thiếu sanitization
* thông tin thêm:
  * redos là 1 cách DOS bằng việc gửi 1 chuỗi độc hại vào regex viết không cẩn thận, dẫn đến việc regrex quay lui (backtracking) khi 1 chuỗi không khớp thì sẽ thử nhánh khác. giải quyết bằng cách set timeout hoặc option NonBacktracking của .NET 7 trở lên
  * thiếu sanitization là việc tin tưởng input người dùng. bị hacker đưa mã html có thể bị XSS, hoặc dấu slash đường dẫn dẫn đến path traversal. cách giải quyết là validate allow-list tùy vào bối cảnh. thư viện htmlsanitizer hoặc encode output trước khi hiển thị

## 11. Những rule mà nên đặt cho team dev khi dùng AI?

có thể chia làm 4 cổng chính:

* cổng input
  * không đưa thông tin nhạy cảm như secret, api key, dữ liệu khách hàng vào AI
  * không tự ý dùng các tool AI khác mà chưa có thỏa thuận enterprise, privacy bảo vệ bản quyền của công ty
* cổng process
  * đối với bất kỳ task hay bug cũng cần plan và debate với AI để ra file action plan. sau đó review trước khi gen code. Chi phí sửa plan luôn rẻ hơn chi phí sửa code sai
  * Test do người định đoạt: phải tự xác định test case dựa trên AC, sau đó mới yêu cầu AI viết unit test. tránh trường hợp AI hợp thức hóa code sai
* cổng output
  * Dev phải giải thích được code do AI sinh ra. đặc biệt là những kỹ thuật, security. ko giải thích được ko merge
  * đặt các chốt chặn tự động trong CI/CD: scan SAST, DAST. Áp dụng nguyên tắc cốt lõi là "Shift Left", đẩy bảo mật về phía bên trái để phát hiện và sửa lỗi ngay khi vừa code xong (quy trình chuẩn là: planning > coding > CI pipeline > deployment > production)
* cổng học tập
  * mỗi khi fix bug phức tạp hoặc edge case nguy hiểm thì bắt buộc update vào lesson learned theo cấu trúc "vấn đề - root cause - cách giải quyết" để lần sau AI và team không bị lại nữa

sẽ có trường hợp dev sẽ ko tuân thủ và vi phạm, cho nên rule chỉ có tác dụng khi hầu hết các cổng nên dùng cách tự động hóa

* cổng input: dùng công cụ AI enterprise có chính sách no-training. file ignore cho AI. chỉ dùng dữ liệu giả
* cổng output: có pipeline CI để bắt buộc pass test case và scan SAST
* quy tắc con người duy nhất cần giữ nghiêm là khi review PR, nếu dev ko giải thích được cơ chế hoạt động của code do AI sinh ra thì reject PR

## 12. Làm sao e biết được context file, prompt viết cho AI đọc có hiệu quả không?

* có thể đo lường qua tầng suất lặp lại lỗi (regression / repetition rate). nếu cùng 1 lỗi cũ mà sau khi có rule vẫn mắc phải nghĩ là prompt/rule bị loãng (context window drift hoặc lost in the middle). e cần phải refactor lại rule ngắn gọn, có ví dụ, hoặc chuyển rule đưa vào CI/CD

## 13. DEV nên có những nhận thức gì trong thời đại AI?

theo e thì trong thời đại AI thì năng lực của senior ko còn nằm ở tốc độ gõ code, mà nằm ở 3 nhận thức cốt lõi sau

* thứ nhất là trách nhiệm giải trình

  * AI dẫn đường còn dev thì cầm lái. AI có thể gợi ý 90% code, nhưng người commit và chịu trách nhiệm khi hệ thống sập trên production luôn là dev. cũng vì vậy dev nên giữ nguyên tắc là ko bao giờ hỏi AI "cách fix lỗi" trước mà phải hỏi "root cause" trước. cần phải biết "tại sao" trước khi hỏi "làm thế nào"
* thứ hai là kỹ năng xác minh quan trọng hơn kỹ năng code

  * Code AI sinh ra thường có bẫy nguy hiểm là False Confidence, nhìn rất sạch, compile chạy ngay nhưng tiềm ẩn edge case, sai business logic. Nếu dev ko hiểu mà merge vội thì thường tạo ra nợ vibe coding. vì vậy, dev cần biết cách kiểm soát AI, biết AI làm gì, biết xác minh và quyết định, biết từ chối output sai trông như đúng
* thứ ba là tư duy quy trình và hàng rào kiểm soát

  * Hiện tại AI phát huy sức mạnh khi được đặt trên 1 đường ray chuẩn. Thay vì làm việc theo cảm tính lúc nhớ lúc quên thì nên rút ra những quy trình làm việc cho bản thân và AI để đi đúng và nhanh hơn. xây những chốt chặn để bảo mật, ko paste secret key, dữ liệu nhạy cảm của khách hàng vào AI. tạo hàng rào CI/CD để đảm bảo cả người và AI đều làm đúng chuẩn

## 14. Những từ khóa liên quan đến AI nên nhận thức được là gì?

* context window drift, hallucinate, lost in the middle
* context window drift: là khi chat quá dài, context bị phình to khiến AI dần quên đi các system prompt, coding rule ban đầu và bắt đầu sinh code lệch chuẩn
* hallucinate: là AI tự bịa ra thông tin, logic ko có thật
* lost in the middle: là hiện tượng AI chú ý ở phần đầu và cuối của prompt dài, còn phần giữa dễ bị bỏ sót

## 15. Tư duy system view và detail view

e nghĩ là dev nên có tư duy system view và detail view. system view để xem dòng chảy dữ liệu, các thành phần được kết nối thế nào, input, process, output của các thành phần, từ đó có khả năng nhận thức được khả năng impact. còn detail là để biết những gì mình làm có đúng chưa

ví dụ khi fix bug thì ban đầu e dùng system view vì lỗi có thể nằm ở những thành phần khác, khi khoanh vùng được thì bắt đầu dùng detail view để fix. khi fix xong thì dùng system view để xem lại có impact những thành phần khác ko

## 16. Tư duy inversion thinking

e thấy dev nên có tư duy inversion thinking. thay vì hỏi làm sao để chức năng chạy tốt, thành công thì tư duy này chủ yếu dùng để tìm các lỗi khiến cho chức năng bị chạy sai, bị thất bại. tư duy này có chút thiên về phòng thủ, có thể làm chậm quá trình nhưng thật sự tốt nếu áp dụng có chừng mực

ví dụ: thay vì hỏi làm sao để AI tư vấn tour hay? thì mình sẽ hỏi "điều gì khiến agent phá hỏng trải nghiệm người dùng". câu trả lời sẽ là bịa giá tour, confirm là book lịch tư vấn rồi nhưng db chưa lưu. từ đó e thiết kế tool guardrail

## 17. Những từ khóa quan trọng trong SDLC nên nhận thức là gì?

e nghĩ có thể chia làm 3 nhóm chính:

* nhóm 1, Business Value đảm bảo giải quyết đúng nỗi đau của khách hàng. gồm có từ khóa: business logic, AC, Accuracy
* nhóm 2, resilience và non-functional đảm bảo hệ thống an toàn, sống sót và tự phục hồi khi có sự cố. gồm có từ khóa: Security, Performance, scalability, observability
* nhóm 3, Maintainability & life-cycle đảm bảo code sạch, dễ bảo trì, sống khỏe qua nhiều chu kỳ thay đổi công nghệ. gồm có từ khóa: Impact analysis, Backward compatibility, Testability, code smell
* mindset đúc kết được là: junior sẽ tập trung cho nhóm 1 làm sao cho code chạy . senior sẽ chịu cả 3 nhóm, điểm khác là senior nhìn thấy nhóm 2 và 3 trước và là người gác cổng để hệ thống ko sập trên production và dự án dễ bảo trì về sau

thông tin thêm:

* tưởng tượng phần mềm giống như 1 chiếc xe máy. Business value là xe phải nổ máy được, vặn ga chạy được. đây là lý do cốt lõi mà khách bỏ tiền mua chiếc xe. Resilience & non functional là xe chạy êm không? có bền không? Maintainability & life cycle là sau 3-5 năm xe bị hư thì có phụ tùng để thay ko? thợ bình thường có dễ dàng sửa ko hay phải mua lại xe mới?
* bản chất và mục đích của nhóm Resilience & non-functional là bảo đảm hệ thống sống sót, chạy ổn định và an toàn trong môi trường thực tế khắc nghiệt, chứ ko chỉ chạy được trên máy local của dev
  * Functional: trả lời cho câu hỏi "hệ thống làm được gì?"
  * Non-functional: trả lời cho câu hỏi "hệ thống làm điều đó tốt như thế nào?"
  * Resilience dịch nghĩa là có khả năng chống chịu, tự phục hồi và bật trở lại sau sự cố
    * khi 1 bộ phận hỏng thì hệ thống ko chết chùm (graceful degradation)
    * có cơ chế thử lại thông minh (retry với exponential backoff), hoặc ngắt mạch khi third-party sập hẳn (circuit breaker)
    * có khả năng tự chữa lành, tự khôi phục khi third-party hoạt động trở lại
  * bản chất và mục đích của nhóm maintainability: là đo lường mức độ dễ dàng khi 1 lập trình viên khác đọc code, thêm, sửa, bảo trì tính năng. giữ cho tốc độ phát triển không bị chậm dần theo thời gian. tránh sửa 1 dòng ở module này thì bị lỗi tính năng ở module khác
  * bản chất và mục đích của life-cycle là: nhận thức được toàn bộ quá trình từ lúc code sinh ra > lên production > vận hành > nâng cấp > khai tử
    * khi chạy trên production có log rõ ràng để trace bug ko? biết hệ thống khỏe hay yếu?
    * khi nâng cấp thì có làm phiên bản cũ bị crash ko?
    * trong clean architecture khi tách domain ra khỏi EF Core chính là đang nhìn vào life-cycle, vòng đời của business logic kéo dài hàng chục năm. trong khi vòng đời của công nghệ thì chỉ kéo dài vài tháng hoặc vài năm. tách rời giúp hệ thống sống sót qua nhiều chu kỳ thay đổi công nghệ
  * Bản chất Impact Analysis là dự đoán trước toàn bộ hiệu ứng dây chuyền mà 1 thay đổi nhỏ trong code có thể gây ra cho hệ thống trước khi code
    * ví dụ ẩn dụ trong trò rút gỗ, junior thấy thanh gỗ sẽ rút ngay mà ko nhìn xung quanh tháp. còn senior sẽ quan sát nếu mình rút thanh này thì những thanh nào sẽ có nguy cơ làm tháp bị sụp
    * senior nên quan sát impact ở 3 tầng
      * tầng 1 là code: sửa hàm hoặc interface có lỗi logic khác ko
      * tầng 2 là database: thêm hoặc thay đổi column thì có ảnh hưởng data cũ ko?
      * tầng 3 là integration và system: sửa response API thì UI có bị lỗi ko

## 18. Những loại bẫy dev nên nhận thức được?

* bẫy tự hợp thức hóa lỗi:
  * hiện tượng là dev dùng AI gen tính năng A, code đó bị thiếu 1 edge case, ví dụ tour hết chỗ nhưng vẫn book được. sau đó mình yêu cầu AI viết unit test cho chức năng book tour
  * hậu quả là AI sẽ đọc code hiện tại và viết unit test cho logic bị sai. test chạy xanh lè nhưng khách vẫn book được tour khi đã hết chỗ
  * cách giải quyết là dev ko chỉ yêu cầu mơ hồ "viết unit test cho chức năng đó". mà cần yêu cầu chi tiết hơn bằng cách đưa danh sách test case cụ thể dựa vào AC. sau khi AI generate xong thì dev sẽ soi kỹ phần Assert xem kết quả có đúng logic ko. việc đưa test case dựa vào AC cũng là 1 lần giúp dev đọc và nắm kỹ business logic hơn

## 19. E dùng AI để học kiến thức mới như thế nào?

e áp dụng mô hình input - process - output

* input là e sẽ đọc tài liệu
* process là sẽ dùng AI học chung, hỏi những câu như là bản chất? mục đích? cách hoạt động? ưu điểm, nhược điểm? trade-off? dùng tốt và ko tốt khi nào?
* output là sẽ tạo ra 1 project để thấy được cách hoạt đông của những kiến thức đó

## 20. Làm sao để e biết được là mình đã hiểu và tưởng đã hiểu

thường thì e sẽ có 3 cách để test

1 là e sẽ thử dự đoán trường hợp sẽ xảy ra. 2 là tự giải thích được cho người khác. 3 là nắm được ưu điểm, nhược điểm, tại sao dùng

## 21. Các chân lý trong lập trình

* mọi thứ đều có thể fail
  * tại sao mọi thứ đều có thể fail?
    * 1 chương trình chạy và phụ thuộc nhiều vào rất nhiều yếu tố bên ngoài mà lập trình viên ko thể kiểm soát
      * phần cứng có thể hỏng như cpu quá tải, thiếu bộ nhớ
      * third party bị sập: api bị lỗi, dịch vụ cloud gặp sự cố
      * người dùng nhập liệu sai
      * lập trình viên handle thiếu edge case
    * cách để giải quyết đối với những vấn đề này là
      * phòng thủ khi viết code: luôn kiểm tra dữ liệu đầu vào như null, undefined trước khi dùng
      * handle exception tại middleware, global handler
      * cơ chế tự phục hồi: nếu server sập thì chuyển hướng user sang server phụ
      * thiết kế decoupling: chia nhỏ hệ thống, lỗi chức năng này ko được phép ảnh hưởng chức năng khác
      * log và monitor: để giám sát ngay khi có lỗi xảy ra, biết chính xác lỗi ở dòng code nào để sửa
* code chỉ được viết 1 lần, nhưng đọc hàng trăm lần
  * điều này dẫn đến nguyên lý cốt lõi là: sự đơn giản luôn quan trọng hơn sự thông minh (simplicity over cleverness)
    * nguyên lý này được cụ thể qua 1 định luật: bất kỳ kẻ ngốc nào cũng có thể code mà máy hiểu được. lập trình viên giỏi code mà con người có thể hiểu được
    * KISS
    * YAGNI
    * premature optimization (tối ưu hóa sớm là nguồn cơn của mọi tội lỗi)

---

## 21. AI gen 90% code rồi, tại sao team cần senior mà không cần junior + AI?

chi phí của việc gõ code đúng là đang rẻ, nhưng phần đắt lên là quyết định và kiểm chứng đúng hay chưa và chịu trách nhiệm khi sai

* trước code thì cần hiểu yêu cầu: phát hiện những điểm mơ hồ và làm rõ. chọn giải pháp theo ưu điểm, nhược điểm, trade-off theo dự án. junior + AI có thể làm code chạy nhưng dính nhiều bẫy như là nợ kỹ thuật, leak data, sai business logic, nhưng senior + AI thì sẽ nhìn được rộng hơn so với junior, biết cách đặt câu hỏi đúng, khoanh vùng được những bẫy ngầm sau khi code
* sau code thì cần kiểm chứng: AI thường tạo ra code sai nhưng trông như đúng, người review phải biết lỗi nằm ở đâu

## 23. Estimate một task khi dùng AI khác gì trước đây?

* lúc trước cần estimate thời gian hiểu + code + test. nhưng bây giờ phần code đã giảm lại nhiều, nhưng phần hiểu và test lại thì không đổi, có khi còn tăng vì phải review code mình ko gõ

## 24. e làm việc với legacy code không có test thì phải làm thế nào, AI giúp gì ở đó?

* e nghĩ tư duy là nên có lưới an toàn trước khi làm với legacy code, nếu ko có thì cần phải tạo
* đọc hiểu: dùng system view để tóm tắt các module, dependency, input/output của mỗi module
* ghi lại những hành vi hiện tại kể cả hành vi sai: có thể xem như là snapshot
* chỉ refactor trong vùng có lưới, làm từng bước nhỏ, chạy test mỗi bước

## 27. Khi em và AI bất đồng về giải pháp, em quyết định dựa trên gì?

AI thường sẽ đưa giải pháp phổ biến, còn e thì sẽ muốn giải pháp phù hợp và tối ưu cho dự án

* Có thể yêu cầu AI đưa ra những giả định đang dựa vào, việc này giúp e biết được AI đang dựa trên những thông tin, tiêu chí nào để đưa ra giải pháp
* Khi bất đồng giải pháp thì thường có thể là do đưa context chưa đủ để AI đánh giá và gợi ý
* Nếu đủ context mà vẫn bất đồng thì không nên tranh luận nhiều mà nên tạo test, poc nhỏ để thấy được output. từ đó có thể đánh giá xem giải pháp nào tối ưu và phù hợp với dự án hơn

## 28. Nếu team phản đối quy trình AI của em quá rườm rà, em sẽ làm gì?

* Cần biết lắng nghe, team có thể đúng, hãy coi đây là tín hiệu tốt
* xác định bước nào bị phản đối để giải quyết
* tìm nguyên nhân bằng cách đặt nhiều câu hỏi "tại sao". biết điều gì sẽ làm giảm ma sát để phù hợp với team
* đánh giá chi phí thử

  * nếu rẻ và dễ đảo ngược thì thử ngay, ko tranh luận nhiều
  * nếu mắc và khó đảo ngược thì cần phân tích và đánh giá kỹ hơn về chi phí/rủi ro. thử nhỏ + metric để biết kết quả nhanh
* nếu kết quả có cải thiện và hiệu quả thì giữ lại. nếu không thì cải thiện tiếp

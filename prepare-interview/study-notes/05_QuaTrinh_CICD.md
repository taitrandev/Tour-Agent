## note quá trình làm. sau khi note xong thì soạn lại kịch bản, những gì đã làm

* **command thường dùng**
  * dotnet build ./dotnet-backend-core-main/TourAgent.slnx
  * dotnet run --project ./dotnet-backend-core-main/src/TourAgent.Api/TourAgent.Api.csproj
  * `%APPDATA%\Microsoft\UserSecrets\a7e61eaa-b039-4e2d-a891-7ed59e13e2a1\secrets.json`
  * docker build -t touragent-api:local -f ./dotnet-backend-core-main/Dockerfile ./dotnet-backend-core-main
  * docker images touragent-api:local
  * docker run -d --name touragent-test -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=tour_agent_db;Username=postgres;Password=secretpassword" touragent-api:local
* **TODO:**
  * hiện tại các biến môi trường nằm rải rác trong code, cần review lại
  * soạn lại câu trả lời về: (kết luận - tại sao - trade off- ngươc lại - ví dụ)
    * giai đoạn 0 làm những việc gì
    * health check
  * hỏi về việc build image giống như gì? cần những gì? từ manual hiểu thì mới chuyển qua dockerfile đúng không?

lúc chuẩn bị CI/CD thì e có các 4 giai đoạn. thứ nhất là bảo đảm source code sẵn sàng cho môi trường cloud. thứ 2 là CI. thứ 3 là CD

## Giai đoạn 0: chuẩn bị source code

1. giai đoạn 1: cloud ready. mục đích trong giai đoạn này là bảo đảm source code cần sẵn sàng cho môi trường cloud (vẫn hardcode, localhost, thiếu health check) thì pipeline CD/CD tốt thế nào thì cũng bị sập (crasCrashLoopBackOff hoặc 500 error)

* sau khi xong:
* appsetting

  * xóa các hardcode, key trong appsetting. tạo appsetting riêng cho dev
  * thêm section CORS để chứa danh sách mặc định
    * cơ chế CORS local vs cloud:
      * ở local khi chạy dotnet run với môi trường Development thì app sẽ đọc json Cors:AllowedOrigins trong appsettings.json
      * ở cloud thì sẽ dùng các biến môi trường như là CORS_ALLOWED_ORIGINS
* **nhận thức vấn đề thêm các enviroment variable trên cloud. cần file checklist khi deploy để thêm đủ các biến**
* **tạo skill build ci cd và cập nhật từ từ, mỗi giai đoạn, mỗi bước thì update lại**

  * **thêm endpoint /health**
    * mục đích để kiểm tra các thành phần trong hệ thống có khỏe ko. ví dụ database, LLM, tài nguyên như là ổ cứng, RAM
    * dùng extension healcheck của Microsoft, vì nó biến health check thành 1 pipeline tổng hợp, cho phép kiểm tra song song nhiều thành phần sau đó tổng hợp lại
    * cơ chế hoạt động của code là là khi gọi endpoint health, thì tất cả các class implement interface IHealCheck và được đăng ký AddCheck sẽ được kích hoạt. khi tất cả class check xong thì kết quả được tổng hợp lại. Nếu tất cả healthy thì status sẽ là healthy và 200. chỉ cần 1 cái UnHealthy thì status sẽ là UnHealthy và 503
    * ưu điểm:
      * azure app service có cơ chế thăm dò (probe), dựa trên status code để quyết định  tự động khởi động lại container bị treo hoặc ngắt container bị lỗi
      * tích hợp sẵn cancellationtoken và timeout riêng. nếu check phản hồi chậm thì việc thăm dò sẽ bị ngắt để tránh tình trạng request làm nghẽn luồng xử lý chính
    * nhược điểm là:
      * phải thêm dll. Cần setup nhiều. rủi ro tạo áp lực lên db, azure gửi thăm dò quá nhiều lần thì sẽ tiêu tốn nhiều connection trong pool
    * xem thêm trong file [Health-Check](D:\github\Tour-Agent\prepare-interview\study-notes\05_QuaTrinh_CICD_HealthCheck.md)

## Giai đoạn 1 :CI

### 1. hiểu ACR và container app trong CI CD

CI sẽ: build => unit test => docker build image => push ACR

CD sẽ: az login => az containerapp update ()

mô tả flow theo văn nói về ACR và ContainerApp:

* ACR như 1 cái khó để chứa image. CI build xong thì sẽ cất image mới vào kho, có dán nhãn vd: commit ID abc456. Xong, ko báo ai hết
* Container App là cái máy đang chạy. trong máy có ghi tờ giấy cho biết  là đang chạy image abc123. lúc khởi động thì máy sẽ đọc tờ giấy đó, lấy image ra kho và chạy. sau đó ko quay lại kho để kiểm tra nữa
* "az containerapp update ...:abc45" là cầm viết sửa tờ giấy thành abc456, máy thấy tờ giấy bị đổi thì sẽ tạo 1 revision, vào kho lấy abc456 để chạy. khi thấy bản mới chạy ổn thì mới chuyển người dùng qua bản mới
* pipeline hiểu đơn giản sẽ có 2 câu:
  * CI: "đóng gói, kiểm tra, ném vào kho" => docker push
  * CD: "máy ơi, chạy gói mới nhất trong kho đi" => az containerapp update

những cách khác để sửa tờ giấy ngoài "az containerapp update"

* sửa trên portal: tự tay bấm
* bicep/terraform: sửa cấu hình bằng code
* bật CD trên portal: azure sinh ra workflow github action, và workflow cũng update, nhưng sẽ bị giấu đi

những lưu ý:

* phải dùng tag khác như là commitID: nếu tag là latest thì sẽ ko tạo revision mới mà sẽ vẫn chạy bản cũ
* secrect và env ko mất khi update image: secret nằm ở cấp app, còn env var nằm trong template và được chép sang revision mới. Vì vậy pipeline chỉ cần biết tên image, không cần biết secret

### 2. Tạo User-Assigned Managed Identity

**Tại sao không dùng ACR Admin Password hay Service Principal Client Secret?**

* **Zero Stored Secrets**: GitHub Actions không lưu bất kỳ password nào. Chỉ lưu 3 ID công khai (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`). Token đăng nhập là JWT ngắn hạn (~1 giờ) do GitHub tự ký và Entra ID tự kiểm tra chữ ký.
* **Nhất quán 1 cơ chế cho cả CI và CD (Chuẩn 80/20)**: Nếu dùng ACR Password, bạn chỉ push được image lên ACR ở bước CI. Đến bước CD (`az containerapp update`), bạn lại phải làm thêm một cơ chế auth khác. Dùng Managed Identity với 2 role (`AcrPush`cho ACR và `Contributor`cho Resource Group) sẽ giải quyết trọn vẹn cả CI lẫn CD.

#### 1. Tạo Managed identity bằng portal theo file (create-oidc-managed-identity.md)

##### hiểu về OIDC, Managed Identity, Entra ID, RBAC

* Bắt đầu tạo Managed Identity
* vào Identity tạo tiếp Federated Credential
* gán quyền RBAC (acrPush trên ACR và Container App Contributor trên Resource Group)
* lấy ClientID, TenantID, SubscriptionID, acr name điền vào Github Repository variable

---

### 🐳 Docker Multi-Stage Build (.NET 10) — Kiến Thức Phỏng Vấn Cốt Lõi

1. **Tại sao dùng Multi-Stage Build?**

   - **Tách biệt môi trường build và runtime**: Stage 1 (`sdk:10.0`) chứa đầy đủ compiler, công cụ build nặng ~1GB. Stage 4 (`aspnet:10.0`) chỉ chứa runtime tối giản để chạy DLL, nặng chỉ **102MB**.
   - **Bảo mật**: Production không có compiler (`csc`) hay package manager (`nuget`), hacker không thể biên dịch mã độc nếu xâm nhập container.
2. **Cơ chế Docker Layer Caching (Tối ưu thời gian build CI/CD):**

   - **Nguyên tắc**: Docker cache từng layer lệnh theo thứ tự từ trên xuống dưới. Nếu layer trên thay đổi, toàn bộ layer dưới phải build lại.
   - **Chiến thuật 80/20**:
     ```dockerfile
     # BƯỚC 1: Chỉ copy file .csproj và Directory.Build.props
     COPY ["Directory.Build.props", "./"]
     COPY ["src/TourAgent.Api/TourAgent.Api.csproj", "src/TourAgent.Api/"]
     # ...
     # BƯỚC 2: Chạy dotnet restore (Tải package từ NuGet)
     RUN dotnet restore "src/TourAgent.Api/TourAgent.Api.csproj"

     # BƯỚC 3: Mới copy toàn bộ mã nguồn .cs còn lại
     COPY src/ src/
     ```
   - **Lợi ích**: Khi lập trình viên chỉ sửa code `.cs` mà không thêm package NuGet mới, Docker nhận diện cache ở Bước 1 & 2 còn nguyên ➔ **Bỏ qua bước restore**, nhảy thẳng vào build giúp pipeline CI/CD chạy nhanh gấp 5 lần!
3. **Bảo mật Non-Root User (`USER $APP_UID`):**

   - Từ .NET 8 trở đi, Microsoft cung cấp user `app` (UID: 1654) không có quyền root.
   - Lắng nghe mặc định cổng `8080` (thay vì 80) vì Linux yêu cầu quyền root mới bind được cổng dưới 1024.
   - Giúp giảm thiểu rủi ro leo thang đặc quyền (Privilege Escalation).
4. **Quản lý Secrets giữa Local và Cloud:**

   - **Local**: Sử dụng **.NET Secret Manager (User Secrets)** lưu tại `%APPDATA%\Microsoft\UserSecrets\<Id>\secrets.json`, nằm ngoài repo Git nên không sợ leak.
   - **Cloud (Production)**: User Secrets tự động vô hiệu hóa. Ứng dụng nạp trực tiếp từ **Environment Variables** (Azure Application Settings) với quy tắc ánh xạ `__` (ví dụ `ConnectionStrings__DefaultConnection`).

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

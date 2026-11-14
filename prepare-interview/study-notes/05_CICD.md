## note quá trình làm

* yêu cầu hướng dẫn CI/CD pipeline theo nguyên lý 80/20 và cho 1 bản pipeline mvp để nắm được CI/CD cần có gì

1. giai đoạn 0: cloud ready. mục đích là source code cần sẵn sàng cho môi trường cloud (vẫn hardcode, localhost, thiếu health check) thì pipeline CD/CD tốt thế nào thì cũng bị sập (crasCrashLoopBackOff hoặc 500 error)




Khi đưa dự án này vào CV và đi phỏng vấn, các interviewer thường sẽ hỏi 5 câu cốt lõi sau. Nắm chắc 5 câu này là đạt 80% điểm phỏng vấn CI/CD:

### **Câu 1: Em hãy mô tả luồng CI/CD mà em đã thiết lập cho dự án TourAgent?**

* **Cách trả lời ăn điểm** :

> *"Em thiết lập workflow bằng GitHub Actions chia làm 2 giai đoạn: CI (Continuous Integration) và CD (Continuous Deployment).*
> *Khi tạo Pull Request vào `main`, CI sẽ tự động restore, build và chạy toàn bộ Unit Tests cùng Architecture Tests (`NetArchTest`). Điểm đặc biệt là Architecture Test đảm bảo không có ai vi phạm Clean Architecture (ví dụ: Domain không tham chiếu tầng ngoài) trước khi merge.*
> *Khi code được merge vào `main`, CD được kích hoạt: ứng dụng được publish dưới dạng Release package và deploy tự động lên Azure App Service Linux qua Azure WebApp Deploy Action."*

### **Câu 2: Em quản lý Connection String và API Key (DeepSeek, Telegram) như thế nào để không bị lộ bí mật?**

* **Cách trả lời ăn điểm** :

> *"Tuyệt đối không commit credentials vào Git. Ở môi trường local dùng `appsettings.Development.json` (được đưa vào `.gitignore`) hoặc `User Secrets`.*
> *Trên CI/CD: Sử dụng GitHub Secrets để lưu thông tin xác thực deploy (Publish Profile/Service Principal).*
> *Trên Cloud: Cấu hình trực tiếp vào **Environment Variables (App Settings)** của Azure App Service. ASP.NET Core hỗ trợ quy tắc `__` để map vào file config (ví dụ `ConnectionStrings__DefaultConnection`). Ở cấp độ production lớn hơn, em sẽ dùng **Azure Key Vault** kết hợp **Managed Identity** để app tự lấy secret mà không cần connection string dạng plain-text."*

### **Câu 3: Làm sao để xử lý Database Migration khi triển khai tự động lên Azure?**

* **Cách trả lời ăn điểm** :

> *"Có 2 cách tiếp cận chính:*
> *1. **Tự động apply khi ứng dụng khởi động** (`db.Database.MigrateAsync()`): Phù hợp cho MVP/dự án nhỏ, chạy đơn giản nhưng có rủi ro nếu hệ thống scale nhiều instance (gây race-condition).*
> *2.  **EF Core Migration Bundles / Script trong CI/CD pipeline** : Tạo ra file script `.sql` hoặc file binary bundle thực thi migration trước khi deploy app mới. Nếu migration lỗi, pipeline sẽ dừng lại ngay, bảo vệ hệ thống không bị lỗi runtime."*

### **Câu 4: Làm thế nào để đạt Zero-Downtime Deployment trên Azure App Service?**

* **Cách trả lời ăn điểm** :

> *"Trên Azure App Service (từ tier Standard trở lên), ta sử dụng tính năng **Deployment Slots** (ví dụ slot `staging` và slot `production`).*
> *Pipeline sẽ deploy bản mới vào slot `staging`. Khi staging khởi động xong và vượt qua các bài kiểm tra Health Check (Warm-up), hệ thống sẽ thực hiện **Slot Swap** để đổi staging thành production ngay lập tức mà người dùng không gặp downtime."*

### **Câu 5: Nếu sau khi deploy phiên bản mới phát sinh lỗi nghiêm trọng, em rollback thế nào?**

* **Cách trả lời ăn điểm** :

> *"Nếu dùng Deployment Slots, em chỉ cần thực hiện **Swap ngược lại** về slot trước đó, mất dưới 1 phút.*
> *Nếu dùng GitHub Actions không có slot, có thể cấu hình pipeline rollback bằng cách re-run workflow của commit stable trước đó, hoặc revert commit trên Git để trigger pipeline deploy lại bản cũ."*
>

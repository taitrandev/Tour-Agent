# Bài Học Kinh Nghiệm (Lessons Learned) — Giai Đoạn Continuous Deployment (CD)

Tài liệu này ghi lại các bài học kiến trúc và sự cố kỹ thuật cốt lõi gặp phải trong quá trình triển khai backend `TourAgent.Api` lên **Azure Container Apps (ACA)**.

---

## 1. Bài Học 1: Phân Định Managed Identity Cấp Environment vs. Cấp App

### Triệu chứng & Lỗi gặp phải

Khi cố gắng cấu hình xác thực kéo image từ Azure Container Registry (ACR), Azure từ chối và quăng mã lỗi:

```text
ExpressEnvironmentManagedIdentityNotSupported: Environment-level managed identity is not supported for environment 'cae-touragent-prod' on express environments.
```

### Bản chất kỹ thuật

Azure Container Apps phân tách rõ ràng 2 tầng kiến trúc:

| Tầng kiến trúc                                             | Bản chất                                                                                                                            | Hỗ trợ Managed Identity trên gói Serverless (Consumption / Express)?                                                                     |
| :------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------- |
| **Container Apps Environment** (`cae-touragent-prod`) | Là **Cụm hạ tầng ảo (Cluster/Namespace)** dùng chung cho nhiều ứng dụng. Quản lý mạng ảo (VNet) và Log Analytics. | ❌ **KHÔNG HỖ TRỢ**. Azure chỉ cho phép gán Identity ở cấp Environment trên các gói Dedicated Workload Profile đắt tiền. |
| **Container App** (`touragent-api`)                   | Là **Ứng dụng / Microservice cụ thể** chạy trực tiếp container của bạn.                                               | ✅ **HỖ TRỢ 100%**. Cho phép gán cả User-Assigned Managed Identity lẫn System-Assigned Identity.                                 |

### Tại sao nên dùng App-level Identity? (Góc nhìn DevOps & Security)

1. **Nguyên tắc Đặc quyền Tối thiểu (Principle of Least Privilege)**:
   - Nếu gán Identity ở cấp Environment, mọi ứng dụng chạy trong cụm đó đều sở hữu chung quyền kéo image từ ACR hoặc đọc Key Vault, dẫn đến nguy cơ leo thang đặc quyền (Privilege Escalation).
   - Gán Identity ở cấp App đảm bảo chỉ duy nhất `touragent-api` mới có quyền `AcrPull` trên registry `touragentacr`.
2. **Chuẩn tự động hóa (Automation / IaC)**:
   - Trong script tự động hóa (`create-container-app.sh`), luôn khai báo định danh gán thẳng vào Container App thông qua 2 cờ:
     ```bash
     --user-assigned "$IDENTITY_ID" \
     --registry-identity "$IDENTITY_ID"
     ```



Để dễ hình dung nhất, bạn hãy tưởng tượng mô hình  **Tòa nhà Chung Cư** :

<pre dir="ltr"><div dir="ltr" class="relative whitespace-pre-wrap word-break-all my-2 rounded-xl bg-muted border"><div class="min-h-7 relative box-border flex flex-row items-center justify-between rounded-t border-b border-border px-2 py-0.5"><div class="font-sans text-base text-muted-foreground"></div><div class="flex flex-row items-center gap-2 justify-end"></div></div><div class="p-3"><div dir="ltr" class="code-block w-full text-sm cursor-text  "><div class="flex flex-col w-full"><div data-index="0" data-line-number="1" data-line-start="1" data-line-end="1" class="code-line "><div class="line-content"><span class="">[ Container Apps Environment: cae-touragent-prod ]  <--- TÒA NHÀ CHUNG CƯ (Hạ tầng mạng, Log Analytics)</span></div></div><div data-index="1" data-line-number="2" data-line-start="2" data-line-end="2" class="code-line "><div class="line-content"><span class=""> │</span></div></div><div data-index="2" data-line-number="3" data-line-start="3" data-line-end="3" class="code-line "><div class="line-content"><span class=""> ├── [ Container App 1: touragent-api ]            <--- Căn hộ 1 (Backend của bạn)</span></div></div><div data-index="3" data-line-number="4" data-line-start="4" data-line-end="4" class="code-line "><div class="line-content"><span class=""> └── [ Container App 2: touragent-frontend ]       <--- Căn hộ 2 (Frontend SPA sau này)</span></div></div></div></div></div></div></pre>

#### **🏢 Cấp Environment (Environment-level Identity) = "Thẻ từ chung của cả tòa nhà"**

* Là danh tính đại diện cho toàn bộ cụm hạ tầng. Mọi container chạy bên trong tòa nhà này đều dùng chung danh tính đó.
* **Tại sao bị lỗi?** Gói bạn đang dùng là  **Consumption / Express (Serverless - miễn phí/siêu rẻ)** . Ở gói này, Azure chia sẻ hạ tầng động (Multi-tenant) nên **Microsoft chủ động khóa tính năng Identity ở cấp Environment** để tránh xung đột tài nguyên. Nếu chọn, Azure sẽ chặn ngay: `ExpressEnvironmentManagedIdentityNotSupported`.  *(Chỉ gói Dedicated đắt tiền mới có)* .

#### **🚪 Cấp App (App-level Identity) = "Chìa khóa riêng của từng căn hộ"**

* Là danh tính gán thẳng vào từng Container App cụ thể (`touragent-api`).
* **Chuẩn bảo mật (Least Privilege)** : Căn hộ nào cần kéo image từ kho `touragentacr` thì chỉ căn hộ đó được giữ chìa khóa, không ảnh hưởng đến các app khác.
* Gói Serverless/Express hỗ trợ 100% cấp này.

> **💡 Vì sao lúc đầu bấm tay trên Portal không thấy dòng `(app)`?**
> Vì khi bạn đang ở màn hình tạo mới, **App đó chưa tồn tại** nên nó chưa thể sở hữu identity.
> Nhưng khi bạn chạy file bash (`create-container-app.sh`), lệnh CLI đã dùng cờ `--user-assigned $IDENTITY_ID` để vừa tạo app vừa trao luôn chìa khóa vào tay app. Kết quả là ở ảnh mới nhất bạn chụp, Portal đã nhận diện được mục chuẩn xác:  **`id-github-touragent-deploy (app)`** !
>

---

## 2. Bài Học 2: Xung Đột CORS Giữa Wildcard `*` và `AllowCredentials()`

### Triệu chứng & Lỗi gặp phải

Sau khi deploy thành công container lên Azure, khi gọi endpoint `/health/live` thì nhận được thông báo:

```json
{"error": "App port is not ready"}
```

Khi soi log container (`az containerapp logs show` hoặc log console), phát hiện exception chí mạng:

```text
crit: Microsoft.AspNetCore.Hosting.Diagnostics[6]
Application startup exception
System.InvalidOperationException: The CORS protocol does not allow specifying a wildcard (any) origin and credentials at the same time. Configure the CORS policy by listing individual origins if credentials needs to be supported.
   at Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder.Build()
   at Program.<Main>$(String[] args) in /app/src/TourAgent.Api/Program.cs:line 37
```

### Bản chất kỹ thuật (Tiêu chuẩn Bảo mật W3C CORS)

1. **Tại sao Kestrel / ASP.NET Core ném ngoại lệ làm crash app?**
   - Theo đặc tả bảo mật của tổ chức W3C và chuẩn CORS của mọi trình duyệt hiện đại: **Khi một request yêu cầu gửi kèm thông tin xác thực (`AllowCredentials() = true` như Cookies, HTTP Authentication hoặc Bearer tokens), server TUYỆT ĐỐI KHÔNG ĐƯỢC trả về header `Access-Control-Allow-Origin: *`**.
   - Nếu cho phép `*` đi kèm Credentials, bất kỳ trang web độc hại nào cũng có thể gửi request ngầm từ trình duyệt của nạn nhân và tự động đính kèm cookie đăng nhập của họ (lỗ hổng nghiêm trọng dạng CSRF / Credential Leak).
2. **Hệ quả dây chuyền trên Cloud**:
   - Khi cấu hình biến môi trường `CORS_ALLOWED_ORIGINS="*"`, ASP.NET Core kiểm tra thấy có gọi `.AllowCredentials()` nên đã **chủ động dừng toàn bộ tiến trình ứng dụng ngay lúc boot**.
   - Do ứng dụng bị crash, cổng `8080` không bao giờ được mở.
   - Azure Container Apps thăm dò thấy cổng 8080 không phản hồi nên báo lỗi `App port is not ready`.

### Giải pháp khắc phục chuẩn 2 tầng (Defense-in-depth)

#### Tầng 1: Chuẩn hóa Biến Môi Trường (Environment Configuration)

Tuyệt đối không đặt `CORS_ALLOWED_ORIGINS="*"` trên môi trường Production. Luôn khai báo danh sách domain cụ thể phân tách bởi dấu phẩy:

```bash
CORS_ALLOWED_ORIGINS="http://localhost:5173,http://localhost:3000,https://tour-agent.vercel.app"
```

#### Tầng 2: Cơ Chế Tự Vệ Trong Mã Nguồn C# (`Program.cs`)

Thêm logic kiểm tra an toàn để ứng dụng không bao giờ bị crash nếu người vận hành vô tình cấu hình `*`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCorsPolicy", policy =>
    {
        var hasWildcard = allowedOrigins.Any(origin => origin == "*");
        if (hasWildcard)
        {
            // Nếu có wildcard "*", bắt buộc bỏ AllowCredentials() theo chuẩn W3C CORS
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Liệt kê domain cụ thể thì được phép bật AllowCredentials()
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});
```

---

## 3. Bài Học 3: Cấu Hình Declarative Bằng YAML Cho Env-Vars Phức Tạp & HTTP Health Probes

### Vấn đề 1: Tại sao truyền `--env-vars` qua CLI lúc tạo App bị mất, nhưng lúc update biến CORS lại được?
- **Nguyên nhân gốc rễ**: 
  - Chuỗi kết nối cơ sở dữ liệu `ConnectionStrings__DefaultConnection` chứa rất nhiều dấu chấm phẩy `;` và khoảng trắng: `Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`.
  - Cờ CLI `--env-vars` phân tách các biến bằng dấu cách (`space`). Khi bash và bộ phân tích cú pháp của Azure CLI xử lý chuỗi có chứa dấu `;` và dấu `=`, nó dễ bị lỗi phân tách từ (word-splitting / argument parsing), dẫn đến việc Azure CLI bỏ qua hoặc thất bại âm thầm khi nạp các biến môi trường lúc khởi tạo.
  - Ngược lại, biến `CORS_ALLOWED_ORIGINS="https://toidicopydao.io.vn"` là một chuỗi URL đơn giản, không có dấu chấm phẩy hay khoảng trắng, nên lệnh `update` ăn ngay.
- **Giải pháp chuẩn hóa (Best Practice)**:
  - Thay vì cố gắng escape chuỗi phức tạp trong dòng lệnh bash, hãy khai báo các biến môi trường dưới dạng mảng YAML tường minh (`template.containers[].env`). Bộ phân tích YAML xử lý chuỗi raw độc lập với shell arguments, đảm bảo nạp chính xác 100% mọi ký tự đặc biệt.

### Vấn đề 2: Tại sao Health Probes hiển thị Transport là TCP thay vì HTTP?
- **Nguyên nhân gốc rễ**:
  - Khi tách riêng bước cấu hình probe bằng một file YAML phụ chạy lệnh `az containerapp update --yaml probe-config.yaml`, Azure Container Apps trả về lỗi:
    ```text
    (ContainerAppImageRequired) Container with name 'touragent-api' must have an 'Image' property specified.
    ```
  - Vì lệnh update bị văng lỗi (exit), Azure **chưa từng áp dụng cấu hình probe mới**. Azure Container Apps vẫn giữ cấu hình mặc định ban đầu là kiểm tra cổng kết nối TCP (`tcpSocket: { port: 8080 }`).
- **Giải pháp chuẩn hóa (Best Practice)**:
  - **Gom toàn bộ vào 1 bước duy nhất (All-in-one YAML)**: Cấu hình `probes` với `httpGet` (`scheme: HTTP`, `path: /health/live`, `path: /health/ready`, `port: 8080`) được nhúng trực tiếp cùng với `image`, `resources`, và `env` ngay trong template của container app lúc tạo hoặc cập nhật.
  - Ngay khi áp dụng, Azure Container Apps sẽ chuyển Transport của cả 2 probes sang chuẩn **HTTP**.

---

## 4. Bài Học 4: Cạm Bẫy Đường Dẫn Tương Đối Phía Frontend & Bản Chất Của SPA Rewrite Rule (Tránh Lỗi 404)

### Vấn đề 1: Cạm bẫy của đường dẫn tương đối (`/api/...`) và ảo tưởng từ Vite Dev Server Proxy
* **Triệu chứng**:
  * Ở local, gọi API từ các trang Admin (`/admin/offers`, `/admin/products`,...) chạy bình thường.
  * Khi deploy lên Production (Vercel / Azure), trang chủ gọi được API (`https://touragent-api.../api/v1/tours`), nhưng trang Admin lại gọi vào `https://<domain-frontend>/api/v1/offers` và trả về mã 304 / nội dung HTML của `index.html`.
* **Nguyên nhân cốt lõi**:
  * Các trang Admin gọi trực tiếp `fetch('/api/v1/offers')` bằng đường dẫn tương đối.
  * Ở local, Vite dev server có cấu hình `server.proxy` tự động bắt các request bắt đầu bằng `/api` và bí mật chuyển tiếp tới `http://localhost:5132`. Lập trình viên tưởng rằng code đã chuẩn, nhưng thực chất đang phụ thuộc vào "cái nạng" của Vite proxy.
  * Ở Production (static file hosting), không còn Node.js hay Vite proxy nữa. Trình duyệt gửi request `/api/v1/offers` lên chính Web Server của frontend.
  * Thậm chí ở trang `AdminOrdersPage`, code bị hardcode cứng `const API_BASE_URL = 'http://localhost:5132'`, khiến người dùng bên ngoài không thể kết nối tới server.
* **Giải pháp chuẩn hóa (Single Source of Truth - SSOT)**:
  * Không dùng URL tương đối hay hardcode trong từng component.
  * Mọi request bắt buộc phải đi qua một client tập trung duy nhất (`src/api/client.ts` -> `apiFetch()` / `fetchJson()`), tự động gắn `VITE_API_BASE_URL` động theo môi trường (Local, Vercel, Azure Container Apps, AWS,...).

---

### Vấn đề 2: Tại sao route `/admin/products` tồn tại nhưng Web Server lại báo 404 nếu không có Rewrite về `index.html`?
* **Câu hỏi lớn**: *Route `/admin/products` trong code React vẫn chạy bình thường, tại sao cần cấu hình rewrite về `index.html`? Đây có phải là cách "chữa cháy" không?*
* **Bản chất kiến trúc**:
  * Đây **KHÔNG PHẢI** là cách chữa cháy. Đây là **nguyên lý vận hành nền tảng bắt buộc của mọi ứng dụng Single Page Application (SPA)** (React, Vue, Angular) trên toàn thế giới (từ Nginx `try_files`, Apache, Azure Static Web Apps đến Vercel).
* **Phân biệt Server-Side Routing vs. Client-Side Routing**:
  1. **Trong ổ cứng của Web Server (`dist/`)**:
     * Thư mục `dist/` sau khi build chỉ gồm: `index.html`, `assets/index.js`, `assets/index.css`.
     * **Hoàn toàn KHÔNG TỒN TẠI** thư mục vật lý nào tên là `/admin/` hay file `/admin/products.html` trên máy chủ!
  2. **Route `/admin/products` thực sự nằm ở đâu?**:
     * Nó **chỉ tồn tại trong bộ nhớ RAM của trình duyệt**, do thư viện JavaScript `React Router` quản lý (`<Route path="/admin/products" ... />`).
  3. **Hai kịch bản duyệt web**:
     * **Kịch bản 1 (Chuyển trang nội bộ)**: Khách vào trang chủ `/` (tải `index.html` và JS vào RAM). Khi click vào menu "Quản trị", React Router dùng HTML5 History API (`window.history.pushState`) đổi URL trên thanh địa chỉ thành `/admin/products` mà **không hề gửi request lên web server**. React tự tráo đổi giao diện trong RAM nên ta thấy trang đổi mượt mà.
     * **Kịch bản 2 (Gõ trực tiếp URL hoặc nhấn F5 Refresh)**: Trình duyệt gửi một request HTTP GET nguyên thủy lên Web Server: *"Cho tôi xin file tại đường dẫn `/admin/products`"*. Web Server tìm trên đĩa cứng không thấy file nào tên là `admin/products` nên theo chuẩn HTTP nó buộc phải trả về: **`404 Not Found`**!
  4. **Quy tắc Rewrite giải quyết bài toán này như thế nào?**:
     * Quy tắc `rewrite: "/(.*)" -> "/index.html"` chỉ thị cho Web Server: *"Nếu khách yêu cầu bất kỳ đường dẫn nào mà không phải là file tĩnh cụ thể, hãy cứ trả file `index.html` về cho trình duyệt"*.
     * Ngay khi trình duyệt nhận được `index.html`, nó sẽ khởi động mã JavaScript của React.
     * React Router khởi động trong RAM, đọc thanh địa chỉ (đang là `/admin/products`), và lập tức hiển thị đúng giao diện Admin Products cho người dùng!

---

## 5. Tổng Kết Mindset Vận Hành Cho Kỹ Sư DevOps / Fullstack

1. **Automation Over Manual**: Tránh phụ thuộc vào các bước bấm chuột trên giao diện Portal vì UI wizard thường phát sinh xung đột quyền giữa các tầng tài nguyên. Hãy luôn chuẩn hóa vào file script bash idempotent (`create-container-app.sh`).
2. **Đọc Log Từ Dưới Lên**: Khi gặp lỗi `App port is not ready`, đó chỉ là triệu chứng bên ngoài của Azure. Nguyên nhân gốc rễ luôn nằm ở dòng `crit: Microsoft.AspNetCore.Hosting.Diagnostics` trong console log của container.
3. **Bảo Mật CORS Không Bao Giờ Đi Tắt**: Wildcard `*` tiện cho dev local nhưng là cạm bẫy chết người khi kết hợp với xác thực người dùng trên Cloud.
4. **Hiểu Bản Chất SPA**: Client-side routing chỉ tồn tại trong JavaScript runtime. Web Server luôn cần cơ chế fallback/rewrite về `index.html` để giao quyền định tuyến lại cho React Router.

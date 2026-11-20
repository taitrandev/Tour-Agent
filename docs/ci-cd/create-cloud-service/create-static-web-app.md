# Hướng Dẫn Khởi Tạo & Triển Khai Azure Static Web Apps (Frontend)

Tài liệu này hướng dẫn chi tiết quy trình khởi tạo dịch vụ **Azure Static Web Apps (SWA - Gói Free)** cho ứng dụng `tour-agent-frontend` (React 19 + Vite), cấu hình GitHub Actions CI/CD và kết nối với Backend .NET 10 (`touragent-api`).

---

## 1. Tổng Quan Kiến Trúc & Thông Số Kỹ Thuật

* **Dịch vụ**: Azure Static Web Apps.
* **Gói cước (SKU)**: `Free` ($0/tháng, 100 GB bandwidth, SSL tự động, 2 custom domain).
* **Resource Group**: `rg-touragent-prod` (dùng chung với Backend ACA).
* **Tên ứng dụng đề xuất**: `swa-touragent-frontend`.
* **Vùng triển khai (Region)**: `southeastasia` (Đông Nam Á - Singapore, cùng region với Resource Group và Backend).
* **Cơ chế xác thực CI/CD**: **Deployment Token** (`AZURE_STATIC_WEB_APPS_API_TOKEN`) lưu trong GitHub Secret.

> [!IMPORTANT]
> **2 Nguyên Tắc Sống Còn Cần Nhớ:**
>
> 1. **Biến môi trường Vite (`VITE_API_BASE_URL`) chỉ có tác dụng lúc BUILD**: Trình duyệt không đọc biến môi trường lúc runtime. Biến này bắt buộc phải được truyền vào lệnh `npm run build` trên GitHub Actions runner.
> 2. **Chọn Deployment Source là "Other"**: Khi tạo SWA, **không** chọn GitHub để tránh Azure tự động sinh một workflow lạ vào repo gây xung đột với pipeline tùy biến của chúng ta.

---

## 2. Cách Khởi Tạo Azure Static Web Apps

Bạn có thể chọn **Cách 1 (Azure CLI - Khuyến nghị)** hoặc **Cách 2 (Azure Portal)**.

### Cách 1: Khởi tạo nhanh bằng Azure CLI (Khuyến nghị 80/20)

Mở **Azure Cloud Shell** (hoặc terminal đã login Azure) và chạy các lệnh sau:

```bash
# 1. Định nghĩa thông số (Sử dụng southeastasia cùng region với Resource Group)
RG="rg-touragent-prod"
LOCATION="southeastasia"
SWA_NAME="swa-touragent-frontend"

# 2. Tạo Static Web App gói Free (không liên kết source tự động)
az staticwebapp create \
  --name "$SWA_NAME" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --sku Free

# 3. Lấy Deployment Token để nạp vào GitHub Secrets
echo "==> DEPLOYMENT TOKEN (Copy giá trị này nạp vào GitHub Secrets):"
az staticwebapp secrets list \
  --name "$SWA_NAME" \
  --resource-group "$RG" \
  --query "properties.apiKey" -o tsv

# 4. Lấy URL mặc định của Frontend SWA
echo "==> DEFAULT HOSTNAME (Domain frontend của bạn):"
az staticwebapp show \
  --name "$SWA_NAME" \
  --resource-group "$RG" \
  --query "defaultHostname" -o tsv
```

---

### Cách 2: Khởi tạo thủ công trên Azure Portal

1. Truy cập **Azure Portal** ➔ Tìm kiếm thanh công cụ trên cùng: **Static Web Apps** ➔ Bấm **+ Create**.
2. **Tab Basics**:

   * **Subscription**: Chọn Subscription của bạn (*Azure for Students*).
   * **Resource Group**: Chọn `rg-touragent-prod`.
   * **Name**: `swa-touragent-frontend`.
   * **Plan type**: Chọn **Free**.
   * **Region**: Chọn **Southeast Asia** (hoặc East Asia nếu subscription cho phép).
   * **Deployment details**:
     > [!WARNING]
     > Ở mục **Source**, chọn **Other**! (Tuyệt đối **không** bấm nút GitHub ở bước này để tránh Azure tự sinh file workflow rác).
     >
3. Bấm **Review + create** ➔ Bấm **Create**.
4. Sau khi tạo xong, bấm **Go to resource**:

   * Ở thanh menu trên cùng của trang **Overview**, bấm vào **Manage deployment token**.
   * Copy chuỗi Deployment token xuất hiện.

---

## 3. Cấu Hình GitHub Repository Secrets & Variables

Vào GitHub Repository của bạn: **Settings** ➔ **Secrets and variables** ➔ **Actions**.

### 3.1. Thêm Secret (Deployment Token)

* Tab **Secrets** ➔ Bấm **New repository secret**:
  * **Name**: `AZURE_STATIC_WEB_APPS_API_TOKEN`
  * **Secret**: Dán chuỗi Deployment token bạn vừa copy ở Bước 2.

### 3.2. Thêm Variable (URL Backend API)

* Tab **Variables** ➔ Bấm **New repository variable**:
  * **Name**: `VITE_API_BASE_URL`
  * **Value**: `https://<fqdn-backend-aca>/api/v1`
    *(Ví dụ: `https://touragent-api.politestone-c266986a.southeastasia.azurecontainerapps.io/api/v1`)*

---

## 4. Cấu Hình SPA Routing (`staticwebapp.config.json`)

File cấu hình này đã được tạo tại `tour-agent-frontend/public/staticwebapp.config.json`. Khi chạy `npm run build`, Vite sẽ tự động copy file này vào thư mục `dist/`.

Nội dung file:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": [
      "/assets/*",
      "*.{css,js,png,svg,ico,woff2}"
    ]
  }
}
```

* **Tác dụng**: Bắt mọi request định tuyến sâu (như `/tour/ha-long`, `/admin/products`) chuyển hướng về `/index.html` với HTTP 200 để React Router v7 xử lý, tránh lỗi 404 khi người dùng F5 tải lại trang.
* **Mục `exclude`**: Đảm bảo các file tĩnh như CSS, JS nếu bị thiếu sẽ trả về 404 thực sự, không rewrite nhầm về `index.html`.

---

## 5. Workflow Tự Động Hóa CI/CD (`frontend-ci.yml`)

Tạo file `.github/workflows/frontend-ci.yml` trong repo:

```yaml
name: Frontend CI/CD - Azure Static Web Apps

on:
  push:
    branches: [ main ]
    paths:
      - 'tour-agent-frontend/**'
      - '.github/workflows/frontend-ci.yml'

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    name: Build & Deploy Frontend
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Setup Node.js 22.x
        uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: 'npm'
          cache-dependency-path: tour-agent-frontend/package-lock.json

      - name: Install Dependencies
        working-directory: tour-agent-frontend
        run: npm ci

      - name: Build SPA With Production API URL
        working-directory: tour-agent-frontend
        env:
          VITE_API_BASE_URL: ${{ vars.VITE_API_BASE_URL }}
        run: npm run build

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "tour-agent-frontend/dist"  # Đẩy trực tiếp thư mục dist đã build
          output_location: ""                      # Bỏ trống vì app_location đã trỏ thẳng vào dist
          skip_app_build: true                     # Bỏ qua trình build Oryx của Azure để tự kiểm soát
```

> [!TIP]
> **Tại sao không deploy môi trường Preview khi tạo PR trong giai đoạn MVP?**
> Mỗi Pull Request preview trên SWA sẽ có một subdomain ngẫu nhiên (ví dụ `https://lemon-island-123-1.eastasia.azurestaticapps.net`). Do Backend ASP.NET Core giới hạn CORS nghiêm ngặt theo domain cụ thể (`WithOrigins`), các trang preview sẽ bị Backend chặn CORS. Để chuẩn 80/20, giai đoạn MVP chỉ kích hoạt deploy khi code được merge vào nhánh `main`.

---

## 6. Cập Nhật CORS Trên Backend `touragent-api`

Sau khi SWA được tạo, bạn lấy hostname của SWA (ví dụ `https://calm-sea-012345.eastasia.azurestaticapps.net`) và bổ sung vào danh sách `CORS_ALLOWED_ORIGINS` của Backend Container App:

```bash
# Thay thế domain SWA thực tế của bạn vào chuỗi bên dưới
az containerapp update \
  --name touragent-api \
  --resource-group rg-touragent-prod \
  --set-env-vars CORS_ALLOWED_ORIGINS="https://toidicopydao.io.vn,https://<ten-swa-cua-ban>.azurestaticapps.net"
```

---

## 7. Checklist Nghiệm Thu Sau Khi Triển Khai

1. **Kiểm tra SPA Routing**:
   * Truy cập thẳng vào một deep link: `https://<ten-swa>.azurestaticapps.net/admin/products`.
   * Nhấn **F5** tải lại trang ➔ Trang vẫn hiển thị đúng, **không** bị lỗi HTTP 404.
2. **Kiểm tra Kết Nối API & Chat AI**:
   * Mở F12 (DevTools) tab **Network**.
   * Gửi một tin nhắn tư vấn tour cho AI Agent Sakura ➔ Kiểm tra request gửi tới `https://touragent-api.../api/v1/agent/chat/stream`.
   * Request trả về HTTP 200, phản hồi dạng stream `text/event-stream` (SSE), không có lỗi CORS màu đỏ.

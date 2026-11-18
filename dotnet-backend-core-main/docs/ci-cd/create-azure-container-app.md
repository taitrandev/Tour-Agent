# Hướng Dẫn Khởi Tạo Azure Container Apps (Day-0 Provisioning) Cho TourAgent API

Tài liệu này hướng dẫn chi tiết từng bước tạo và cấu hình **Azure Container Apps (ACA)** cho dịch vụ backend `TourAgent.Api` (.NET 10), kết nối với Azure Container Registry (ACR), Database Supabase PostgreSQL, và thiết lập Health Probes sẵn sàng cho pipeline Continuous Deployment (CD).

---

## 1. Bản Chất Kỹ Thuật & Kiến Trúc Day-0

```
[ Azure Resource Group (rg-touragent-prod) ]
│
├── [ Azure Container Registry: touragentacr ]
│       ▲
│       │ (Pull Image qua role AcrPull)
│       │
├── [ Container Apps Managed Environment: cae-touragent-prod ]
│       │ (Quản lý mạng ảo & Log Analytics workspace)
│       │
└── [ Azure Container App: touragent-api ]
        ├─ Ingress: HTTP/HTTPS công khai, Target Port: 8080
        ├─ Identity: Managed Identity có quyền AcrPull
        ├─ Environment Variables:
        │     - ConnectionStrings__DefaultConnection (Supabase PostgreSQL)
        │     - DeepSeek__ApiKey (AI API Key)
        │     - ASPNETCORE_ENVIRONMENT=Production
        ├─ Probes:
        │     - Liveness Probe:  Path /health/live  (Cổng 8080)
        │     - Readiness Probe: Path /health/ready (Cổng 8080)
        └─ Scaling: 0 đến 1 replica (Scale-to-zero khi không dùng để tiết kiệm chi phí)
```

### Tại sao cần Day-0 Provisioning trước khi chạy CD?
- Lệnh `az containerapp update` trong CD pipeline **chỉ làm nhiệm vụ cập nhật tag image mới** (`az containerapp update --image ...`). Lệnh này sẽ báo lỗi nếu Container App chưa tồn tại.
- Bước Day-0 này chỉ cần thực hiện **1 LẦN DUY NHẤT** để dựng bộ khung hạ tầng ban đầu.

---

## 2. Thông Số Chuẩn Bị Cho TourAgent

Hãy kiểm tra các thông số sau của dự án trước khi tạo:

| Thông số | Giá trị khuyến nghị | Ý nghĩa |
| :--- | :--- | :--- |
| **Resource Group** | `rg-touragent-prod` | Chứa toàn bộ tài nguyên của TourAgent |
| **Region** | `Southeast Asia` (hoặc East Asia) | Cùng region với ACR và gần Supabase Singapore |
| **Container Apps Environment** | `cae-touragent-prod` | Môi trường cụm container ảo |
| **Container App Name** | `touragent-api` | Tên dịch vụ backend |
| **ACR Name** | `touragentacr` | Registry đã chứa image `touragent-api` |
| **Target Port** | `8080` | Port non-root cấu hình trong `Dockerfile` |
| **CPU / Memory** | `0.25 vCPU` / `0.5 GiB` | Gói tối thiểu (đủ cho MVP, chi phí gần như 0đ) |

---

## 3. Các Bước Thực Hiện Chi Tiết

Bạn có thể làm theo **Cách A (Giao diện Azure Portal trực quan)** hoặc **Cách B (Azure CLI / Cloud Shell nhanh)**.

---

### Cách A: Thao Tác Trực Quan Trên Azure Portal

#### Bước A1: Truy cập trang tạo Container App
1. Đăng nhập [Azure Portal](https://portal.azure.com/).
2. Trên thanh tìm kiếm, gõ `Container Apps` ➔ bấm vào dịch vụ **Container Apps**.
3. Bấm **+ Create** ➔ chọn **Container App**.

#### Bước A2: Cấu hình Tab "Basics"
- **Subscription**: Chọn Subscription của bạn.
- **Resource group**: Chọn `rg-touragent-prod` (nơi chứa ACR).
- **Container app name**: Nhập `touragent-api`.
- **Region**: Chọn vùng cùng với ACR (ví dụ `Southeast Asia`).
- **Container Apps Environment**:
  - Nếu chưa có: Bấm **Create new**.
  - Đặt tên Environment: `cae-touragent-prod`.
  - Mục Zone redundancy: Chọn **Disabled** (tiết kiệm chi phí).
  - Bấm **Create**.
- Bấm **Next: Container >**.

#### Bước A3: Cấu hình Tab "Container"
1. **Uncheck** ô *Use quickstart image* (để dùng image của riêng ta).
2. **Name**: `touragent-api`.
3. **Image source**: Chọn **Azure Container Registry**.
   - **Registry**: Chọn `touragentacr`.
   - **Image**: Chọn `touragent-api`.
   - **Image tag**: Chọn tag SHA mới nhất (hoặc bất kỳ tag nào đã push).
4. **Development stack-specific features**:
   - Mục **Development stack**: Chọn **`.NET`** (Azure sẽ tự động bật các tối ưu hóa runtime và giám sát telemetry đặc thù cho container chạy .NET).
5. **Container resource allocation**:
   - CPU: `0.25 vCPU`.
   - Memory: `0.5 Gi`.
5. **Environment variables (Biến môi trường)**:
   Bấm **+ Add** thêm các biến sau:
   - `ConnectionStrings__DefaultConnection` (Manual entry) = `<Chuỗi Supabase PostgreSQL của bạn>`
   - `DeepSeek__ApiKey` (Manual entry / Secret) = `<API Key DeepSeek của bạn>`
   - `ASPNETCORE_ENVIRONMENT` (Manual entry) = `Production`
   - `CORS_ALLOWED_ORIGINS` (Manual entry) = `*` (hoặc link frontend sau này)
6. Bấm **Next: Ingress >**.

#### Bước A4: Cấu hình Tab "Ingress" (Cực kỳ quan trọng)
1. **Ingress**: Tick chọn **Enabled**.
2. **Ingress traffic**: Chọn **Accepting traffic from anywhere** (External HTTP/HTTPS công khai).
3. **Target port**: Nhập chính xác **`8080`** (vì container .NET 10 chạy non-root ở port 8080).
4. Bấm **Review + create** ➔ Bấm **Create** (chờ khoảng 1-2 phút).

#### Bước A5: Cấu hình Health Probes (Sau khi App tạo xong)
Để Azure Container Apps tự động nhận diện container khỏe mạnh trước khi định tuyến traffic (Zero-Downtime):
1. Vào Container App `touragent-api` vừa tạo.
2. Menu trái mục **Application** ➔ chọn **Containers**.
3. Bấm vào container `touragent-api` ➔ chọn tab **Health probes**.
4. **Thêm Liveness Probe**:
   - Bấm **+ Add liveness probe**.
   - Protocol: `HTTP`
   - Path: `/health/live`
   - Port: `8080`
   - Initial delay: `10` seconds.
   - Period: `10` seconds.
   - Failure threshold: `3`.
5. **Thêm Readiness Probe**:
   - Bấm **+ Add readiness probe**.
   - Protocol: `HTTP`
   - Path: `/health/ready`
   - Port: `8080`
   - Initial delay: `10` seconds.
   - Period: `10` seconds.
   - Failure threshold: `3`.
6. Bấm **Save**.

#### Bước A6: Gán Quyền AcrPull (Nếu dùng Managed Identity riêng)
1. Mở trang Container App ➔ Menu **Settings** ➔ **Identity**.
2. Tab **User assigned**: Bấm **+ Add** ➔ chọn `id-github-touragent-deploy` (hoặc identity bạn dùng) ➔ **Add**.
3. Đảm bảo identity này đã có quyền `AcrPull` trên ACR `touragentacr`.

---

### Cách B: Thao Tác Bằng Azure CLI / Cloud Shell (Copy-Paste 1 Lần)

Nếu bạn mở Azure Cloud Shell (Bash) hoặc terminal có cài Azure CLI, bạn chỉ cần thay giá trị thật vào và chạy:

```bash
# 1. Khai báo biến
RG="rg-touragent-prod"
LOCATION="southeastasia"
ACR_NAME="touragentacr"
APP_NAME="touragent-api"
ENV_NAME="cae-touragent-prod"
IDENTITY_NAME="id-github-touragent-deploy"

# Thay thế bằng chuỗi kết nối và key thật của bạn
SUPABASE_CONN="Host=aws-1-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.wdsfssenfbesfmevifkf;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
DEEPSEEK_KEY="sk-your-deepseek-api-key"

# 2. Tạo Container Apps Environment
az containerapp env create \
  --name $ENV_NAME \
  --resource-group $RG \
  --location $LOCATION

# 3. Lấy Resource ID của Managed Identity
IDENTITY_ID=$(az identity show -g $RG -n $IDENTITY_NAME --query id -o tsv)
IDENTITY_PRINCIPAL=$(az identity show -g $RG -n $IDENTITY_NAME --query principalId -o tsv)
ACR_ID=$(az acr show -n $ACR_NAME -g $RG --query id -o tsv)

# 4. Gán quyền AcrPull cho Identity trên ACR (nếu chưa có)
az role assignment create \
  --assignee-object-id $IDENTITY_PRINCIPAL \
  --assignee-principal-type ServicePrincipal \
  --role AcrPull \
  --scope $ACR_ID

# 5. Khởi tạo Container App với đầy đủ Port 8080, Ingress, Env Vars và Image đầu tiên
LATEST_IMAGE=$(az acr repository show-tags -n $ACR_NAME --repository touragent-api --orderby time_desc --top 1 -o tsv)

az containerapp create \
  --name $APP_NAME \
  --resource-group $RG \
  --environment $ENV_NAME \
  --image "${ACR_NAME}.azurecr.io/touragent-api:${LATEST_IMAGE}" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 1 \
  --user-assigned $IDENTITY_ID \
  --registry-server "${ACR_NAME}.azurecr.io" \
  --registry-identity $IDENTITY_ID \
  --env-vars \
    ConnectionStrings__DefaultConnection="$SUPABASE_CONN" \
    DeepSeek__ApiKey="$DEEPSEEK_KEY" \
    ASPNETCORE_ENVIRONMENT="Production" \
    CORS_ALLOWED_ORIGINS="*"
```

---

## 4. Bổ Sung Biến Vào GitHub Repository Variables

Để pipeline GitHub Actions có thể ra lệnh cập nhật Container App ở bước CD, bạn cần nạp thêm 2 biến sau vào GitHub Repository:

👉 Truy cập: `https://github.com/taitrandev/Tour-Agent/settings/variables/actions`
*(Settings ➔ Secrets and variables ➔ Actions ➔ Tab **Variables**)*

| Tên Variable | Giá trị mẫu | Ý nghĩa |
| :--- | :--- | :--- |
| **`ACA_NAME`** | `touragent-api` | Tên của Azure Container App |
| **`AZURE_RG`** | `rg-touragent-prod` | Tên của Resource Group chứa Container App |

*(Các biến `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME` đã có từ Phase 1).*

---

## 5. Kiểm Thử Nhanh Sau Khi Khởi Tạo (Smoke Test Thủ Công)

1. Sau khi tạo xong, mở Azure Container App ➔ mục **Overview** ➔ sao chép đường link **Application Url** (chính là FQDN, ví dụ: `https://touragent-api.agreeablecliff-xxxx.southeastasia.azurecontainerapps.io`).
2. Mở trình duyệt hoặc curl kiểm tra:
   - `https://<fqdn>/health/live` ➔ Trả về: `{"status":"Healthy"}` (Process ASP.NET Core sống).
   - `https://<fqdn>/health/ready` ➔ Trả về: `{"status":"Healthy"}` (PostgreSQL Supabase kết nối thành công 100%!).
   - `https://<fqdn>/api/offers` ➔ Trả về danh sách offer mẫu đã được Seeder tự động nạp vào Supabase!


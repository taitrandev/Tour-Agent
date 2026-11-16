# Hướng Dẫn Cấu Hình OIDC & User-Assigned Managed Identity Kết Nối GitHub Actions với Azure

Tài liệu này hướng dẫn chi tiết cách thiết lập xác thực không cần mật khẩu (**Workload Identity Federation / OIDC**) giữa GitHub Actions và Microsoft Entra ID (Azure AD), sử dụng **User-Assigned Managed Identity**.

---

## 1. Bản Chất Kỹ Thuật & Kiến Trúc

### 1.1. OIDC và Managed Identity là gì?

- **User-Assigned Managed Identity (UAMI)**: Là một tài khoản định danh ("robot account") miễn phí trong Microsoft Entra ID, độc lập với vòng đời của bất kỳ máy chủ nào. Nó có `Client ID` và `Principal ID`, nhưng **không có mật khẩu**. Bạn gán các quyền Azure RBAC (như `AcrPush`, `Contributor`) trực tiếp cho identity này.
- **OIDC (OpenID Connect / Federated Credential)**: Không phải là một dịch vụ tính phí mà là **giao thức tin cậy**. Nó cho phép Entra ID tin tưởng các token bảo mật do GitHub Actions cấp phát, miễn là token đó khớp với repository và nhánh quy định (`repo:taitrandev/Tour-Agent:ref:refs/heads/main`).

> [!NOTE]
> Bạn chỉ cần tạo **DUY NHẤT 1 Resource trên Azure** là **User-Assigned Managed Identity**, sau đó khai báo 1 dòng Federated Credential trên identity đó. Không có chi phí phát sinh.

### 1.2. Sơ đồ luồng xác thực (Authentication Flow)

```
[ GitHub Actions Runner ]
       │
       │ (1) Yêu cầu OIDC Token (JWT) từ GitHub Provider
       ▼
[ GitHub OIDC Token Service ]
       │  (Issuer: https://token.actions.githubusercontent.com)
       │  (Subject: repo:taitrandev/Tour-Agent:ref:refs/heads/main)
       ▼
[ GitHub Actions Runner ]
       │
       │ (2) Gửi JWT qua action azure/login@v2
       ▼
[ Microsoft Entra ID (Azure AD) ]
       │
       │ (3) Đối chiếu chữ ký GitHub & Federated Credential của Managed Identity
       │     -> Nếu trùng khớp: Cấp Azure Access Token (thời hạn ~1 giờ)
       ▼
[ GitHub Actions Runner ]
       │
       ├─► (4) az acr login & docker push (Dùng quyền AcrPush trên ACR)
       └─► (5) az containerapp update (Dùng quyền Container Apps Contributor trên Resource Group)
```

### 1.3. Bảng so sánh phương thức xác thực

| Tiêu chí                                | ACR Admin User / Password               | Service Principal + Secret           | OIDC + User-Assigned Managed Identity                         |
| :---------------------------------------- | :-------------------------------------- | :----------------------------------- | :------------------------------------------------------------ |
| **Lưu trữ bí mật trên GitHub** | Lưu username & password dài hạn      | Lưu Client Secret dài hạn         | **Zero Stored Secrets** (Chỉ lưu 3 ID công khai)     |
| **Rủi ro rò rỉ secret**          | Cao (nếu leak là truy cập mãi mãi) | Cao (nếu quên set expiry/rotation) | **Gần như bằng 0** (Token sống ~1h, tự hủy)       |
| **Phạm vi sử dụng**              | Chỉ dùng để push/pull ACR           | Dùng được nhiều nơi            | **Dùng được cho cả CI (push ACR) và CD (deploy)** |
| **Chuẩn công nghiệp**            | Dành cho test nhanh cá nhân          | Cách truyền thống cũ             | **Chuẩn Best Practice Enterprise & Cloud-Native**      |

---

## 2. Thông Số Quy Hoạch Của Dự Án

Hãy chuẩn bị hoặc xác nhận các thông số sau trên Azure của bạn trước khi thực hiện:

- **Resource Group**: `rg-touragent-prod` (hoặc tên RG chứa ACR của bạn)
- **Azure Container Registry (ACR)**: Ví dụ `acrtouragent` (hoặc tên bạn đã tạo)
- **Tên Managed Identity dự kiến**: `id-github-touragent-deploy`
- **GitHub Repository**: `taitrandev/Tour-Agent`
- **Nhánh triển khai chính**: `main`

---

## 3. Các Bước Thực Hiện

Bạn có thể chọn làm theo **Cách A (Giao diện Azure Portal)** hoặc **Cách B (Azure CLI / Cloud Shell)**.

### Cách A: Thao Tác Trực Quan Trên Azure Portal

#### Bước A1: Tạo User-Assigned Managed Identity

1. Đăng nhập [Azure Portal](https://portal.azure.com/).
2. Trên thanh tìm kiếm, gõ `Managed Identities` và chọn dịch vụ này.
3. Nhấn **+ Create**.
4. Điền các trường:
   - **Subscription**: Chọn Subscription của bạn.
   - **Resource group**: Chọn Resource Group chứa ACR (ví dụ `rg-touragent-prod`).
   - **Region**: Chọn vùng cùng với ACR (ví dụ `Southeast Asia`).
   - **Name**: Nhập `id-github-touragent-deploy`.
5. Nhấn **Review + create** -> **Create**.

#### Bước A2: Cấu hình Federated Credential (OIDC)

1. Sau khi tạo xong, bấm **Go to resource** (vào Managed Identity vừa tạo).
2. Ở menu bên trái, mục **Settings** -> chọn **Federated credentials**.
3. Bấm **+ Add credential**.
4. Trong ô **Federated credential scenario**, chọn dropdown: **GitHub Actions deploying Azure resources**.
5. Điền chính xác các thông tin sau:
   - **Organization**: `taitrandev` (tên tài khoản GitHub của bạn).
   - **Organization ID**: view page source và tìm user_id
   - **Repository**: `Tour-Agent` (tên repo GitHub).
   - **Repository ID**: view page source và tìm repository_id
   - **Entity type**: Chọn **Branch**.
   - **GitHub branch name**: `main`.
   - **Name**: `github-actions-main` (tên gợi nhớ cho credential này).
6. Nhấn **Add**.

#### Bước A3: Gán Quyền (RBAC Role Assignment)

Identity này cần 2 quyền:

- Quyền 1: `AcrPush` trên Azure Container Registry (để CI push image).
- Quyền 2: `Container Apps Contributor` trên Resource Group (chuẩn **Least Privilege** — chỉ cho phép quản lý và cập nhật Container Apps, không can thiệp trái phép vào các dịch vụ khác như Database hay Network).

**Gán quyền AcrPush trên ACR:**

1. Mở trang dịch vụ **Container registries** -> bấm vào registry của bạn (ví dụ `acrtouragent`).
2. Chọn menu **Access control (IAM)** -> bấm **+ Add** -> chọn **Add role assignment**.
3. Tại tab **Role**, tìm kiếm và chọn `AcrPush` -> bấm **Next**.
4. Tại tab **Members**:
   - Chọn **Assign access to**: `Managed identity`.
   - Bấm **+ Select members**.
   - Ở mục Managed identity, chọn `User-assigned managed identity`.
   - Chọn `id-github-touragent-deploy` vừa tạo -> bấm **Select**.
5. Bấm **Review + assign** -> **Review + assign**.

**Gán quyền Container Apps Contributor trên Resource Group:**

1. Mở trang **Resource groups** -> bấm vào Resource Group của bạn (ví dụ `rg-touragent-prod`).
2. Chọn **Access control (IAM)** -> **+ Add** -> **Add role assignment**.
3. Tab **Role**: Tìm kiếm và chọn **`Container Apps Contributor`** -> bấm **Next**.
4. Tab **Members**: Chọn `Managed identity` -> chọn `id-github-touragent-deploy`.
5. Bấm **Review + assign** -> **Review + assign**.

---

### Cách B: Thao Tác Nhanh Bằng Azure CLI / Cloud Shell

Nếu bạn mở Azure Cloud Shell (Bash) trên Portal hoặc dùng Azure CLI máy cá nhân, chỉ cần chạy các lệnh sau:

```bash
# 1. Khai báo các biến thông số
RG_NAME="rg-touragent-prod"
ACR_NAME="acrtouragent"           # Tên ACR bạn đã tạo
IDENTITY_NAME="id-github-touragent-deploy"
REPO="taitrandev/Tour-Agent"
BRANCH="main"

# 2. Tạo User-Assigned Managed Identity
az identity create \
  --name $IDENTITY_NAME \
  --resource-group $RG_NAME

# 3. Tạo Federated Credential kết nối GitHub Actions nhánh main
az identity federated-credential create \
  --identity-name $IDENTITY_NAME \
  --resource-group $RG_NAME \
  --name "github-actions-main" \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:${REPO}:ref:refs/heads/${BRANCH}" \
  --audiences "api://AzureADTokenExchange"

# 4. Lấy ID cần thiết
PRINCIPAL_ID=$(az identity show --name $IDENTITY_NAME --resource-group $RG_NAME --query principalId -o tsv)
ACR_ID=$(az acr show --name $ACR_NAME --resource-group $RG_NAME --query id -o tsv)
RG_ID=$(az group show --name $RG_NAME --query id -o tsv)

# 5. Gán Role AcrPush trên ACR
az role assignment create \
  --assignee-object-id $PRINCIPAL_ID \
  --assignee-principal-type ServicePrincipal \
  --role AcrPush \
  --scope $ACR_ID

# 6. Gán Role Container Apps Contributor trên Resource Group (Least Privilege)
az role assignment create \
  --assignee-object-id $PRINCIPAL_ID \
  --assignee-principal-type ServicePrincipal \
  --role "Container Apps Contributor" \
  --scope $RG_ID
```

---

## 4. Lấy 3 Thông Số Cần Thiết & Cấu Hình Trên GitHub

### 4.1. Lấy thông số từ Azure

Bạn cần lấy đúng 3 giá trị định danh sau (chúng là thông tin công khai, không phải mật khẩu):

1. **Client ID** của Managed Identity:
   - Trên Portal: Mở `id-github-touragent-deploy` -> xem trường `Client ID`.
   - Bằng CLI: `az identity show -g $RG_NAME -n $IDENTITY_NAME --query clientId -o tsv`
2. **Tenant ID** của Azure AD:
   - Trên Portal: Tìm kiếm `Microsoft Entra ID` -> xem trường `Tenant ID`.
   - Bằng CLI: `az account show --query tenantId -o tsv`
3. **Subscription ID**:
   - Trên Portal: Xem trong phần `Subscriptions`.
   - Bằng CLI: `az account show --query id -o tsv`

### 4.2. Cấu hình vào GitHub Repository Variables

Vì OIDC không dùng password, bạn lưu các thông số này vào **Repository Variables** (không bắt buộc phải giấu trong Secrets):

1. Truy cập repo GitHub: `https://github.com/taitrandev/Tour-Agent/settings/variables/actions`
   *(Settings -> Secrets and variables -> Actions -> Tab **Variables**)*
2. Bấm **New repository variable** và thêm lần lượt:
   - Name: `AZURE_CLIENT_ID` -> Value: `<Client ID>`
   - Name: `AZURE_TENANT_ID` -> Value: `<Tenant ID>`
   - Name: `AZURE_SUBSCRIPTION_ID` -> Value: `<Subscription ID>`
   - Name: `ACR_NAME` -> Value: `<Tên ACR của bạn, ví dụ: acrtouragent>`

---

## 5. Mẫu GitHub Actions Workflow Sử Dụng OIDC

File workflow `.github/workflows/backend-ci.yml` sẽ sử dụng cấu hình như sau:

```yaml
name: Backend CI & Artifact Build

on:
  push:
    branches: [ main ]
    paths:
      - 'dotnet-backend-core-main/**'
      - '.github/workflows/backend-ci.yml'
  pull_request:
    branches: [ main ]
    paths:
      - 'dotnet-backend-core-main/**'

# Yêu cầu bắt buộc để GitHub cấp token OIDC
permissions:
  id-token: write  # Bắt buộc để xin JWT từ GitHub OIDC provider
  contents: read   # Đọc source code

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore dotnet-backend-core-main/TourAgent.sln

      - name: Build
        run: dotnet build dotnet-backend-core-main/TourAgent.sln -c Release --no-restore

      - name: Test
        run: dotnet test dotnet-backend-core-main/TourAgent.sln --no-build -c Release --verbosity normal

  docker-build-and-push:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      # Nếu chỉ là Pull Request: Chỉ test compile Dockerfile, không login Azure, không push image
      - name: Build Docker Image (PR Validation Only)
        if: github.event_name == 'pull_request'
        uses: docker/build-push-action@v6
        with:
          context: ./dotnet-backend-core-main
          file: ./dotnet-backend-core-main/Dockerfile
          push: false
          tags: touragent-api:test

      # Nếu là Push nhánh main: Xác thực OIDC với Azure và Push Image
      - name: Azure Login via OIDC
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - name: Login to Azure Container Registry
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        run: az acr login --name ${{ vars.ACR_NAME }}

      - name: Build and Push Docker Image to ACR
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        uses: docker/build-push-action@v6
        with:
          context: ./dotnet-backend-core-main
          file: ./dotnet-backend-core-main/Dockerfile
          push: true
          tags: |
            ${{ vars.ACR_NAME }}.azurecr.io/touragent-api:${{ github.sha }}
            ${{ vars.ACR_NAME }}.azurecr.io/touragent-api:latest
```

---

## 6. Các Điểm Cần Lưu Ý & Khắc Phục Lỗi (Failure Modes)

1. **Lỗi `unable to get ACTIONS_ID_TOKEN_REQUEST_URL`**:
   - *Nguyên nhân*: Quên khai báo `permissions: id-token: write` ở cấp job hoặc workflow.
   - *Khắc phục*: Thêm quyền `id-token: write` vào file YAML.
2. **Lỗi `AADSTS70021: No matching federated identity record found`**:
   - *Nguyên nhân*: Subject trong Federated Credential không khớp với context chạy của GitHub runner.
   - *Khắc phục*: Đảm bảo Subject chính xác là:
     `repo:taitrandev/Tour-Agent:ref:refs/heads/main`
     *(Lưu ý phân biệt chữ hoa/thường: `Tour-Agent` vs `tour-agent`)*.
3. **Lỗi 403 Forbidden khi `az acr login` hoặc `docker push` ngay sau khi tạo role**:
   - *Nguyên nhân*: Azure RBAC mất khoảng 1 đến 3 phút để đồng bộ (propagation delay).
   - *Khắc phục*: Chờ 2 phút rồi kích hoạt lại workflow.
4. **Phân biệt hai Managed Identity khác nhau**:
   - `id-github-touragent-deploy` (User-Assigned): Dùng cho **CI/CD pipeline** (cần quyền `AcrPush` và `Contributor`).
   - Container App Identity (System-Assigned): Dùng cho **máy chủ runtime** pull image từ ACR (chỉ cần quyền `AcrPull`). Tách bạch hai identity này giúp tuân thủ nguyên tắc đặc quyền tối thiểu (Least Privilege).

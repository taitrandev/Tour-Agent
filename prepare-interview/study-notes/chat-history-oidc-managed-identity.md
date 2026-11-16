
## Kết luận

**Không. Dùng OIDC với User-Assigned Managed Identity.** ACR token giải quyết được *một nửa* bài toán (push image) nhưng bước CD (`az containerapp update`) vẫn cần đăng nhập Azure — bạn sẽ phải làm OIDC/Service Principal nữa trong tuần sau. Làm 2 cơ chế thay vì 1 không phải 80/20.

Giả định của bạn cần sửa: "không cần tạo thêm cloud service". App Registration và Managed Identity **không phải dịch vụ tính phí** — chúng là object định danh trong Entra ID, miễn phí. Thứ bạn né không tốn tiền, chỉ tốn ~5 lệnh.

---

## So sánh cho đúng bài toán của bạn (push ACR **và** deploy Container App từ GitHub Actions)

|                               | ACR token                                | OIDC + Managed Identity                                                               |
| ----------------------------- | ---------------------------------------- | ------------------------------------------------------------------------------------- |
| Push image                    | Có                                      | Có (role `AcrPush`)                                                                |
| `az containerapp update`    | **Không** — token chỉ biết ACR | Có (role `Contributor` trên RG)                                                   |
| Secret lưu ở GitHub         | Password dài hạn, phải xoay           | **Không có secret** — chỉ 3 ID công khai                                   |
| Hết hạn / rotation          | Có, bạn phải nhớ                     | Token sống vài phút, tự cấp                                                      |
| Số cơ chế auth phải hiểu | 2 (token + cái gì đó cho CD)         | 1                                                                                     |
| Câu chuyện phỏng vấn      | "Dùng token"                            | "Workload identity federation, zero stored secrets" — đúng cái JD gọi là DevOps |

ACR token hợp lý khi bạn **chỉ** cần push (ví dụ bên thứ 3 đẩy image vào registry của bạn), đúng như bảng ghi. Đó không phải tình huống của bạn.

## Sửa một chỗ sai trong bảng

Bảng tách "OIDC" và "Managed Identity" thành 2 dòng và nói MI chỉ dùng được *bên trong Azure*. Không đúng nữa: **User-Assigned Managed Identity hỗ trợ federated credential**, nghĩa là GitHub Actions dùng OIDC để "trở thành" MI đó. OIDC là *cơ chế*, MI hay App Registration là *danh tính*. Chọn MI vì không cần quyền tạo App Registration trong Entra và không có client secret để lỡ tay tạo.

## Flow (để tưởng tượng)

```
GitHub runner ──(1) xin JWT từ GitHub OIDC provider: "tôi là repo X, branch main"
              ──(2) gửi JWT cho Entra ID
Entra ID      ──(3) kiểm tra federated credential trên MI: issuer + subject khớp? → cấp access token Azure (ngắn hạn)
runner        ──(4) az acr login / docker push  (MI có AcrPush)
              ──(5) az containerapp update --image ...  (MI có Contributor)
```

Không có bước nào GitHub giữ password. Nếu ai đó copy workflow sang repo khác, bước 3 fail vì subject không khớp.

## Việc cần làm (1 lần, ~15 phút)

1. `az identity create -g <rg> -n gh-deploy-identity`
2. `az identity federated-credential create --identity-name gh-deploy-identity -g <rg> -n github-main --issuer https://token.actions.githubusercontent.com --subject repo:taitrandev/Tour-Agent:ref:refs/heads/main --audiences api://AzureADTokenExchange`
3. Gán role: `AcrPush` trên ACR, `Contributor` trên resource group (thu hẹp sau nếu muốn).
4. GitHub repo → Variables (không cần Secrets): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
5. Workflow: `permissions: id-token: write`, step `azure/login@v2` với 3 ID trên, rồi `az acr login --name <acr>` → `docker push`.

Lưu ý tách biệt: identity này là để **CI/CD push và deploy**. Việc **Container App pull image** từ ACR là chuyện khác — dùng system-assigned identity của chính Container App với role `AcrPull`. Hai identity, hai mục đích; hay bị lẫn.

## Failure mode sẽ gặp

- Quên `permissions: id-token: write` → lỗi "unable to get ACTIONS_ID_TOKEN_REQUEST_URL".
- Subject sai (chạy từ PR thì subject là `pull_request`, không phải `ref:refs/heads/main`) → login fail. MVP chỉ deploy từ main nên 1 credential đủ.
- Role assignment mất 1-2 phút propagate → chạy ngay sau khi gán có thể 403, chạy lại là được.

Tier ACR nào hỗ trợ repository-scoped token tôi không nhớ chắc — không quan trọng nữa vì ta không dùng.

---

câu hỏi: "- vậy tôi cần phải tạo 2 cloud service là OIDC và managed Identity đúng không?

- hãy cho tôi biết OIDC và Assigned Managed Identity bản chất là gì? tại sao cần phải tạo? có vai trò gì trong CI CD trong bước nào? hãy cho tôi flow và ví dụ dễ hiểu

- hãy giúp tôi tạo OIDC và Assigned Managed Identity bằng tay. CI CD cần output gì từ 2 service này?"

## Kết luận

**Không. Bạn tạo 1 resource, không phải 2.** OIDC không phải dịch vụ để tạo — nó là *giao thức* (cách trao đổi token) mà GitHub và Entra ID đều đã hỗ trợ sẵn. Thứ bạn tạo là **1 User-Assigned Managed Identity**, rồi **thêm 1 dòng cấu hình** (federated credential) vào nó để nói "tin GitHub repo X". Rồi gán quyền.

---

## 1. Bản chất

### Managed Identity = một "tài khoản robot" trong Entra ID

- Giống user account nhưng **không có password**, không login bằng tay.
- Chỉ là một object định danh: có `Client ID`, `Principal ID`. Miễn phí.
- Giá trị của nó: Azure **gán quyền** (RBAC) cho object này — "được push vào ACR", "được sửa Container App".
- "User-assigned" = bạn tạo độc lập, gán cho ai cũng được (khác "system-assigned" là dính chặt vào 1 resource).

### OIDC (federated credential) = cách robot chứng minh danh tính mà không cần password

Vấn đề gốc: GitHub runner là máy ngoài Azure. Làm sao Entra ID biết "process này thực sự là repo của Tyler" mà không phải đưa password cho GitHub giữ?

Giải pháp: **tin một bên thứ 3 đã xác thực rồi.** GitHub biết chắc workflow nào đang chạy (nó chạy mà). GitHub ký một "giấy chứng nhận" (JWT) ghi: *issuer = token.actions.githubusercontent.com, subject = repo:taitrandev/Tour-Agent:ref:refs/heads/main*. Entra ID có public key của GitHub nên **tự kiểm tra chữ ký được**.

Federated credential = dòng cấu hình trên Managed Identity nói: *"nếu ai mang giấy từ issuer X với subject Y → cho họ dùng identity này."*

**Ví dụ đời thường:** Bạn vào tòa nhà công ty khách hàng. Cách cũ (client secret): họ đưa bạn 1 thẻ từ, bạn giữ mãi, mất là toang. Cách OIDC: bảo vệ gọi lên công ty bạn xác nhận "đúng là nhân viên Tyler đang đến", rồi cấp thẻ khách dùng trong 1 giờ. Không ai giữ thẻ lâu dài. Managed Identity là *hồ sơ khách* đã đăng ký trước ("Tyler từ công ty A được vào tầng 3"); federated credential là dòng *"xác nhận qua công ty A"*; OIDC là *cuộc gọi xác nhận*.

---

## 2. Vai trò trong pipeline — bước nào

| Bước workflow                    | Dùng gì               | Chuyện gì xảy ra                                                                                                  |
| ---------------------------------- | ----------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `permissions: id-token: write`   | OIDC                    | Cho phép job xin JWT từ GitHub                                                                                     |
| `azure/login@v2`                 | OIDC + MI               | Runner xin JWT → gửi Entra → Entra kiểm tra federated credential trên MI → trả access token Azure (sống ~1h) |
| `az acr login` + `docker push` | MI role `AcrPush`     | Token trên có quyền push                                                                                          |
| `az containerapp update --image` | MI role `Contributor` | Token trên có quyền sửa app                                                                                      |

CI (build/test) **không cần** gì cả. Chỉ các step đụng Azure mới cần.

---

## 3. Tạo bằng tay

Giả sử: RG `rg-tour-agent`, ACR `acrtouragent`, region `southeastasia`.

```bash
# 0. Nền (nếu chưa có)
az group create -n rg-tour-agent -l southeastasia
az acr create -g rg-tour-agent -n acrtouragent --sku Basic

# 1. Tạo Managed Identity (resource duy nhất phải tạo)
az identity create -g rg-tour-agent -n id-github-deploy

# 2. Thêm federated credential = "tin GitHub repo này, branch main"
az identity federated-credential create \
  --identity-name id-github-deploy -g rg-tour-agent \
  -n github-main \
  --issuer https://token.actions.githubusercontent.com \
  --subject repo:taitrandev/Tour-Agent:ref:refs/heads/main \
  --audiences api://AzureADTokenExchange

# 3. Gán quyền cho identity
PRINCIPAL_ID=$(az identity show -g rg-tour-agent -n id-github-deploy --query principalId -o tsv)
ACR_ID=$(az acr show -n acrtouragent --query id -o tsv)
RG_ID=$(az group show -n rg-tour-agent --query id -o tsv)

az role assignment create --assignee-object-id $PRINCIPAL_ID --assignee-principal-type ServicePrincipal --role AcrPush --scope $ACR_ID
az role assignment create --assignee-object-id $PRINCIPAL_ID --assignee-principal-type ServicePrincipal --role Contributor --scope $RG_ID

# 4. Lấy 3 output cho GitHub
az identity show -g rg-tour-agent -n id-github-deploy --query clientId -o tsv
az account show --query tenantId -o tsv
az account show --query id -o tsv
```

Portal tương đương: *Managed Identities → Create* → vào identity → *Federated credentials → Add → GitHub Actions deploying Azure resources* (điền org/repo/branch) → *Access control (IAM)* gán role.

---

## 4. Output CI/CD cần — đúng 3 giá trị

| Tên trong GitHub         | Lấy từ                           | Secret hay Variable?             |
| ------------------------- | ---------------------------------- | -------------------------------- |
| `AZURE_CLIENT_ID`       | `clientId` của Managed Identity | Variable (không phải bí mật) |
| `AZURE_TENANT_ID`       | `az account show`                | Variable                         |
| `AZURE_SUBSCRIPTION_ID` | `az account show`                | Variable                         |

Không có password nào. Đây là điểm khác biệt cốt lõi so với Service Principal + secret.

Trong workflow:

```yaml
permissions:
  id-token: write
  contents: read

steps:
  - uses: azure/login@v2
    with:
      client-id: ${{ vars.AZURE_CLIENT_ID }}
      tenant-id: ${{ vars.AZURE_TENANT_ID }}
      subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
  - run: az acr login --name acrtouragent
  - run: docker push acrtouragent.azurecr.io/tour-agent-api:${{ github.sha }}
```

---

## 5. Lưu ý

- **Subject phải khớp chính xác.** `ref:refs/heads/main` chỉ match push lên main. Chạy từ PR → subject khác → login fail. MVP deploy từ main là đủ.
- **Role propagation** mất 1-5 phút; 403 ngay sau khi gán → chờ, chạy lại.
- **Identity này ≠ identity Container App dùng để pull image.** Khi tạo Container App sau này, bật system-assigned identity của nó và gán `AcrPull`. Hai identity, hai việc; giữ tách để khi phỏng vấn giải thích được nguyên tắc least privilege.
- `Contributor` trên RG là rộng; đủ cho MVP. Thu hẹp sau khi pipeline xanh.

**Next:** chạy 4 bước trên, test bằng workflow chỉ có `azure/login` + `az acr login` (chưa push gì). Xanh → ghép vào CI.

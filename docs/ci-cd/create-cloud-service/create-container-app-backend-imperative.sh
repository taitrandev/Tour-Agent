#!/usr/bin/env bash
# ==============================================================================
# Script: create-container-app-backend-imperative.sh
# Mục đích: Khởi tạo hoặc Cập nhật TourAgent Backend API lên Azure Container Apps
# Phong cách: Hybrid Modular (Micro-YAML cho Probes/Env + Imperative CLI cho Identity/Ingress/Scale)
# ==============================================================================
set -euo pipefail

# --- 1. KHAI BÁO THÔNG SỐ CẤU HÌNH ---
RG="rg-touragent-prod"
LOCATION="southeastasia"
ACR_NAME="touragentacr"
APP_NAME="touragent-api"
REPOSITORY_NAME="touragent-api"
ENV_NAME="cae-touragent-prod"
IDENTITY_NAME="id-github-touragent-deploy"
TARGET_PORT=8080

# Các biến bảo mật & kết nối ứng dụng
DB_CONN="Host=aws-1-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.wdsfssenfbesfmevifkf;Password=Taitrantien123456;SSL Mode=Require;Trust Server Certificate=true"
DEEPSEEK_KEY="sk-92e03f3300c7429389be0a047169e06a"
TELEGRAM_TOKEN="8195824138:AAENOM5ujl4bzs9ceixW7wIggDFY8OHekAM"
ALLOWED_CORS="https://touragent-frontend.gentlecliff-410d3b6b.southeastasia.azurecontainerapps.io"

echo "==> [1/5] Thu thập thông tin định danh từ Azure..."
IDENTITY_ID=$(az identity show -g "$RG" -n "$IDENTITY_NAME" --query id -o tsv)
IDENTITY_PRINCIPAL=$(az identity show -g "$RG" -n "$IDENTITY_NAME" --query principalId -o tsv)
ACR_ID=$(az acr show -n "$ACR_NAME" -g "$RG" --query id -o tsv)

# --- 2. ĐẢM BẢO CONTAINER APPS ENVIRONMENT TỒN TẠI ---
if ! az containerapp env show -n "$ENV_NAME" -g "$RG" &>/dev/null; then
  echo "==> [2/5] Tạo mới Container App Environment: $ENV_NAME..."
  az containerapp env create -n "$ENV_NAME" -g "$RG" -l "$LOCATION" --logs-destination none
else
  echo "==> [2/5] Container App Environment [$ENV_NAME] đã sẵn sàng."
fi
ENV_ID=$(az containerapp env show -n "$ENV_NAME" -g "$RG" --query id -o tsv)

# --- 3. ĐẢM BẢO QUYỀN AcrPull CHO MANAGED IDENTITY (IDEMPOTENT) ---
ASSIGNMENT_EXISTS=$(az role assignment list \
  --assignee-object-id "$IDENTITY_PRINCIPAL" \
  --role "AcrPull" \
  --scope "$ACR_ID" \
  --query "[0].id" -o tsv 2>/dev/null || true)

if [ -z "$ASSIGNMENT_EXISTS" ]; then
  echo "==> [3/5] Gán quyền AcrPull cho Managed Identity..."
  az role assignment create \
    --assignee-object-id "$IDENTITY_PRINCIPAL" \
    --assignee-principal-type ServicePrincipal \
    --role AcrPull \
    --scope "$ACR_ID"
else
  echo "==> [3/5] Quyền AcrPull đã tồn tại trên ACR."
fi

# --- 4. ÁP DỤNG MICRO-YAML (CHỈ CHỨA CONTAINER, ENV VARS VÀ PROBES) ---
echo "==> [4/5] Áp dụng cấu hình Container & HTTP Health Probes..."
LATEST_TAG=$(az acr repository show-tags -n "$ACR_NAME" --repository "$REPOSITORY_NAME" --orderby time_desc --top 1 -o tsv 2>/dev/null || true)

if [ -n "$LATEST_TAG" ]; then
  IMAGE_URI="${ACR_NAME}.azurecr.io/${REPOSITORY_NAME}:${LATEST_TAG}"
  echo "    -> Image tag: $IMAGE_URI"
else
  IMAGE_URI="mcr.microsoft.com/azuredocs/aci-helloworld:latest"
  echo "    -> Dùng image tạm: $IMAGE_URI"
fi

# File Micro-YAML: Chứa Container Template + Identity & Registry để Azure có chìa khóa kéo image từ ACR lúc khởi tạo
cat << EOF > backend-container.yaml
location: ${LOCATION}
name: ${APP_NAME}
identity:
  type: UserAssigned
  userAssignedIdentities:
    '${IDENTITY_ID}': {}
properties:
  environmentId: ${ENV_ID}
  configuration:
    activeRevisionsMode: Single
    registries:
      - server: ${ACR_NAME}.azurecr.io
        identity: ${IDENTITY_ID}
  template:
    containers:
      - name: ${APP_NAME}
        image: ${IMAGE_URI}
        resources:
          cpu: 0.25
          memory: 0.5Gi
        env:
          - name: ConnectionStrings__DefaultConnection
            value: "${DB_CONN}"
          - name: DeepSeek__ApiKey
            value: "${DEEPSEEK_KEY}"
          - name: Telegram__BotToken
            value: "${TELEGRAM_TOKEN}"
          - name: ASPNETCORE_ENVIRONMENT
            value: "Production"
          - name: CORS_ALLOWED_ORIGINS
            value: "${ALLOWED_CORS}"
        probes:
          - type: Liveness
            httpGet:
              path: /health/live
              port: ${TARGET_PORT}
              scheme: HTTP
            initialDelaySeconds: 10
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
          - type: Readiness
            httpGet:
              path: /health/ready
              port: ${TARGET_PORT}
              scheme: HTTP
            initialDelaySeconds: 10
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
EOF

if ! az containerapp show -n "$APP_NAME" -g "$RG" &>/dev/null; then
  echo "    -> Đang TẠO MỚI Container App [${APP_NAME}]..."
  az containerapp create --name "$APP_NAME" --resource-group "$RG" --yaml backend-container.yaml
else
  echo "    -> Đang CẬP NHẬT Container App [${APP_NAME}]..."
  az containerapp update --name "$APP_NAME" --resource-group "$RG" --yaml backend-container.yaml
fi

rm -f backend-container.yaml

# --- 5. ĐỒNG BỘ CẤU HÌNH HẠ TẦNG BẰNG CÁC LỆNH IMPERATIVE CLI CHUYÊN BIỆT ---
echo "==> [5/5] Cấu hình Mạng (Ingress), Auto-Scaling và Định danh ACR..."

echo "    -> 1. Cấu hình Ingress External Port ${TARGET_PORT}..."
az containerapp ingress enable \
  --name "$APP_NAME" \
  --resource-group "$RG" \
  --type external \
  --target-port "$TARGET_PORT"

echo "    -> 2. Cấu hình Scaling (Min: 0, Max: 1)..."
az containerapp update \
  --name "$APP_NAME" \
  --resource-group "$RG" \
  --min-replicas 0 \
  --max-replicas 1

echo "    -> 3. Gán User-Assigned Managed Identity..."
az containerapp identity assign \
  --name "$APP_NAME" \
  --resource-group "$RG" \
  --user-assigned "$IDENTITY_ID"

echo "    -> 4. Cấu hình xác thực ACR với Managed Identity..."
az containerapp registry set \
  --name "$APP_NAME" \
  --resource-group "$RG" \
  --server "${ACR_NAME}.azurecr.io" \
  --identity "$IDENTITY_ID"

echo "=============================================================================="
echo "  HOÀN TẤT THÀNH CÔNG! ĐƯỜNG DẪN TRUY CẬP BACKEND API (FQDN):"
FQDN=$(az containerapp show -n "$APP_NAME" -g "$RG" --query "properties.configuration.ingress.fqdn" -o tsv)
echo "  https://${FQDN}"
echo "=============================================================================="
echo "  Kiểm tra nhanh Health Endpoints:"
echo "  curl -s -i https://${FQDN}/health/live"
echo "  curl -s -i https://${FQDN}/health/ready"
echo "=============================================================================="

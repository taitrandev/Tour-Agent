#!/usr/bin/env bash
# ==============================================================================
# Script: create-container-app.sh
# Mục đích: Khởi tạo (Day-0) hoặc Cập nhật TourAgent API lên Azure Container Apps
# Tiêu chuẩn: 80/20 Cloud-Ready, 100% Cấu hình YAML (Probes + Env Vars + Identity)
# ==============================================================================
set -euo pipefail

# --- 1. KHAI BÁO THÔNG SỐ CẤU HÌNH ---
RG="rg-touragent-prod"
LOCATION="southeastasia"
ACR_NAME="touragentacr"
APP_NAME="touragent-api"
ENV_NAME="cae-touragent-prod"
IDENTITY_NAME="id-github-touragent-deploy"
TARGET_PORT=8080

# Các biến bảo mật & kết nối ứng dụng
DB_CONN=""
DEEPSEEK_KEY=""
TELEGRAM_TOKEN=""
ALLOWED_CORS="https://toidicopydao.io.vn"

echo "==> [1/4] Thu thập thông tin định danh từ Azure..."
IDENTITY_ID=$(az identity show -g "$RG" -n "$IDENTITY_NAME" --query id -o tsv)
IDENTITY_PRINCIPAL=$(az identity show -g "$RG" -n "$IDENTITY_NAME" --query principalId -o tsv)
ACR_ID=$(az acr show -n "$ACR_NAME" -g "$RG" --query id -o tsv)

# --- 2. ĐẢM BẢO CONTAINER APPS ENVIRONMENT TỒN TẠI ---
if ! az containerapp env show -n "$ENV_NAME" -g "$RG" &>/dev/null; then
  echo "==> [2/4] Tạo mới Container App Environment: $ENV_NAME..."
  az containerapp env create -n "$ENV_NAME" -g "$RG" -l "$LOCATION" --logs-destination none
else
  echo "==> [2/4] Container App Environment [$ENV_NAME] đã sẵn sàng."
fi
ENV_ID=$(az containerapp env show -n "$ENV_NAME" -g "$RG" --query id -o tsv)

# --- 3. ĐẢM BẢO QUYỀN AcrPull CHO MANAGED IDENTITY (IDEMPOTENT) ---
ASSIGNMENT_EXISTS=$(az role assignment list \
  --assignee-object-id "$IDENTITY_PRINCIPAL" \
  --role "AcrPull" \
  --scope "$ACR_ID" \
  --query "[0].id" -o tsv 2>/dev/null || true)

if [ -z "$ASSIGNMENT_EXISTS" ]; then
  echo "==> [3/4] Gán quyền AcrPull cho Managed Identity..."
  az role assignment create \
    --assignee-object-id "$IDENTITY_PRINCIPAL" \
    --assignee-principal-type ServicePrincipal \
    --role AcrPull \
    --scope "$ACR_ID"
else
  echo "==> [3/4] Quyền AcrPull đã tồn tại trên ACR."
fi

# --- 4. TỔNG HỢP CẤU HÌNH VÀO FILE YAML (ENV VARS + HEALTH PROBES HTTP) ---
echo "==> [4/4] Khởi tạo / Cập nhật Container App bằng cấu hình YAML chuẩn..."
LATEST_TAG=$(az acr repository show-tags -n "$ACR_NAME" --repository touragent-api --orderby time_desc --top 1 -o tsv)
IMAGE_URI="${ACR_NAME}.azurecr.io/touragent-api:${LATEST_TAG}"
echo "    -> Image tag: $IMAGE_URI"

# Sinh file YAML cấu hình hoàn chỉnh: Giải quyết triệt để vấn đề ký tự đặc biệt trong Env Vars
# và cấu hình chính xác HTTP Probes (/health/live, /health/ready) ngay trong 1 bước duy nhất!
cat << EOF > app-config.yaml
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
    ingress:
      external: true
      targetPort: ${TARGET_PORT}
      transport: auto
      allowInsecure: false
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
    scale:
      minReplicas: 0
      maxReplicas: 1
EOF

# Triển khai cấu hình bằng lệnh CLI
if ! az containerapp show -n "$APP_NAME" -g "$RG" &>/dev/null; then
  echo "    -> Đang TẠO MỚI Container App..."
  az containerapp create --name "$APP_NAME" --resource-group "$RG" --environment "$ENV_NAME" --yaml app-config.yaml
else
  echo "    -> Đang CẬP NHẬT Container App (đồng bộ Env Vars & Health Probes HTTP)..."
  az containerapp update --name "$APP_NAME" --resource-group "$RG" --yaml app-config.yaml
fi

# Dọn dẹp file tạm
rm -f app-config.yaml

echo "=============================================================================="
echo "  HOÀN TẤT THÀNH CÔNG! ĐƯỜNG DẪN TRUY CẬP (FQDN):"
FQDN=$(az containerapp show -n "$APP_NAME" -g "$RG" --query "properties.configuration.ingress.fqdn" -o tsv)
echo "  https://${FQDN}"
echo "=============================================================================="
echo "  Kiểm tra nhanh Health Endpoints:"
echo "  curl -s -i https://${FQDN}/health/live"
echo "  curl -s -i https://${FQDN}/health/ready"
echo "=============================================================================="

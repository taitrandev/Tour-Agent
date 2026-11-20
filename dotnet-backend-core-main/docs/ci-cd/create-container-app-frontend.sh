#!/usr/bin/env bash
# ==============================================================================
# Script: create-container-app-frontend.sh
# Mục đích: Khởi tạo (Day-0) hoặc Cập nhật TourAgent Frontend lên Azure Container Apps
# Tiêu chuẩn: 80/20 Cloud-Ready, 100% Cấu hình YAML (Nginx SPA + Probes + Managed Identity)
# ==============================================================================
set -euo pipefail

# --- 1. KHAI BÁO THÔNG SỐ CẤU HÌNH ---
RG="rg-touragent-prod"
LOCATION="southeastasia"
ACR_NAME="touragentacr"
APP_NAME="touragent-frontend"
REPOSITORY_NAME="touragent-frontend"
ENV_NAME="cae-touragent-prod"
IDENTITY_NAME="id-github-touragent-deploy"
TARGET_PORT=80

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

# --- 4. TỔNG HỢP CẤU HÌNH VÀO FILE YAML (HEALTH PROBES HTTP + NGINX PORT 80) ---
echo "==> [4/4] Khởi tạo / Cập nhật Container App bằng cấu hình YAML chuẩn..."

# Kiểm tra xem repository trên ACR đã có tag nào chưa
LATEST_TAG=$(az acr repository show-tags -n "$ACR_NAME" --repository "$REPOSITORY_NAME" --orderby time_desc --top 1 -o tsv 2>/dev/null || true)

if [ -n "$LATEST_TAG" ]; then
  IMAGE_URI="${ACR_NAME}.azurecr.io/${REPOSITORY_NAME}:${LATEST_TAG}"
  echo "    -> Sử dụng Image mới nhất từ ACR: $IMAGE_URI"
else
  # Nếu lần đầu chưa chạy CI/CD đẩy image lên ACR, dùng image quickstart tạm để khởi tạo app
  IMAGE_URI="mcr.microsoft.com/azuredocs/aci-helloworld:latest"
  echo "    -> Chưa có image trong ACR repository [$REPOSITORY_NAME]."
  echo "    -> Khởi tạo bằng image tạm: $IMAGE_URI (CI/CD sẽ tự động cập nhật sau)"
fi

# Sinh file YAML cấu hình hoàn chỉnh
cat << EOF > frontend-config.yaml
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
        probes:
          - type: Liveness
            httpGet:
              path: /
              port: ${TARGET_PORT}
              scheme: HTTP
            initialDelaySeconds: 5
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
          - type: Readiness
            httpGet:
              path: /
              port: ${TARGET_PORT}
              scheme: HTTP
            initialDelaySeconds: 5
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
    scale:
      minReplicas: 0
      maxReplicas: 1
EOF

# Triển khai cấu hình bằng lệnh Azure CLI (Idempotent: Tạo mới nếu chưa có, Cập nhật nếu đã có)
if ! az containerapp show -n "$APP_NAME" -g "$RG" &>/dev/null; then
  echo "    -> Đang TẠO MỚI Container App [${APP_NAME}]..."
  az containerapp create --name "$APP_NAME" --resource-group "$RG" --environment "$ENV_NAME" --yaml frontend-config.yaml
else
  echo "    -> Đang CẬP NHẬT Container App [${APP_NAME}]..."
  az containerapp update --name "$APP_NAME" --resource-group "$RG" --yaml frontend-config.yaml
fi

# Dọn dẹp file YAML tạm
rm -f frontend-config.yaml

echo "=============================================================================="
echo "  HOÀN TẤT THÀNH CÔNG! ĐƯỜNG DẪN TRUY CẬP FRONTEND (FQDN):"
FQDN=$(az containerapp show -n "$APP_NAME" -g "$RG" --query "properties.configuration.ingress.fqdn" -o tsv)
echo "  https://${FQDN}"
echo "=============================================================================="


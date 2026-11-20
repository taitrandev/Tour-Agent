#!/usr/bin/env bash
# ==============================================================================
# Script: create-container-app-frontend-imperative.sh
# Mục đích: Khởi tạo hoặc Cập nhật TourAgent Frontend lên Azure Container Apps
# Phong cách: 100% Imperative CLI (Không dùng file YAML, trực quan, dễ bảo trì)
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

# --- 4. TRIỂN KHAI FRONTEND BẰNG IMPERATIVE CLI (ZERO YAML) ---
echo "==> [4/4] Khởi tạo / Cập nhật Container App bằng 100% lệnh CLI tường minh..."

# Kiểm tra image mới nhất trong ACR hoặc dùng image tạm
LATEST_TAG=$(az acr repository show-tags -n "$ACR_NAME" --repository "$REPOSITORY_NAME" --orderby time_desc --top 1 -o tsv 2>/dev/null || true)

if [ -n "$LATEST_TAG" ]; then
  IMAGE_URI="${ACR_NAME}.azurecr.io/${REPOSITORY_NAME}:${LATEST_TAG}"
  echo "    -> Sử dụng Image mới nhất từ ACR: $IMAGE_URI"
else
  IMAGE_URI="mcr.microsoft.com/azuredocs/aci-helloworld:latest"
  echo "    -> Chưa có image trong ACR repository. Khởi tạo bằng image tạm: $IMAGE_URI"
fi

if ! az containerapp show -n "$APP_NAME" -g "$RG" &>/dev/null; then
  echo "    -> Đang TẠO MỚI Container App [${APP_NAME}] bằng CLI trực tiếp..."
  az containerapp create \
    --name "$APP_NAME" \
    --resource-group "$RG" \
    --environment "$ENV_NAME" \
    --image "$IMAGE_URI" \
    --target-port "$TARGET_PORT" \
    --ingress external \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 0 \
    --max-replicas 1 \
    --user-assigned "$IDENTITY_ID" \
    --registry-server "${ACR_NAME}.azurecr.io" \
    --registry-identity "$IDENTITY_ID"
else
  echo "    -> Đang CẬP NHẬT Container App [${APP_NAME}]..."
  az containerapp update \
    --name "$APP_NAME" \
    --resource-group "$RG" \
    --image "$IMAGE_URI" \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 0 \
    --max-replicas 1

  echo "    -> Cấu hình Ingress Port 80..."
  az containerapp ingress enable \
    --name "$APP_NAME" \
    --resource-group "$RG" \
    --type external \
    --target-port "$TARGET_PORT"

  echo "    -> Gán Managed Identity..."
  az containerapp identity assign \
    --name "$APP_NAME" \
    --resource-group "$RG" \
    --user-assigned "$IDENTITY_ID"

  echo "    -> Cấu hình xác thực ACR với Managed Identity..."
  az containerapp registry set \
    --name "$APP_NAME" \
    --resource-group "$RG" \
    --server "${ACR_NAME}.azurecr.io" \
    --identity "$IDENTITY_ID"
fi

echo "=============================================================================="
echo "  HOÀN TẤT THÀNH CÔNG! ĐƯỜNG DẪN TRUY CẬP FRONTEND (FQDN):"
FQDN=$(az containerapp show -n "$APP_NAME" -g "$RG" --query "properties.configuration.ingress.fqdn" -o tsv)
echo "  https://${FQDN}"
echo "=============================================================================="

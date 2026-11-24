#!/usr/bin/env bash
set -e

# Xác định thư mục root của repository
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SLN_PATH="$REPO_ROOT/dotnet-backend-core-main/TourAgent.slnx"

if [ ! -f "$SLN_PATH" ]; then
    echo "❌ Không tìm thấy file solution tại: $SLN_PATH"
    exit 1
fi

# Chuyển đổi path sang Windows style nếu đang gọi dotnet.exe trên Windows
DOTNET_CMD="dotnet"
if ! command -v dotnet &> /dev/null; then
    if command -v dotnet.exe &> /dev/null; then
        DOTNET_CMD="dotnet.exe"
    elif [ -f "/c/Program Files/dotnet/dotnet.exe" ]; then
        DOTNET_CMD="/c/Program Files/dotnet/dotnet.exe"
    elif [ -f "/mnt/c/Program Files/dotnet/dotnet.exe" ]; then
        DOTNET_CMD="/mnt/c/Program Files/dotnet/dotnet.exe"
    fi
fi

TARGET_SLN="$SLN_PATH"
if command -v cygpath &> /dev/null; then
    TARGET_SLN=$(cygpath -w "$SLN_PATH")
elif command -v wslpath &> /dev/null; then
    TARGET_SLN=$(wslpath -w "$SLN_PATH")
fi

echo "====================================================="
echo "🚀 Đang kiểm tra toàn diện Backend TourAgent (.NET 10)"
echo "📂 Solution: $TARGET_SLN"
echo "====================================================="

START_TIME=$(date +%s)

"$DOTNET_CMD" test "$TARGET_SLN" "$@"
EXIT_CODE=$?

END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo "====================================================="
    echo "✅ TẤT CẢ TESTS PASSED (Thời gian: ${ELAPSED}s)"
    echo "====================================================="
else
    echo "====================================================="
    echo "❌ CÓ TEST THẤT BẠI HOẶC BUILD ERROR (Exit Code: $EXIT_CODE)"
    echo "====================================================="
fi

exit $EXIT_CODE

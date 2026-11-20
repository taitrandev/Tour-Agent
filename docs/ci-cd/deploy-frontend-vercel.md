# Hướng Dẫn Deploy Frontend Lên Vercel (Kết Nối Backend Azure)

Tài liệu này hướng dẫn chi tiết quy trình deploy ứng dụng `tour-agent-frontend` (React 19 + Vite) lên **Vercel** (miễn phí 100%, Global CDN, tự động CI/CD khi push GitHub) và kết nối an toàn với Backend .NET 10 đang chạy trên **Azure Container Apps**.

---

## 1. Chuẩn Bị File Cấu Hình SPA Routing (`vercel.json`)

Để tránh lỗi HTTP 404 khi người dùng truy cập trực tiếp hoặc nhấn F5 trên các deep link (`/tour/:slug`, `/admin/products`), file `tour-agent-frontend/vercel.json` đã được tạo sẵn:

```json
{
  "rewrites": [
    {
      "source": "/(.*)",
      "destination": "/index.html"
    }
  ]
}
```

* **Cơ chế**: Vercel sẽ tự động rewrite mọi request đường dẫn trang web về `/index.html` để React Router v7 xử lý định tuyến phía client.

---

## 2. Các Bước Deploy Trên Vercel (Chỉ mất 2 phút)

### Bước 2.1: Đăng nhập Vercel
1. Truy cập [https://vercel.com](https://vercel.com).
2. Chọn **Sign Up** hoặc **Log In** bằng tài khoản **GitHub** (chứa repo `Tour-Agent`).

### Bước 2.2: Import Repository
1. Tại màn hình Dashboard của Vercel, bấm nút **Add New...** (góc trên bên phải) ➔ Chọn **Project**.
2. Tìm repository **`Tour-Agent`** trong danh sách ➔ Bấm **Import**.

### Bước 2.3: Cấu Hình Dự Án (QUAN TRỌNG)
Ở màn hình **Configure Project**:

1. **Project Name**: Để mặc định (hoặc đặt `tour-agent-frontend`).
2. **Root Directory (BẮT BUỘC CHỈNH)**:
   * Bấm nút **Edit** bên cạnh mục Root Directory.
   * Chọn thư mục **`tour-agent-frontend`** ➔ Bấm **Continue**.
   *(Vì repo của bạn là monorepo chứa cả frontend và backend, nên phải trỏ đúng vào thư mục frontend)*.
3. **Framework Preset**: Vercel sẽ tự động nhận diện là **Vite**.
4. **Environment Variables**:
   * Mở rộng mục **Environment Variables**.
   * Thêm biến kết nối Backend API:
     * **Key**: `VITE_API_BASE_URL`
     * **Value**: `https://touragent-api.politestone-c266986a.southeastasia.azurecontainerapps.io/api/v1`
   * Bấm **Add**.

### Bước 2.4: Bấm Deploy
* Bấm nút **Deploy**.
* Vercel sẽ tự động pull code, chạy `npm install`, `npm run build` và deploy lên Global Edge Network trong khoảng **30 - 45 giây**.
* Khi màn hình pháo hoa xuất hiện, bạn sẽ nhận được một domain chính thức (dạng: `https://tour-agent-frontend-xxx.vercel.app` hoặc `https://tour-agent-frontend.vercel.app`).

---

## 3. Cập Nhật CORS Cho Backend Azure Container Apps

Khi đã có domain Vercel, bạn cần cấp phép cho domain này gọi vào Backend .NET 10.

Mở **Azure Cloud Shell** và chạy lệnh:

```bash
# Thay domain Vercel thực tế của bạn vào chuỗi bên dưới
az containerapp update \
  --name touragent-api \
  --resource-group rg-touragent-prod \
  --set-env-vars CORS_ALLOWED_ORIGINS="https://toidicopydao.io.vn,https://<ten-du-an-cua-ban>.vercel.app"
```

Lệnh này sẽ tạo một revision mới trên Azure và kích hoạt CORS cho domain Vercel ngay lập tức mà không làm gián đoạn hệ thống.

---

## 4. Tự Động Hóa CI/CD Sau Này

Từ nay về sau:
* Mỗi khi bạn `git push` code mới lên nhánh `main` trong thư mục `tour-agent-frontend/**`, **Vercel sẽ tự động build và deploy lại phiên bản mới** trong vòng 30 giây mà bạn không cần phải thao tác thủ công.
* Khi bạn tạo Pull Request, Vercel sẽ tự động tạo một đường link **Preview** riêng để bạn test giao diện trước khi merge.

---

## 5. Checklist Nghiệm Thu

1. **Kiểm tra SPA Routing**:
   * Truy cập thẳng vào: `https://<ten-du-an>.vercel.app/admin/products`.
   * Nhấn **F5** ➔ Trang vẫn hiển thị danh sách sản phẩm tour, **không** bị lỗi 404.
2. **Kiểm tra Chat AI Sakura & Gọi API**:
   * Mở trang chủ ➔ Bấm vào khung chat với AI Agent Sakura.
   * Gửi một câu hỏi (ví dụ: *"Tư vấn cho tôi tour Đà Lạt 3 ngày 2 đêm"*).
   * Mở F12 (DevTools) tab **Network**: Kiểm tra request `/api/v1/agent/chat/stream` trả về HTTP 200, tin nhắn được stream mượt mà từng chữ về giao diện.


# ADR-0010: Tập Trung Hóa API Routes Trên Frontend (Zero Hardcoded URLs)

- **Trạng thái**: Accepted (Đã chấp thuận & Áp dụng)
- **Ngày quyết định**: 2026-11-24
- **Người tham gia**: User (Product Owner) & Antigravity Agent
- **Phạm vi ảnh hưởng**: `tour-agent-frontend`, `src/constants/apiRoutes.ts`, React Pages & Components, API Services

---

> [!IMPORTANT]
> ### 🛑 NGUYÊN TẮC BẤT DI BẤT DỊCH CHO AI AGENT (Golden Rules)
> - **✅ ĐƯỢC PHÉP**: 
>   - Mọi URL endpoint HTTP được gọi từ Frontend (`apiFetch`, `fetch`, SSE `EventSource`, v.v.) **bắt buộc** định nghĩa tập trung trong `tour-agent-frontend/src/constants/apiRoutes.ts` thông qua hằng số `API_ROUTES`.
>   - Với các route có tham số động (như `id`, `slug`), bắt buộc định nghĩa hàm helper nhận tham số (ví dụ: `API_ROUTES.ADMIN.TRAVEL_BY_ID(id)`, `API_ROUTES.TRAVELS.BY_SLUG(slug)`).
>   - Các service hoặc component chỉ được import và gọi API thông qua `API_ROUTES.*`.
> - **❌ TUYỆT ĐỐI CẤM**:
>   - CẤM gõ chuỗi URL trực tiếp như `'/api/v1/...'` rải rác trong React components, hooks hay API services.
>   - CẤM ghép chuỗi thủ công bằng template string (ví dụ: `` `/api/v1/admin/travels/${id}` ``) tại nơi gọi.
>   - Mọi endpoint mới phải được bổ sung vào `API_ROUTES` trước khi sử dụng.

---

## 1. Bối Cảnh & Vấn Đề (Context & Problem Statement)

Trong quá trình phát triển nhanh giao diện quản trị (Admin Desk) và cổng thông tin khách hàng (Client Portal):
1. **Phân mảnh chuỗi URL (Magic Strings Sprawl)**: Hơn 35 vị trí gọi API rải rác khắp 15 file (`AdminProductsPage.tsx`, `AdminOrdersPage.tsx`, `toursApi.ts`, `FloatingChatWidget.tsx`...) đều tự gõ trực tiếp chuỗi URL (ví dụ: `apiFetch('/api/v1/admin/travels')`, `apiFetch('/api/v1/offers')`).
2. **Nguy cơ lỗi sai chính tả (Typo) & Thiếu Type-Safety**: Khi lập trình viên hoặc AI Agent gõ tay, chỉ cần nhầm một ký tự (ví dụ: `/api/v1/travel` thay vì `/api/v1/travels`) là API sẽ báo lỗi 404 trong lúc runtime mà TypeScript compiler không thể phát hiện lúc build.
3. **Chi phí Refactor khổng lồ (High Reverse Cost)**: Khi backend thay đổi phiên bản (ví dụ từ `/api/v1/` sang `/api/v2/`) hoặc tái cấu trúc route (như việc hợp nhất `ToursController` vào `TravelsController`), lập trình viên phải thực hiện tìm kiếm và thay thế thủ công trên hàng chục file, rất dễ bỏ sót endpoint.

---

## 2. Các Lựa Chọn Đã Xem Xét (Options Considered)

- **Phương án A: Tiếp tục hardcode chuỗi URL tại từng component (Bị bác bỏ)**:
  - Dễ dãi lúc viết code ban đầu nhưng biến dự án thành bãi mìn khi mở rộng, vi phạm nguyên lý DRY (Don't Repeat Yourself).
- **Phương án B: Tạo enum chuỗi (Bị bác bỏ)**:
  - TypeScript Enum không hỗ trợ truyền tham số động cho các route như `/api/v1/admin/travels/:id`.
- **Phương án C: Hằng số tập trung `API_ROUTES` theo Domain Namespace kết hợp Helper Functions (Được chọn)**:
  - Tổ chức dạng nested object phân theo domain (`AGENT`, `TRAVELS`, `OFFERS`, `ADMIN`, `WEBHOOKS`).
  - Sử dụng hằng số cho static route và helper arrow function cho dynamic route.
  - Tận dụng TypeScript autocomplete (IntelliSense) 100% giúp việc gõ API trở nên dễ dàng và chuẩn xác.

---

## 3. Quyết Định Kiến Trúc (Decision Outcome)

Toàn bộ Frontend thống nhất sử dụng hằng số tập trung tại file duy nhất: `tour-agent-frontend/src/constants/apiRoutes.ts`.

### Mã Nguồn Mẫu Chuẩn (Standard Code Example):

#### 1. Định nghĩa tập trung tại `src/constants/apiRoutes.ts`:
```typescript
/**
 * Nguồn chân lý duy nhất (Single Source of Truth) cho toàn bộ API routes trên Frontend.
 * Nghiêm cấm hardcode chuỗi URL trực tiếp trong components/hooks/services.
 */
export const API_ROUTES = {
  AGENT: {
    CHAT: '/api/v1/agent/chat',
    CHAT_STREAM: '/api/v1/agent/chat/stream',
    ADMIN_CHAT: '/api/v1/agent/admin/chat',
    ADMIN_CHAT_STREAM: '/api/v1/agent/admin/chat/stream',
    METRICS: '/api/v1/agent/metrics',
  },
  TRAVELS: {
    SEARCH: '/api/v1/travels/search',
    LIST: '/api/v1/travels',
    BY_SLUG: (slug: string) => `/api/v1/travels/slug/${encodeURIComponent(slug)}`,
    BY_ID: (id: string | number) => `/api/v1/travels/${id}`,
  },
  ADMIN: {
    TRAVELS: '/api/v1/admin/travels',
    TRAVEL_BY_ID: (id: string | number) => `/api/v1/admin/travels/${id}`,
    OFFERS: '/api/v1/admin/offers',
    OFFER_BY_ID: (id: string | number) => `/api/v1/admin/offers/${id}`,
  },
} as const;
```

#### 2. Sử dụng trong React Component / Service:
```typescript
import { API_ROUTES } from '@/constants/apiRoutes';
import { apiFetch } from '@/utils/apiFetch';

// Gọi static route
const res = await apiFetch(API_ROUTES.ADMIN.TRAVELS);

// Gọi dynamic route với helper function an toàn
const res = await apiFetch(API_ROUTES.ADMIN.TRAVEL_BY_ID(travelId), {
  method: 'DELETE',
});
```

---

## 4. Hệ Quả & Đánh Đổi (Consequences & Trade-offs)

### Ưu điểm (Pros)
1. **Single Source of Truth (SSOT)**: Nếu backend nâng cấp tiền tố API hoặc cấu trúc route, chỉ cần cập nhật đúng 1 dòng tại `apiRoutes.ts`, 100% ứng dụng sẽ cập nhật theo.
2. **Zero Typo & Type-Safe**: Tận dụng triệt để IDE Autocomplete, loại bỏ hoàn toàn khả năng gõ nhầm URL.
3. **An toàn URL Encoding**: Các dynamic parameters dạng text (`slug`) được tự động bọc `encodeURIComponent`, tránh lỗi vỡ URL khi có ký tự đặc biệt.

### Đánh đổi / Điểm cần lưu ý (Cons / Trade-offs)
- Khi phát triển tính năng mới có thêm API endpoint, developer bắt buộc phải qua bước khai báo vào `apiRoutes.ts` trước khi gọi.

---

## 5. Kiểm Thử Bảo Vệ (Verification & Test Guard)

1. Quy tắc này được đưa thành **Quy tắc bắt buộc số 12** và nằm trong **Danh sách điều CẤM** tại [AGENTS.md](../../AGENTS.md).
2. Lệnh kiểm tra tĩnh bằng `git grep`:
   ```bash
   # Không được có bất kỳ chuỗi '/api/v1/' nào nằm ngoài apiRoutes.ts
   git grep "'/api/v1/" tour-agent-frontend/src
   ```
   Kết quả chỉ được xuất hiện bên trong `src/constants/apiRoutes.ts`.


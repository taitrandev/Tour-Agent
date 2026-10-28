# 📡 API ENDPOINTS — Danh Mục & Đặc Tả Kỹ Thuật API (.NET 10)

> **File này là Single Source of Truth về các API Endpoints trên Backend .NET Core.**  
> Hỗ trợ Frontend Developer / AI Agent tra cứu nhanh route, query parameters, request payload, response schema và các status code.

---

## 🌐 Thông Tin Chung (General Info)
- **Base URL (Local Dev)**: `http://localhost:5000` (hoặc cấu hình theo launchSettings `http://localhost:5246`)
- **API Prefix**: `/api/v1`
- **Swagger / OpenAPI UI**: `http://localhost:5000/swagger`
- **Kiến trúc**: ASP.NET Core Web API Controllers, C# 13 / .NET 10
- **Định dạng dữ liệu**: `application/json` (trừ SSE Streaming: `text/event-stream`)

---

## 📋 Bảng Tổng Hợp Endpoints Đang Hoạt Động (Active Endpoints)

| Method | Endpoint | Controller | Chức năng | Trạng thái |
| :--- | :--- | :--- | :--- | :---: |
| `POST` | `/api/v1/agent/chat` | `AgentController` | Chat đồng bộ với AI Agent (Multi-turn tool loop) | ✅ Hoạt động |
| `POST` | `/api/v1/agent/chat/stream` | `AgentController` | Chat thời gian thực với AI Agent (SSE Streaming) | ✅ Hoạt động |
| `GET` | `/api/v1/tours` | `ToursController` | Tìm kiếm, lọc & phân trang danh sách tour | ✅ Hoạt động |
| `POST` | `/api/v1/webhooks/telegram` | `TelegramWebhookController` | Tiếp nhận Webhook tin nhắn Telegram (Async Channel) | ✅ Hoạt động |
| `GET` | `/api/v1/webhooks/telegram/status` | `TelegramWebhookController` | Healthcheck kiểm tra trạng thái kênh Telegram | ✅ Hoạt động |

---

## 🔍 Chi Tiết Từng Endpoint

### 1. AI Agent Consultant — Chat Đồng Bộ (Request-Response)

* **Route**: `POST /api/v1/agent/chat`
* **Mô tả**: Gửi tin nhắn câu hỏi từ người dùng/khách hàng. Hệ thống chạy chu trình đa lượt (Autonomous Multi-Turn Agent Loop), tự động gọi các tool tra cứu tour, chính sách, ngày bay từ PostgreSQL và trả về câu trả lời trọn vẹn kèm số liệu kiểm toán.
* **Headers**:
  ```http
  Content-Type: application/json
  Accept: application/json
  ```
* **Request Body Schema (`ChatRequest`)**:
  ```json
  {
    "conversationId": 1727412345678,
    "message": "Tôi muốn đi du lịch Nhật Bản vào tháng 11, ngân sách khoảng 30 triệu cho 2 người, có tour nào phù hợp không?",
    "currentTravelId": 1,
    "currentTravelTitle": "Cung Đường Vàng Tokyo - Phú Sĩ",
    "country": "Japan"
  }
  ```
  - `conversationId` *(long?, optional)*: Mã phiên hội thoại. Nếu truyền `null`, backend sẽ tự sinh ID theo Unix timestamp ms.
  - `message` *(string, required)*: Nội dung tin nhắn người dùng (không được để trống).
  - `currentTravelId` *(long?, optional)*: ID của tour khách hàng đang xem trên màn hình (để bot lấy ngữ cảnh tư vấn sâu).
  - `currentTravelTitle` *(string?, optional)*: Tiêu đề tour đang xem.
  - `country` *(string?, optional)*: Quốc gia của tour đang xem.

* **Response Success (200 OK — `AgentChatResult`)**:
  ```json
  {
    "responseText": "Chào bạn! Với ngân sách khoảng 30 triệu, mình xin gợi ý tour **Cung Đường Vàng Tokyo - Núi Phú Sĩ 5N4Đ** khởi hành từ Hà Nội vào ngày 15/11/2026 với giá chỉ 28.990.000 VNĐ/khách...",
    "toolsExecuted": [
      "search_travels",
      "check_departures"
    ],
    "totalTokens": 1280,
    "estimatedCostUsd": 0.00256,
    "totalLatencyMs": 1420,
    "turnsCount": 2,
    "isEscalated": false
  }
  ```
* **Response Error (400 Bad Request)**:
  ```json
  {
    "error": "Nội dung tin nhắn 'Message' không được để trống."
  }
  ```

---

### 2. AI Agent Consultant — Chat Trực Tiếp Thời Gian Thực (SSE Streaming)

* **Route**: `POST /api/v1/agent/chat/stream`
* **Mô tả**: Truyền phát trực tiếp từng token văn bản và sự kiện kích hoạt tool theo chuẩn **Server-Sent Events (SSE)**. Giúp Frontend hiển thị hiệu ứng gõ phím mượt mà và badge trạng thái tool tương tự ChatGPT / Claude.
* **Headers**:
  ```http
  Content-Type: application/json
  Accept: text/event-stream
  ```
* **Request Body**: Tương tự như `POST /api/v1/agent/chat`.
* **Response Stream Event Schema (`AgentStreamEvent`)**:
  Mỗi sự kiện trả về dưới dạng JSON stream:
  ```json
  {"eventType": "tool_start", "content": null, "toolName": "search_travels"}
  ```
  ```json
  {"eventType": "tool_end", "content": "Tìm thấy 2 tour phù hợp", "toolName": "search_travels"}
  ```
  ```json
  {"eventType": "token", "content": "Chào ", "toolName": null}
  ```
  ```json
  {"eventType": "token", "content": "bạn! ", "toolName": null}
  ```
  ```json
  {"eventType": "completed", "content": null, "toolName": null}
  ```

---

### 3. Tra Cứu & Lọc Tour Du Lịch (Search Tours)

* **Route**: `GET /api/v1/tours`
* **Mô tả**: Tìm kiếm và phân trang danh sách Tour du lịch dựa trên các bộ lọc linh hoạt: Điểm đến, Quốc gia, Thành phố xuất phát, Mức giá trần, và Từ khóa tự do.
* **Query Parameters (`SearchToursQuery`)**:
  | Tham số | Kiểu dữ liệu | Mặc định | Mô tả ví dụ |
  | :--- | :--- | :---: | :--- |
  | `destination` | string | `null` | Điểm đến (VD: `Tokyo`, `Đà Nẵng`) |
  | `country` | string | `null` | Quốc gia (VD: `Japan`, `Vietnam`) |
  | `departureCity` | string | `null` | Nơi khởi hành (VD: `Hà Nội`, `TP.HCM`) |
  | `maxPrice` | decimal | `null` | Giá trần tối đa (VD: `35000000`) |
  | `search` | string | `null` | Từ khóa chung tìm theo tên/mã tour |
  | `page` | int | `1` | Số thứ tự trang (bắt đầu từ 1) |
  | `pageSize` | int | `10` | Số bản ghi trên 1 trang |

* **Ví dụ Request URL**:
  ```http
  GET /api/v1/tours?country=Japan&departureCity=Hà Nội&maxPrice=30000000&page=1&pageSize=10
  ```

* **Response Success (200 OK — `SearchToursResult`)**:
  ```json
  {
    "items": [
      {
        "id": 1,
        "code": "JP-TYO-01",
        "name": "Khám Phá Cung Đường Vàng Tokyo - Núi Phú Sĩ - Kyoto",
        "destination": "Tokyo - Kyoto - Osaka",
        "country": "Japan",
        "departureCity": "Hà Nội",
        "durationDays": 6,
        "durationNights": 5,
        "priceFrom": 28990000.0,
        "currency": "VND",
        "highlights": "Ngắm hoa anh đào, Trải nghiệm tàu Shinkansen, Tắm Onsen truyền thống",
        "nextDepartureDate": "2026-10-20",
        "departuresCount": 4
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10
  }
  ```

* **Response Error (400 Bad Request — `Error`)**:
  ```json
  {
    "code": "Tours.InvalidFilter",
    "message": "Số trang (Page) hoặc kích thước trang (PageSize) phải lớn hơn 0."
  }
  ```

---

### 4. Telegram Webhook & Inbound Queue

* **Route**: `POST /api/v1/webhooks/telegram`
* **Mô tả**: Endpoint nhận trực tiếp payload từ Telegram Bot API Webhook. Đẩy gói tin vào `System.Threading.Channels.Channel` in-memory và lập tức trả lời HTTP 200 `{ "ok": true }` để tránh timeout/retry phía Telegram. Một `BackgroundService` ngầm sẽ tiêu thụ hàng đợi, nạp lịch sử cuộc trò chuyện, kích hoạt `AgentLoop` và gửi câu trả lời qua `TelegramAdapter`.
* **Payload**: Định dạng chuẩn Telegram Update JSON.
* **Response**:
  ```json
  {
    "ok": true
  }
  ```

---

### 5. Telegram Webhook Healthcheck

* **Route**: `GET /api/v1/webhooks/telegram/status`
* **Mô tả**: Kiểm tra trạng thái sẵn sàng của kênh tiếp nhận tin nhắn Telegram.
* **Response (200 OK)**:
  ```json
  {
    "status": "online",
    "channel": "telegram",
    "timestamp": "2026-09-27T12:00:00.000Z"
  }
  ```

---

## 🚀 Kế Hoạch Bổ Sung Endpoints Cho Giai Đoạn Frontend (Proposed for UI)

Dưới đây là danh sách các endpoints cần bổ sung trong slice tiếp theo để phục vụ đầy đủ Dashboard Admin và Client Travel Page:

| Phân hệ | Phương thức | Endpoint dự kiến | Mục đích cho Frontend |
| :--- | :---: | :--- | :--- |
| **Tours Management** | `GET` | `/api/v1/tours/{id}` | Lấy chi tiết lịch trình ngày 1..N, danh sách đợt bay, chính sách tour |
| | `POST` | `/api/v1/tours` | Nhập tour mới (Form Manual Entry thay thế modal PHP) |
| | `PUT` | `/api/v1/tours/{id}` | Sửa thông tin giá sàn, hành trình, đợt mở bán |
| | `DELETE` | `/api/v1/tours/{id}` | Xóa hoặc chuyển trạng thái ẩn tour |
| **Conversations Hub** | `GET` | `/api/v1/conversations` | Danh sách các phiên chat của khách (phân trang, lọc theo kênh web/telegram) |
| | `GET` | `/api/v1/conversations/{id}/messages` | Xem toàn bộ hội thoại, tool execution logs và token count |
| | `POST` | `/api/v1/conversations/{id}/escalate` | Nhân viên can thiệp tiếp quản cuộc trò chuyện |
| **Admin Stats** | `GET` | `/api/v1/analytics/overview` | Thống kê số lượng tour, số lượt tư vấn thành công, chi phí LLM tiêu thụ |

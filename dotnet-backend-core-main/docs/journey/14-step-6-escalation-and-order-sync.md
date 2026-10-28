# 14. Bước 6: Bàn Quyết Định (Escalations Desk) & Đồng Bộ Tự Động Đơn Hàng Tour (Order Sync)

> **Mục tiêu:** Xây dựng hệ thống Human-in-the-Loop hoàn chỉnh kết nối 3 chiều giữa **Phiên Chat AI**, **Bàn Quyết Định (Escalation Desk)**, **Đơn Hàng Tour (TourOrder)** và **Hồ Sơ Khách Hàng (CRM Contact)**, giải quyết triệt để vấn đề mất dấu lead nóng và đồng bộ hóa đơn hàng tự động.

---

## 1. Bối Cảnh Nghiệp Vụ & Vấn Đề Gặp Phải

### 1.1 Hiện Tượng
Khách hàng chat trên Telegram chốt giữ chỗ tour và cung cấp số điện thoại (ví dụ: `0909349909`), AI Agent phản hồi:
> *"Dạ em đã gửi yêu cầu thành công ạ! 📋 Phiếu yêu cầu giữ chỗ: #ESC-15916..."*

Tuy nhiên khi truy cập vào **Bàn Quyết Định (`/admin/escalations`)** và **Đường Ống Đơn Hàng (`/admin/orders`)**:
- Bảng Escalations chỉ hiển thị mock dữ liệu cũ (`#ESC-01`, `#ESC-02`, `#ESC-03`), không có ticket `#ESC-15916`.
- Bảng Orders không có bất kỳ đơn hàng nào được tạo mới.

### 1.2 Nguyên Nhân Gốc Rễ (Root Cause)
1. **Mock Tĩnh ở Frontend**: File `AdminEscalationsPage.tsx` khởi tạo `useState` với 3 vé cứng, chưa hề kết nối với API backend.
2. **Thiếu Entity & API Escalation trong .NET**: Bảng `agent_escalations` có sẵn trong CSDL PostgreSQL nhưng chưa có Entity `AgentEscalation` trong Domain, chưa đăng ký trong `AppDbContext` và chưa có Controller API.
3. **Mã Ticket Ảo**: `EscalateToHumanTool.cs` sinh mã vé bằng công thức ngẫu nhiên `DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000` (cho ra số `15916`) mà **hoàn toàn chưa ghi dữ liệu vào CSDL**.
4. **Thiếu Logic Tạo Order & Contact**: Tool không có code khởi tạo `TourOrder` và không cập nhật phân hạng `Stage = "hot"` trong `CrmContact`.

---

## 2. Kiến Trúc Giải Pháp & Quy Trình Đồng Bộ 3 Chiều

```text
               [Khách Hàng Chat Yêu Cầu Giữ Chỗ / SĐT]
                                  │
                                  ▼
                     [AgentLoop / DeepSeek AI]
                                  │
                     Kích hoạt tool escalate_to_human
                                  │
                                  ▼
                 ┌─────────────────────────────────┐
                 │     EscalateToHumanTool         │
                 └────────────────┬────────────────┘
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
┌──────────────────┐    ┌──────────────────┐     ┌──────────────────┐
│   CrmContact     │    │ AgentEscalation  │     │    TourOrder     │
│   (CRM 360°)     │    │ (Bàn Quyết Định) │     │ (Order Pipeline) │
├──────────────────┤    ├──────────────────┤     ├──────────────────┤
│ - Phone: 0909... │    │ - Ticket: #ESC-21│     │ - ORD-2609-XXXX  │
│ - Stage: "hot"   │    │ - Type: deal_hot │     │ - Deposit: 50%   │
│ - Channel: tele  │    │ - Status: pending│     │ - Status: deposit│
└──────────────────┘    └──────────────────┘     └──────────────────┘
```

---

## 3. Các Thành Phần Kỹ Thuật Đã Triển Khai

### 3.1 Domain Layer: Entity `AgentEscalation`
- Vị trí: `TourAgent.Domain/Escalations/AgentEscalation.cs`.
- Hỗ trợ đầy đủ các trạng thái nghiệp vụ: `pending`, `contacting`, `approved`, `rejected`, `closed`.
- Tính năng **Smart Merge**: Khi cùng một phiên hội thoại phát sinh thêm thông tin (bổ sung số khách, tour, ghi chú), ticket pending cũ sẽ tự động được hợp nhất mà không sinh ticket rác.

### 3.2 Infrastructure Layer: Ánh Xạ EF Core
- Vị trí: `TourAgent.Infrastructure/Persistence/Configurations/AgentEscalationConfiguration.cs`.
- Ánh xạ chuẩn vào bảng `agent_escalations` trong PostgreSQL, kết nối khóa ngoại với `conversations` và `travels`.

### 3.3 Application Layer: CQRS & Auto Sync Tool
- **`GetEscalationsQuery`**: Truy vấn danh sách tickets kèm thống kê 4 chỉ số KPI (`total`, `pending`, `contacting`, `approved`).
- **`UpdateEscalationStatusCommand`**: Cập nhật trạng thái khi chuyên viên nhấn *Nhận Cuộc Gọi*, *Duyệt Ưu Đãi*, *Đóng*.
- **`EscalateToHumanTool`**:
  - Tự động trích xuất hoặc phục hồi SĐT từ tin nhắn khách bằng Regex.
  - Tự động tìm hoặc tạo hồ sơ `CrmContact`, nâng hạng lên `StageHot`.
  - Khởi tạo bản ghi `AgentEscalation` thật trong CSDL (lấy ID tự tăng thực tế `#ESC-{id}`).
  - Khởi tạo đơn hàng `TourOrder` ở trạng thái `deposit_pending` với cọc 50%, ngày khởi hành và tổng giá tour.
  - Cập nhật phiên hội thoại sang `HumanActive`.

### 3.4 API Layer: `EscalationsController`
- `GET /api/v1/admin/escalations`: Truy vấn danh sách có hỗ trợ lọc status và search.
- `POST /api/v1/admin/escalations/{id}/contacting`: Tiếp nhận cuộc gọi.
- `POST /api/v1/admin/escalations/{id}/approve`: Duyệt chiết khấu/ưu đãi.
- `POST /api/v1/admin/escalations/{id}/close`: Đóng ticket.

### 3.5 Frontend: Kết Nối Real-time API
- Vị trí: `tour-agent-frontend/src/pages/AdminEscalationsPage.tsx`.
- Thay thế hoàn toàn mock tĩnh bằng `fetch('/api/v1/admin/escalations')`.
- Hỗ trợ thao tác cập nhật trạng thái trực tiếp trên giao diện với phản hồi tức thì.

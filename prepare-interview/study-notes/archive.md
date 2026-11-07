## phân biệt tcp socket và websocket

TCP socket là khái niệm gốc của hệ điều hành. bất cứ kết nối mạng nào trên đời từ HTTP, database connection, SSH thì bên dưới đều là TCP Socket. Hầu hết các giao thức ứng dụng phổ biến chạy trên TCP socket. Còn Websocket là 1 giao thức cấp application (application protocol) chạy trên nền TCP socket, dùng để làm chat real time 2 chiều

TCP socket đầy đủ sẽ bao gồm IP Address + Port. còn TCP Connection sẽ cần 1 cặp TCP socket để có được nguồn và đích, có thể xem như 1 đường ống dẫn nước

## Em dùng AI như thế nào trong SDLC?

SDLC gồm: planning & requirement analysis >>> Defining requirement >>> Design >>> Development >>> Testing >>> Deployment & maintenance

khách yêu cầu: tôi muốn tạo agent tư vấn tour. tôi muốn có agent tư để giảm thời gian chờ của khách, có mặt 24/24 khi khách cần. tôi muốn 3 tháng sau sẽ có để dùng. chi phí bao nhiêu

* planning: lấy nỗi đau của khách và xác định giới hạn để giải quyết
* defining: xác định chức năng và phạm vi phải làm gì?
* design: làm theo cách nào? cần phải thiết kế kiến trúc, luồng chạy
* development: bắt đầu xây
* testing: kiểm tra chạy đúng ko
* deployment & maintenance: deploy để chạy thật và giữ cho chạy tốt

## phân biệt các cấp độ

| Cấp độ                         | Là gì                                                       | Phạm vi                 | Ví dụ                                 |
| --------------------------------- | ------------------------------------------------------------- | ------------------------ | --------------------------------------- |
| **Principle**(nguyên tắc) | Quy tắc tư duy                                              | Method/class             | CQS, SRP                                |
| **Design pattern**          | Lời giải lặp lại cho cách các class/object tương tác | Vài class               | **Mediator**, Decorator, Strategy |
| **Architectural pattern**   | Cách tổ chức cả ứng dụng hoặc hệ thống               | Module, tầng, DB        | **CQRS**, Layered, Microservices  |
| **Library**(thư viện)     | Code có sẵn hiện thực một pattern                        | Dependency trong project | **MediatR**, Wolverine            |

## Thuật ngữ Native là gì? Tại sao nên dùng Native hơn third-party khi có thể?

**Thuật ngữ Native**: Là những tính năng do chính Microsoft / .NET cung cấp sẵn mà không cần cài thêm bất kỳ package của bên thứ ba nào

Ưu điểm khi dùng native là:

- không sợ xung đột version package khi nâng cấp các bản .NET mới
- Hạn chế rủ ro về license: không lo 1 ngày đẹp trời tác giá đổi license thu phí hoặc bỏ rơi dự án ko maintain
- có hiệu năng cao nhất: ví dụ như MediatR phải dùng reflection sẽ chậm hơn so với native gọi trực tiếp




## 1️⃣ 4-Layer Structure & Dependency Rule

### Sơ đồ phụ thuộc (chỉ đi vào trong)

```
Api  ──►  Application  ──►  Domain
 │                             ▲
 └──►  Infrastructure  ────────┘
```

### Ẩn dụ để nhớ

> **Não** (Domain) không biết cơ thể có gì → không import gì cả
> **Thần kinh** (Application) điều phối nhưng không tự làm
> **Tay chân** (Infrastructure) làm việc cụ thể theo lệnh thần kinh
> **Miệng** (Api) nhận yêu cầu từ bên ngoài, ra lệnh cho bên trong

---

## 🆕 Cập Nhật Mới (Ghi chú chuyên sâu phản biện phỏng vấn)

### 1. "Clean Architecture Tinh Giản" là gì?

- **Vấn đề của Clean Architecture truyền thống**: Thường bị over-engineering khi tạo quá nhiều lớp trung gian (mỗi entity có Generic Repository, UnitOfWork, MediatR Commands/Queries, AutoMapper Profile, Handler). Một thay đổi nhỏ phải sửa 8-10 file.
- **Tinh giản (Pragmatic)**:
  - **Giữ cốt lõi**: Giữ nghiêm ngặt hướng phụ thuộc một chiều (Domain <- Application <- Infrastructure <- Api).
  - **Cắt bỏ phần thừa**: Không Generic Repository (dùng trực tiếp EF Core qua `IAppDbContext`), Không MediatR (inject Handler trực tiếp), Không AutoMapper (dùng Extension Methods).

### 2. Cô lập Logic Nghiệp Vụ và Logic AI trong tầng Application

- **Vị trí**: Cùng nằm trong `TourAgent.Application` nhưng tách biệt folder:
  - `Features/`: Chứa nghiệp vụ du lịch thuần túy (Search, GetDetails). Hoàn toàn **không biết** về sự tồn tại của AI.
  - `Agent/`: Chứa điều phối LLM (`AgentLoop`), System Prompts, Tool Definitions.
- **Cơ chế**: AI Agent gọi vào `Features` qua Tool giống như một HTTP Controller gọi. Tắt AI thì core business vẫn hoạt động 100% nguyên vẹn.

### 3. So Sánh Kiến Trúc: SME (TourAgent) vs Enterprise

| Tiêu chí              | SME (TourAgent hiện tại)              | Enterprise (Hệ thống lớn)                                     |
| ----------------------- | --------------------------------------- | ---------------------------------------------------------------- |
| **Cấu trúc**    | Modular Monolith (4 projects)           | Microservices / Distributed Domains (DDD)                        |
| **Message Queue** | In-memory `System.Threading.Channels` | Distributed Broker: RabbitMQ, Apache Kafka, Azure Service Bus    |
| **Database**      | 1 DB PostgreSQL                         | CQRS: Read DB (Elasticsearch/Replicas) tách riêng Write DB     |
| **AI Gateway**    | Gọi API trực tiếp qua `HttpClient` | LiteLLM / Azure APIM Gateway (caching, rate limit, multi-tenant) |
| **Auth**          | Simple JWT / API Key                    | OAuth2 / OIDC (Keycloak, Azure Entra ID), RBAC/ABAC              |
| **Telemetry**     | Bảng audit `LlmCall` trong DB        | OpenTelemetry, Jaeger, Datadog / ELK Stack                       |

### 4. Handler Trực Tiếp vs MediatR

- **Bản chất MediatR**: In-process Message Bus. Dùng Reflection/Service Locator để tìm Handler tại runtime.
- **Hạn chế của MediatR**: Khó debug (không F12 được), che giấu dependencies, overhead runtime.
- **Bù đắp tính năng của MediatR**:
  - Tính năng mạnh nhất của MediatR là Pipeline Behaviors (Logging, Validation).
  - Trong ASP.NET Core hiện đại (.NET 7/8/10), ta bù đắp bằng **`EndpointFilter`** native của Minimal API (`.AddEndpointFilter<ValidationFilter>()`), vừa không cần thư viện ngoài vừa chạy nhanh hơn.

### 5. Cơ Chế Hoạt Động của NetArchTest.Rules

- **Chạy khi nào**: Là **Unit Test thông thường**, chạy trong **vài chục mili-giây** ngay trên local qua `dotnet test` hoặc IDE Test Explorer (không phải đợi đến CI/CD).
- **Cơ chế**: Dùng Reflection quét metadata trong file compiled `.dll`. Đọc toàn bộ class/method xem có tham chiếu sang namespace bị cấm hay không. Nếu vi phạm, test lập tức fail đỏ đèn.

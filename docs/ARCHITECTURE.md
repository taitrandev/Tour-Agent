# ARCHITECTURE.md — Kiến Trúc Tổng Thể TourAgent

> 📌 **Chi tiết toàn bộ hành trình tư duy, phản biện kỹ thuật và quyết định kiến trúc**: Xem tại [MIGRATION JOURNEY](journey/README.md).

## 1. Triết Lý Thiết Kế: Clean Architecture Tinh Giản (Pragmatic Clean Architecture)

Hệ thống được tổ chức theo 4 layers theo đúng chiều phụ thuộc một chiều (Dependency Inversion Principle):

```
┌─────────────────────────────────────────────────────────────┐
│                    TourAgent.Api                            │
│  (Minimal APIs, Scalar Docs, Middleware, Program.cs)        │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
               ▼                               ▼
┌──────────────────────────────┐ ┌─────────────────────────────┐
│   TourAgent.Application      │ │  TourAgent.Infrastructure   │
│  (Features, DTOs, Handlers,  │ │  (EF Core DbContext,        │
│   Agent Tools, Abstractions) │ │   DeepSeek LLM Client,      │
└──────────────┬───────────────┘ │   Background Channels)      │
               │                 └─────────────┬───────────────┘
               │                               │
               ▼                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    TourAgent.Domain                         │
│  (Entities, Value Objects, Result<T>, Error, Rules)         │
└─────────────────────────────────────────────────────────────┘
```

### Các nguyên tắc cốt lõi:
1. **Domain là trung tâm**: Hoàn toàn độc lập, không tham chiếu tới EF Core, ASP.NET hay bất kỳ thư viện bên thứ 3 nào.
2. **Application định nghĩa hợp đồng**: Chứa use cases và interface (`IAppDbContext`, `ILlmClient`).
3. **Infrastructure cài đặt chi tiết**: Cài đặt interface từ Application (EF Core, kết nối API LLM).
4. **Api tiếp nhận request**: Gọi trực tiếp use case handler, trả về HTTP status code phù hợp. Không chứa business logic.
5. **NetArchTest bảo vệ ranh giới**: Bất kỳ vi phạm nào về việc import chéo giữa các tầng sẽ bị Unit Test bắt tại compile-time và CI/CD.

---

## 2. Tổ Chức Feature Folders Trong Application Layer

Thay vì chia theo các thư mục kỹ thuật ngang (Commands, Queries, DTOs...), Application layer tổ chức theo **Vertical Feature Folders**:

```
TourAgent.Application/
├── Abstractions/
│   ├── IAppDbContext.cs
│   └── ILlmClient.cs
├── Common/
│   ├── ICommandHandler.cs
│   └── IQueryHandler.cs
├── Features/
│   ├── Tours/
│   │   └── SearchTours/
│   │       ├── SearchToursQuery.cs
│   │       ├── SearchToursHandler.cs
│   │       ├── SearchToursResponse.cs
│   │       └── SearchToursValidator.cs
│   └── Conversations/
│       └── HandleIncomingMessage/
│           ├── HandleIncomingMessageCommand.cs
│           └── HandleIncomingMessageHandler.cs
└── Agent/
    ├── Tools/              # 5 Travel Tools
    └── Prompts/            # System Prompts có version
```

Cách tổ chức này giúp:
- Khi phát triển hoặc sửa một tính năng, toàn bộ code liên quan nằm trong cùng 1 thư mục.
- Trực quan, dễ review code, dễ bảo trì và hạn chế tối đa context switching.

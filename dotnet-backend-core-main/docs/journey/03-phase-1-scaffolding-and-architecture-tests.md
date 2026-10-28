# 🧭 Hành Trình Chuyển Đổi: Kỳ 3 — Dựng Khung Nền Tảng & Khóa Ranh Giới Kiến Trúc

> **Mục tiêu tài liệu**: Ghi lại quy trình dựng khung Solution .NET 10 bằng CLI chuẩn xác, thiết lập chính sách biên dịch nghiêm ngặt (`Directory.Build.props`), và xây dựng lớp phòng thủ kiến trúc tự động bằng `NetArchTest.Rules`.

---

## 1. 🏗️ Nguyên Tắc Dựng Khung: Dùng `dotnet` CLI, Không Phụ Thuộc Template AI

### Nhận thức quan trọng:
- Nhiều lập trình viên thường để AI tự do tạo file dự án hoặc copy các template cồng kềnh trên mạng (như Jason Taylor hay Ardalis). Điều này thường dẫn đến:
  - Bị cài package thừa không cần thiết.
  - Sử dụng package phiên bản cũ hoặc cú pháp lỗi thời.
  - Cấu trúc phức tạp vượt quá nhu cầu thực tế.
- **Giải pháp của tôi**: Tự điều phối script `dotnet` CLI chuẩn xác từng lệnh một trên nền **.NET 10 (SDK 10.0.401)**:

```bash
# Solution & Source Projects
dotnet new sln -n TourAgent
dotnet new classlib -n TourAgent.Domain         -o src/TourAgent.Domain         -f net10.0
dotnet new classlib -n TourAgent.Application    -o src/TourAgent.Application    -f net10.0
dotnet new classlib -n TourAgent.Infrastructure -o src/TourAgent.Infrastructure -f net10.0
dotnet new webapi   -n TourAgent.Api            -o src/TourAgent.Api            -f net10.0

# Test Projects
dotnet new xunit -n TourAgent.Domain.UnitTests      -o tests/TourAgent.Domain.UnitTests      -f net10.0
dotnet new xunit -n TourAgent.Application.UnitTests -o tests/TourAgent.Application.UnitTests -f net10.0
dotnet new xunit -n TourAgent.ArchitectureTests     -o tests/TourAgent.ArchitectureTests     -f net10.0
```

---

## 2. 🛡️ Chuẩn Hóa Compiler Toàn Dự Án: `Directory.Build.props`

Để đảm bảo toàn bộ Solution tuân thủ tiêu chuẩn chất lượng cao nhất ngay từ ngày đầu tiên, file [Directory.Build.props](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/Directory.Build.props) được tạo ngay tại thư mục gốc:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### Ý nghĩa kỹ thuật:
- `<Nullable>enable</Nullable>`: Yêu cầu tường minh về khả năng null của dữ liệu, loại bỏ lỗi kinh điển `NullReferenceException`.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`: **Chính sách không khoan nhượng với cảnh báo** (Zero Warning Policy). Bất kỳ biến không dùng, thiếu nullable check hay thiếu type-hint đều bị coi là lỗi biên dịch và làm fail build ngay lập tức.

---

## 3. 🔐 "Vũ Khí Bí Mật": Architecture Tests Với `NetArchTest.Rules`

Trong các dự án phần mềm, ranh giới kiến trúc (Layer Boundaries) thường bị xói mòn theo thời gian do các developer (hoặc AI coding assistants) vô tình import chéo thư viện (ví dụ: tầng `Domain` lại đi import `Microsoft.EntityFrameworkCore` hoặc gọi `Infrastructure`).

Tôi đã cài đặt package **`NetArchTest.Rules`** và thiết lập bộ kiểm thử tự động tại [LayerTests.cs](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/tests/TourAgent.ArchitectureTests/LayerTests.cs):

```csharp
public class LayerTests
{
    private const string DomainNamespace = "TourAgent.Domain";
    private const string ApplicationNamespace = "TourAgent.Application";
    private const string InfrastructureNamespace = "TourAgent.Infrastructure";
    private const string ApiNamespace = "TourAgent.Api";

    [Fact]
    public void Domain_ShouldNot_HaveOutwardDependencies()
    {
        var domainAssembly = typeof(TourAgent.Domain.AssemblyMarker).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain không được phép phụ thuộc vào Application, Infrastructure, Api hoặc EF Core.");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_InfrastructureOrApi()
    {
        var applicationAssembly = typeof(TourAgent.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Application không được phép phụ thuộc vào Infrastructure hoặc Api.");
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var infrastructureAssembly = typeof(TourAgent.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Infrastructure không được phép phụ thuộc vào Api.");
    }
}
```

### Kết quả:
Chạy `dotnet test` tự động bắt lỗi ngay trong quá trình CI/CD. Cả 3 bài test kiến trúc đều **PASSED (Xanh 100%)**.

---

## 4. 🧠 Bộ Nhớ & Quy Tắc Vận Hành: `AGENTS.md` & `PROGRESS.md`

Để AI Assistant không bị "mất trí nhớ" giữa các chat session và tuân thủ kỷ luật nghiêm ngặt:
- [AGENTS.md](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/AGENTS.md): Bản tuyên ngôn luật lệ:
  - Bắt buộc viết XML doc comment cho mọi hàm.
  - Cấm tự ý cài package ngoài danh mục cho phép.
  - Cấm dùng MediatR, AutoMapper.
  - Cấm bịa giá tour / ngày bay (Anti-hallucination).
- [PROGRESS.md](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/docs/PROGRESS.md): Bảng theo dõi tiến độ chi tiết từng giai đoạn, lưu vết các quyết định kỹ thuật đã chốt.

---

## 🎤 Góc Nhìn Phỏng Vấn (Interview Talking Points)

> **Khi nhà tuyển dụng hỏi**: *"Làm thế nào bạn đảm bảo cả team (hoặc AI coding tools) tuân thủ đúng nguyên tắc Dependency Inversion của Clean Architecture mà không phá vỡ cấu trúc tầng sau vài tháng phát triển?"*

**Câu trả lời chuẩn mực**:
> *"Tôi không chỉ dựa vào quy tắc trên giấy hay code review thủ công. Tôi xây dựng một dự án test riêng biệt là `TourAgent.ArchitectureTests` sử dụng thư viện `NetArchTest.Rules`. Bộ test này kiểm tra metadata phản xạ (Reflection/IL analysis) của từng Assembly. Nếu bất kỳ ai cố tình reference EF Core vào Domain, hoặc gọi Infrastructure từ Application, Unit Test kiến trúc sẽ đỏ ngay trên máy dev và chặn merge trên GitHub Actions CI pipeline."*

using NetArchTest.Rules;
using Xunit;

namespace TourAgent.ArchitectureTests;

/// <summary>
/// Các bài kiểm tra kiến trúc để đảm bảo tính toàn vẹn của Clean Architecture.
/// Các ranh giới tầng (Layer Boundaries) được bảo vệ tự động bằng NetArchTest.
/// </summary>
public class LayerTests
{
    private const string DomainNamespace = "TourAgent.Domain";
    private const string ApplicationNamespace = "TourAgent.Application";
    private const string InfrastructureNamespace = "TourAgent.Infrastructure";
    private const string ApiNamespace = "TourAgent.Api";

    /// <summary>
    /// Kiểm tra tầng Domain không được phụ thuộc vào bất kỳ tầng bên ngoài nào hoặc EF Core.
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_HaveOutwardDependencies()
    {
        // Arrange
        var domainAssembly = typeof(TourAgent.Domain.AssemblyMarker).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Domain không được phép phụ thuộc vào Application, Infrastructure, Api hoặc EF Core.");
    }

    /// <summary>
    /// Kiểm tra tầng Application không được phụ thuộc vào tầng Infrastructure hoặc Api.
    /// </summary>
    [Fact]
    public void Application_ShouldNot_DependOn_InfrastructureOrApi()
    {
        // Arrange
        var applicationAssembly = typeof(TourAgent.Application.AssemblyMarker).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Application không được phép phụ thuộc vào Infrastructure hoặc Api.");
    }

    /// <summary>
    /// Kiểm tra tầng Infrastructure không được phụ thuộc vào tầng Api.
    /// </summary>
    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        // Arrange
        var infrastructureAssembly = typeof(TourAgent.Infrastructure.AssemblyMarker).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Infrastructure không được phép phụ thuộc vào Api.");
    }

    [Fact]
    public void Controllers_ShouldNot_DependOn_Infrastructure()
    {
        // 1. Lấy assembly của Api
        var apiAssembly = typeof(TourAgent.Api.Controllers.TravelsController).Assembly;

        // 2. Chỉ lọc ra các class là Controller (kế thừa ControllerBase hoặc có hậu tố "Controller")
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("TourAgent.Api.Controllers") // Hoặc .Inherit(typeof(ControllerBase))
            .ShouldNot()
            .HaveDependencyOn("TourAgent.Infrastructure")
            .GetResult();

        // 3. Assert: Nếu Controller nào "using TourAgent.Infrastructure" -> Báo lỗi đỏ đèn ngay!
        Assert.True(result.IsSuccessful,

            $"Controller không được phép phụ thuộc trực tiếp vào Infrastructure: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

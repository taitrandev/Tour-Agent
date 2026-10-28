using TourAgent.Application.Agent.Harness.State;
using TourAgent.Application.Agent.Harness.StatusBar;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent.Harness;

/// <summary>
/// Các bài kiểm tra đơn vị cho Kỹ thuật G2: General Agent Status Bar Framework (State Reflection Injection).
/// </summary>
public class StatusBarBuilderTests
{
    /// <summary>
    /// Giả lập Provider cung cấp thông tin ngữ cảnh nghiệp vụ Tour vào status bar.
    /// </summary>
    private sealed class MockTourStatusProvider : IAgentStatusProvider
    {
        /// <summary>
        /// Kiểm tra hỗ trợ domain 'travel'.
        /// </summary>
        public bool Supports(string domain) => domain == "travel";

        /// <summary>
        /// Tạo dữ liệu phản xạ trạng thái nghiệp vụ.
        /// </summary>
        public IReadOnlyDictionary<string, object> ProvideStatusBarData(AgentState state)
        {
            return new Dictionary<string, object>
            {
                ["active_tour"] = "JP-01 Tokyo 6N5D"
            };
        }
    }

    /// <summary>
    /// Kiểm tra thanh trạng thái render đúng cấu trúc thẻ XML chuẩn và hiển thị đầy đủ ngân sách và trạng thái.
    /// </summary>
    [Fact]
    public void Build_ShouldContainStandardXmlElements()
    {
        // Arrange
        var builder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var state = new AgentState
        {
            CurrentGoal = "Hỗ trợ khách đặt tour",
            WorkflowStatus = "booking_flow"
        };
        state.ConfirmedFacts["customer_phone"] = "0909123456";

        // Act
        var xml = builder.Build(state);

        // Assert
        Assert.Contains("<agent_status", xml);
        Assert.Contains("</agent_status>", xml);
        Assert.Contains("<current_goal>Hỗ trợ khách đặt tour</current_goal>", xml);
        Assert.Contains("<workflow_status>booking_flow</workflow_status>", xml);
        Assert.Contains("<fact key=\"customer_phone\">0909123456</fact>", xml);
    }

    /// <summary>
    /// Kiểm tra cơ chế mã hóa ký tự đặc biệt (XML escaping) bảo vệ thẻ XML không bị hỏng khi dữ liệu chứa ký tự đặc biệt.
    /// </summary>
    [Fact]
    public void Build_WhenDataContainsSpecialCharacters_ShouldEscapeProperly()
    {
        // Arrange
        var builder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var state = new AgentState
        {
            CurrentGoal = "Khách yêu cầu giá < 10 triệu & có ăn uống 'cao cấp' \"5 sao\""
        };
        state.ConfirmedFacts["note"] = "Tom & Jerry > Mickey";

        // Act
        var xml = builder.Build(state);

        // Assert
        Assert.DoesNotContain("Tom & Jerry", xml);
        Assert.Contains("Tom &amp; Jerry", xml);
        Assert.Contains("&lt; 10 triệu", xml);
        Assert.Contains("&amp; có ăn uống", xml);
    }

    /// <summary>
    /// Kiểm tra các IAgentStatusProvider được tích hợp sẽ tiêm nội dung vào phân đoạn domain_context.
    /// </summary>
    [Fact]
    public void Build_WithCustomStatusProvider_ShouldIncludeDomainContext()
    {
        // Arrange
        var providers = new IAgentStatusProvider[] { new MockTourStatusProvider() };
        var builder = new StatusBarBuilder(providers);
        var state = new AgentState();

        // Act
        var xml = builder.Build(state, domain: "travel");

        // Assert
        Assert.Contains("<domain_context domain=\"travel\">", xml);
        Assert.Contains("<property key=\"active_tour\">JP-01 Tokyo 6N5D</property>", xml);
        Assert.Contains("</domain_context>", xml);
    }
}

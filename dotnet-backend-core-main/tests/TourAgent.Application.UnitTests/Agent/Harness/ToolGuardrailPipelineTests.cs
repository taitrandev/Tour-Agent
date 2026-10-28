using System.Text.Json;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.Guardrails;
using TourAgent.Application.Agent.Harness.State;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent.Harness;

/// <summary>
/// Các bài kiểm tra đơn vị cho Kỹ thuật G3: Tool Guardrail Pipeline (3 pha Constrain -> Execute -> Verify & Correct).
/// </summary>
public class ToolGuardrailPipelineTests
{
    /// <summary>
    /// Giả lập công cụ phục vụ kiểm thử Pipeline.
    /// </summary>
    private sealed class EchoTool : IAgentTool
    {
        private static readonly JsonDocument Schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "message": { "type": "string", "description": "Thông điệp cần in ra" }
            },
            "required": ["message"]
        }
        """);

        /// <summary>
        /// Tên công cụ.
        /// </summary>
        public string Name => "echo_tool";

        /// <summary>
        /// Mô tả công cụ.
        /// </summary>
        public string Description => "Lặp lại thông điệp đầu vào.";

        /// <summary>
        /// Bản đặc tả JSON Schema.
        /// </summary>
        public JsonDocument ParametersSchema => Schema;

        /// <summary>
        /// Thực thi công cụ.
        /// </summary>
        public Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("{\"status\":\"success\",\"echo\":\"hello\"}");
        }
    }

    /// <summary>
    /// Giả lập công cụ luôn ném ngoại lệ đột ngột để kiểm tra tầng Execute bảo vệ.
    /// </summary>
    private sealed class BuggyTool : IAgentTool
    {
        private static readonly JsonDocument Schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {}
        }
        """);

        /// <summary>
        /// Tên công cụ lỗi.
        /// </summary>
        public string Name => "buggy_tool";

        /// <summary>
        /// Mô tả công cụ lỗi.
        /// </summary>
        public string Description => "Công cụ luôn ném ngoại lệ hệ thống.";

        /// <summary>
        /// Bản đặc tả JSON Schema.
        /// </summary>
        public JsonDocument ParametersSchema => Schema;

        /// <summary>
        /// Thực thi cố ý gây ra ngoại lệ.
        /// </summary>
        public Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Lỗi kết nối cơ sở dữ liệu nội bộ!");
        }
    }

    /// <summary>
    /// Kiểm tra khi gọi tên công cụ không tồn tại, pipeline bắt lỗi và đề xuất danh sách công cụ hợp lệ.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenToolNotFound_ShouldReturnRecoveryHint()
    {
        // Arrange
        var tools = new IAgentTool[] { new EchoTool() };
        var pipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());
        var state = new AgentState();

        // Act
        var result = await pipeline.ExecuteAsync("unknown_tool", "{}", state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("tool_lookup", result.GuardrailPhase);
        Assert.Contains("không tồn tại", result.ResultJson);
    }

    /// <summary>
    /// Kiểm tra Pha 1 (Constrain): Khi tham số JSON bị sai cú pháp, chặn ngay lập tức và hướng dẫn AI tự sửa.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenJsonIsMalformed_ShouldInterceptInConstrainPhase()
    {
        // Arrange
        var tools = new IAgentTool[] { new EchoTool() };
        var pipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());
        var state = new AgentState();

        // Act - Truyền chuỗi JSON thiếu dấu ngoặc đóng
        var result = await pipeline.ExecuteAsync("echo_tool", "{\"message\": \"test\"", state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("constrain", result.GuardrailPhase);
        Assert.Contains("pre_condition_failed", result.ResultJson);
        Assert.Contains("recovery_hint", result.ResultJson);
    }

    /// <summary>
    /// Kiểm tra Pha 1 (Constrain): Khi thiếu trường tham số bắt buộc (required), chặn trước khi gọi công cụ.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenMissingRequiredParameter_ShouldInterceptInConstrainPhase()
    {
        // Arrange
        var tools = new IAgentTool[] { new EchoTool() };
        var pipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());
        var state = new AgentState();

        // Act - Truyền JSON rỗng trong khi tool yêu cầu trường 'message'
        var result = await pipeline.ExecuteAsync("echo_tool", "{}", state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("constrain", result.GuardrailPhase);
        Assert.Contains("message", result.ResultJson);
        Assert.Contains("recovery_hint", result.ResultJson);
    }

    /// <summary>
    /// Kiểm tra Pha 2 (Execute): Khi công cụ bên trong bị crash hoặc ném ngoại lệ, pipeline bọc lại an toàn không làm sập ứng dụng.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenToolThrowsException_ShouldCatchGracefully()
    {
        // Arrange
        var tools = new IAgentTool[] { new BuggyTool() };
        var pipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());
        var state = new AgentState();

        // Act
        var result = await pipeline.ExecuteAsync("buggy_tool", "{}", state);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Lỗi kết nối cơ sở dữ liệu nội bộ", result.ResultJson);
        Assert.Contains("recovery_hint", result.ResultJson);
    }

    /// <summary>
    /// Kiểm tra chu trình thành công trọn vẹn qua 3 pha khi đầu vào hợp lệ.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenValidInput_ShouldSucceed()
    {
        // Arrange
        var tools = new IAgentTool[] { new EchoTool() };
        var pipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());
        var state = new AgentState();

        // Act
        var result = await pipeline.ExecuteAsync("echo_tool", "{\"message\": \"Xin chào\"}", state);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("\"status\":\"success\"", result.ResultJson);
    }
}

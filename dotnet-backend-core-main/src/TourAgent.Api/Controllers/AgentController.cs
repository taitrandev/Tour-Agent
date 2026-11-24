using Microsoft.AspNetCore.Mvc;
using TourAgent.Api.Contracts.Agent;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller cung cấp các Endpoints tương tác với AI Agent Tư vấn Du lịch và AI Agent Commander điều hành.
/// Hỗ trợ cả phản hồi JSON trọn gói và truyền phát dữ liệu thời gian thực (SSE Streaming).
/// Phân tách rõ ràng giữa Public Consultant Route (Read-Only) và Admin Commander Route (Read + Write).
/// </summary>
[ApiController]
[Route("api/v1/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IAgentLoop _agentLoop;

    /// <summary>
    /// Khởi tạo AgentController với IAgentLoop orchestrator.
    /// </summary>
    /// <param name="agentLoop">Trình điều phối chu trình suy luận đa lượt của Agent.</param>
    public AgentController(IAgentLoop agentLoop)
    {
        _agentLoop = agentLoop;
    }

    /// <summary>
    /// Gửi câu hỏi cho AI Agent tư vấn và nhận câu trả lời hoàn chỉnh sau chu trình gọi công cụ (Synchronous Request-Response).
    /// Cưỡng chế phân quyền CustomerConsultant, chỉ cho phép tra cứu và thu thập lead.
    /// </summary>
    /// <param name="request">Nội dung câu hỏi và ngữ cảnh tour của khách.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ HTTP.</param>
    /// <returns>Kết quả phản hồi của AI kèm thống kê token và công cụ đã kích hoạt.</returns>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AgentChatResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChatAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Nội dung tin nhắn 'Message' không được để trống." });
        }

        var convId = request.ConversationId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var input = new AgentChatInput(
            ConversationId: convId,
            UserMessage: request.Message,
            CurrentTravelId: request.CurrentTravelId,
            CurrentTravelTitle: request.CurrentTravelTitle,
            Country: request.Country,
            Role: AgentRole.CustomerConsultant);

        var result = await _agentLoop.RunAsync(input, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Truyền phát trực tiếp câu trả lời của AI tư vấn theo chuẩn Server-Sent Events (SSE).
    /// Cưỡng chế phân quyền CustomerConsultant.
    /// </summary>
    /// <param name="request">Nội dung câu hỏi và ngữ cảnh tour của khách.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ HTTP.</param>
    /// <returns>Luồng bất đồng bộ phát ra các sự kiện AgentStreamEvent.</returns>
    [HttpPost("chat/stream")]
    [Produces("text/event-stream")]
    public async IAsyncEnumerable<AgentStreamEvent> StreamChatAsync(
        [FromBody] ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var convId = request.ConversationId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var input = new AgentChatInput(
            ConversationId: convId,
            UserMessage: request.Message,
            CurrentTravelId: request.CurrentTravelId,
            CurrentTravelTitle: request.CurrentTravelTitle,
            Country: request.Country,
            Role: AgentRole.CustomerConsultant);

        await foreach (var evt in _agentLoop.StreamRunAsync(input, cancellationToken))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Gửi câu lệnh điều hành cho AI Agent Commander và nhận câu trả lời sau chu trình gọi công cụ (Sync Request-Response).
    /// Endpoint dành riêng cho khu vực quản trị (/admin), được cấp quyền thực thi các công cụ cập nhật (Write Tools).
    /// </summary>
    /// <param name="request">Nội dung câu lệnh của Quản trị viên và ngữ cảnh màn hình.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ HTTP.</param>
    /// <returns>Kết quả phản hồi của AI Commander kèm danh sách công cụ đã kích hoạt.</returns>
    [HttpPost("admin/chat")]
    [ProducesResponseType(typeof(AgentChatResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminChatAsync(
        [FromBody] AdminChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Nội dung tin nhắn 'Message' không được để trống." });
        }

        var convId = request.ConversationId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var input = new AgentChatInput(
            ConversationId: convId,
            UserMessage: request.Message,
            Role: AgentRole.AdminCommander,
            AdminUsername: request.AdminUsername,
            PageContext: request.PageContext);

        var result = await _agentLoop.RunAsync(input, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Truyền phát trực tiếp sự kiện suy luận và gọi công cụ điều hành của AI Agent Commander theo chuẩn SSE.
    /// Endpoint dành riêng cho khu vực quản trị (/admin).
    /// </summary>
    /// <param name="request">Nội dung câu lệnh của Quản trị viên và ngữ cảnh màn hình.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ HTTP.</param>
    /// <returns>Luồng bất đồng bộ phát ra các sự kiện AgentStreamEvent.</returns>
    [HttpPost("admin/chat/stream")]
    [Produces("text/event-stream")]
    public async IAsyncEnumerable<AgentStreamEvent> AdminStreamChatAsync(
        [FromBody] AdminChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var convId = request.ConversationId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var input = new AgentChatInput(
            ConversationId: convId,
            UserMessage: request.Message,
            Role: AgentRole.AdminCommander,
            AdminUsername: request.AdminUsername,
            PageContext: request.PageContext);

        await foreach (var evt in _agentLoop.StreamRunAsync(input, cancellationToken))
        {
            yield return evt;
        }
    }
}

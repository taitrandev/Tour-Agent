using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Agent.Contracts;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Yêu cầu gửi tin nhắn trò chuyện với AI Agent tư vấn du lịch.
/// </summary>
/// <param name="ConversationId">Mã định danh phiên hội thoại (nếu có, để trống hệ thống sẽ tự cấp mới).</param>
/// <param name="Message">Nội dung câu hỏi của khách hàng.</param>
/// <param name="CurrentTravelId">Mã tour khách hàng đang xem trên trang (nếu có).</param>
/// <param name="CurrentTravelTitle">Tiêu đề tour đang xem.</param>
/// <param name="Country">Quốc gia của tour đang xem.</param>
public sealed record ChatRequest(
    [property: JsonPropertyName("conversationId")] long? ConversationId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("currentTravelId")] long? CurrentTravelId = null,
    [property: JsonPropertyName("currentTravelTitle")] string? CurrentTravelTitle = null,
    [property: JsonPropertyName("country")] string? Country = null);

/// <summary>
/// Controller cung cấp các Endpoints tương tác với AI Agent Tư vấn Du lịch (DeepSeek V3 / Multi-Turn Loop).
/// Hỗ trợ cả phản hồi JSON trọn gói và truyền phát dữ liệu thời gian thực (SSE Streaming).
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
    /// Gửi câu hỏi cho AI Agent và nhận câu trả lời hoàn chỉnh sau chu trình gọi công cụ (Synchronous Request-Response).
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
            Country: request.Country);

        var result = await _agentLoop.RunAsync(input, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Truyền phát trực tiếp câu trả lời của AI và các sự kiện gọi công cụ theo chuẩn Server-Sent Events (SSE).
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
            Country: request.Country);

        await foreach (var evt in _agentLoop.StreamRunAsync(input, cancellationToken))
        {
            yield return evt;
        }
    }
}

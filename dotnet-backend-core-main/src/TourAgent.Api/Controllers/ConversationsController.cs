using Microsoft.AspNetCore.Mvc;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Conversations;
using TourAgent.Application.Features.Conversations.DeleteConversation;
using TourAgent.Application.Features.Conversations.GetConversationDetail;
using TourAgent.Application.Features.Conversations.GetConversations;
using TourAgent.Domain.Common;

namespace TourAgent.Api.Controllers;

/// <summary>
/// Controller cung cấp API giám sát và quản lý các cuộc hội thoại khách hàng (AI Observability & Inspector).
/// Hỗ trợ cả hai định dạng route: chuẩn module (/api/v1/agent/consultant/conversations) và chuẩn RESTful (/api/v1/conversations).
/// </summary>
[ApiController]
public sealed class ConversationsController : ControllerBase
{
    private readonly IQueryHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationSummaryDto>>> _getConversationsHandler;
    private readonly IQueryHandler<GetConversationDetailQuery, Result<ConversationDetailDto>> _getConversationDetailHandler;
    private readonly ICommandHandler<DeleteConversationCommand, Result<string>> _deleteConversationHandler;

    /// <summary>
    /// Khởi tạo ConversationsController với các handlers tương ứng từ tầng Application.
    /// </summary>
    /// <param name="getConversationsHandler">Handler lấy danh sách tóm tắt hội thoại.</param>
    /// <param name="getConversationDetailHandler">Handler lấy chi tiết tin nhắn và logs LLM.</param>
    /// <param name="deleteConversationHandler">Handler xóa hội thoại an toàn.</param>
    public ConversationsController(
        IQueryHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationSummaryDto>>> getConversationsHandler,
        IQueryHandler<GetConversationDetailQuery, Result<ConversationDetailDto>> getConversationDetailHandler,
        ICommandHandler<DeleteConversationCommand, Result<string>> deleteConversationHandler)
    {
        _getConversationsHandler = getConversationsHandler;
        _getConversationDetailHandler = getConversationDetailHandler;
        _deleteConversationHandler = deleteConversationHandler;
    }

    /// <summary>
    /// Lấy danh sách các cuộc hội thoại khách hàng kèm số lượng tin nhắn và lượt gọi LLM DeepSeek.
    /// </summary>
    /// <param name="channel">Lọc theo kênh (all, telegram, web_widget, zalo).</param>
    /// <param name="search">Tìm kiếm theo ID, ChannelUserId hoặc Channel.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Danh sách tóm tắt các cuộc hội thoại.</returns>
    [HttpGet("api/v1/agent/consultant/conversations")]
    [HttpGet("api/v1/conversations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationsAsync(
        [FromQuery] string? channel,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetConversationsQuery(channel, search);
        var result = await _getConversationsHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error.Description });
        }

        return Ok(new
        {
            success = true,
            data = result.Value
        });
    }

    /// <summary>
    /// Lấy thông tin chi tiết một cuộc hội thoại gồm toàn bộ lịch sử tin nhắn, nhật ký các lượt gọi LLM và System Prompt.
    /// </summary>
    /// <param name="id">Mã định danh cuộc hội thoại.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Chi tiết hội thoại, tin nhắn và lượt gọi LLM.</returns>
    [HttpGet("api/v1/agent/consultant/conversations/{id:long}")]
    [HttpGet("api/v1/conversations/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationDetailAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var query = new GetConversationDetailQuery(id);
        var result = await _getConversationDetailHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new
            {
                success = false,
                message = result.Error.Description
            });
        }

        var detail = result.Value;
        return Ok(new
        {
            success = true,
            conversation = detail.Conversation,
            messages = detail.Messages,
            llm_calls = detail.LlmCalls,
            system_prompt = detail.SystemPrompt
        });
    }

    /// <summary>
    /// Xóa một cuộc hội thoại cùng toàn bộ tin nhắn và nhật ký LLM liên quan.
    /// </summary>
    /// <param name="id">Mã định danh cuộc hội thoại cần xóa.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Thông báo kết quả xóa.</returns>
    [HttpDelete("api/v1/agent/consultant/conversations/{id:long}")]
    [HttpDelete("api/v1/conversations/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversationAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteConversationCommand(id);
        var result = await _deleteConversationHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new
            {
                success = false,
                message = result.Error.Description
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Value
        });
    }
}

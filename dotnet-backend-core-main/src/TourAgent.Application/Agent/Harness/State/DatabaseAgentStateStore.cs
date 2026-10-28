namespace TourAgent.Application.Agent.Harness.State;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Domain.Common;

/// <summary>
/// Cài đặt IAgentStateStore dựa trên cơ sở dữ liệu PostgreSQL (Database-backed State Store).
/// Trạng thái Agent được lưu trữ bên trong trường JSON metadata của bảng 'conversations'
/// dưới khóa 'agent_state', đảm bảo tính toàn vẹn dữ liệu và kiểm soát khóa lạc quan (OCC).
/// </summary>
public sealed class DatabaseAgentStateStore : IAgentStateStore
{
    private readonly IAppDbContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Khởi tạo State Store với DbContext.
    /// </summary>
    /// <param name="dbContext">Giao diện truy cập cơ sở dữ liệu.</param>
    public DatabaseAgentStateStore(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<AgentState> GetAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = _dbContext.Conversations.Local.FirstOrDefault(c => c.Id == conversationId)
            ?? await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return AgentState.CreateInitial(conversationId);
        }

        if (!string.IsNullOrWhiteSpace(conversation.MetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(conversation.MetadataJson);
                if (doc.RootElement.TryGetProperty("agent_state", out var stateElement))
                {
                    var parsed = JsonSerializer.Deserialize<AgentState>(stateElement.GetRawText(), JsonOptions);
                    if (parsed is not null)
                    {
                        parsed.ConversationId = conversationId;
                        return parsed;
                    }
                }
            }
            catch (JsonException)
            {
                // Sử dụng trạng thái mặc định nếu metadata JSON bị sai định dạng
            }
        }

        // Tự động khởi tạo trạng thái ban đầu và lưu vào CSDL
        var initialState = AgentState.CreateInitial(conversationId);
        await PersistStateToConversationAsync(conversation, initialState, cancellationToken);

        return initialState;
    }

    /// <inheritdoc />
    public async Task<Result<AgentState>> SaveAsync(
        long conversationId,
        AgentState state,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var conversation = _dbContext.Conversations.Local.FirstOrDefault(c => c.Id == conversationId)
            ?? await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<AgentState>(Error.NotFound("Conversation", conversationId));
        }

        var currentState = await GetAsync(conversationId, cancellationToken);

        // Kiểm tra khóa lạc quan (Optimistic Concurrency Control Check)
        if (currentState.Version != expectedVersion)
        {
            return Result.Failure<AgentState>(Error.Conflict(
                "AgentState.VersionConflict",
                $"Xung đột phiên bản trạng thái cho hội thoại #{conversationId}. Kỳ vọng version {expectedVersion}, nhưng hiện tại trong CSDL là {currentState.Version}."));
        }

        // Tăng version và cập nhật thời điểm UTC
        state.Version = expectedVersion + 1;
        state.UpdatedAtUtc = DateTime.UtcNow;

        await PersistStateToConversationAsync(conversation, state, cancellationToken);

        return Result.Success(state);
    }

    /// <inheritdoc />
    public async Task<AgentState> PatchAsync(
        long conversationId,
        Action<AgentState> updateAction,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var currentState = await GetAsync(conversationId, cancellationToken);
            var expectedVersion = currentState.Version;

            updateAction(currentState);

            var saveResult = await SaveAsync(conversationId, currentState, expectedVersion, cancellationToken);
            if (saveResult.IsSuccess)
            {
                return saveResult.Value;
            }

            if (attempt == maxRetries)
            {
                throw new InvalidOperationException($"Không thể cập nhật AgentState sau {maxRetries} lần thử do xung đột dữ liệu.");
            }

            // Nghỉ ngắn trước khi retry để giảm tắc nghẽn tranh chấp
            await Task.Delay(20 * attempt, cancellationToken);
        }

        return await GetAsync(conversationId, cancellationToken);
    }

    /// <summary>
    /// Ghi nhận đối tượng AgentState vào JSON metadata của Conversation và lưu vào cơ sở dữ liệu.
    /// </summary>
    /// <param name="conversation">Thực thể cuộc hội thoại.</param>
    /// <param name="state">Trạng thái Agent cần ghi.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    private async Task PersistStateToConversationAsync(
        TourAgent.Domain.Conversations.Conversation conversation,
        AgentState state,
        CancellationToken cancellationToken)
    {
        var metaDict = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(conversation.MetadataJson))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(conversation.MetadataJson, JsonOptions);
                if (existing is not null)
                {
                    metaDict = existing;
                }
            }
            catch (JsonException)
            {
            }
        }

        metaDict["agent_state"] = state;
        var serializedMeta = JsonSerializer.Serialize(metaDict, JsonOptions);

        conversation.UpdateMetadata(serializedMeta);

        // Lưu trực tiếp trường MetadataJson
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

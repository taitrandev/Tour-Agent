using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Domain.Conversations;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent.Harness;

/// <summary>
/// Các bài kiểm tra đơn vị cho Kỹ thuật G1: Agent State Store và kiểm soát tranh chấp OCC.
/// </summary>
public class AgentStateStoreTests
{
    /// <summary>
    /// Tạo DbContext in-memory độc lập cho từng ca kiểm thử.
    /// </summary>
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Kiểm tra khi hội thoại chưa có metadata trạng thái, store trả về AgentState mặc định với Version = 1.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenNoExistingState_ShouldReturnDefaultStateWithVersionOne()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var conversation = Conversation.Create("web", "user_test_01", id: 100L).Value;
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        var store = new DatabaseAgentStateStore(dbContext);

        // Act
        var state = await store.GetAsync(100L);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(1, state.Version);
        Assert.Equal("in_progress", state.WorkflowStatus);
        Assert.Empty(state.ConfirmedFacts);
    }

    /// <summary>
    /// Kiểm tra lưu trạng thái thành công khi version khớp, version tăng lên và metadata được cập nhật.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WhenVersionMatches_ShouldPersistAndIncrementVersion()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var conversation = Conversation.Create("web", "user_test_02", id: 200L).Value;
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        var store = new DatabaseAgentStateStore(dbContext);
        var state = await store.GetAsync(200L);
        state.CurrentGoal = "Tư vấn tour Nhật Bản";
        state.ConfirmedFacts["customer_phone"] = "0909123456";

        // Act
        var saveResult = await store.SaveAsync(200L, state, expectedVersion: 1);

        // Assert
        Assert.True(saveResult.IsSuccess);
        Assert.NotNull(saveResult.Value);
        Assert.Equal(2, saveResult.Value.Version);

        // Kiểm tra đọc lại từ DB
        var reloaded = await store.GetAsync(200L);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal("Tư vấn tour Nhật Bản", reloaded.CurrentGoal);
        Assert.Equal("0909123456", reloaded.ConfirmedFacts["customer_phone"]);
    }

    /// <summary>
    /// Kiểm tra phát hiện xung đột phiên bản (OCC Version Conflict) khi có tiến trình khác đã ghi đè trước.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WhenVersionConflictDetected_ShouldReturnConflictResult()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var conversation = Conversation.Create("web", "user_test_03", id: 300L).Value;
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        var store = new DatabaseAgentStateStore(dbContext);

        // Client A và Client B cùng đọc trạng thái Version = 1
        var stateA = await store.GetAsync(300L);
        var stateB = await store.GetAsync(300L);

        // Client A lưu trước thành công với expectedVersion = 1 -> DB nhảy lên Version = 2
        stateA.CurrentGoal = "Goal của Client A";
        var resultA = await store.SaveAsync(300L, stateA, expectedVersion: 1);
        Assert.True(resultA.IsSuccess);

        // Client B cố gắng lưu với expectedVersion = 1 cũ -> Bị phát hiện xung đột OCC vì DB đã là 2
        stateB.CurrentGoal = "Goal của Client B";
        var resultB = await store.SaveAsync(300L, stateB, expectedVersion: 1);

        // Assert
        Assert.True(resultB.IsFailure);
        Assert.Equal("AgentState.VersionConflict", resultB.Error.Code);
    }

    /// <summary>
    /// Kiểm tra PatchAsync tự động áp dụng lambda biến đổi và lưu trạng thái thành công.
    /// </summary>
    [Fact]
    public async Task PatchAsync_ShouldApplyMutationsAndPersist()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var conversation = Conversation.Create("web", "user_test_04", id: 400L).Value;
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        var store = new DatabaseAgentStateStore(dbContext);

        // Act
        var patchedState = await store.PatchAsync(400L, state =>
        {
            state.CurrentGoal = "Đặt cọc tour";
            state.ConfirmedFacts["pax"] = "4";
        });

        // Assert
        Assert.Equal(2, patchedState.Version);
        Assert.Equal("Đặt cọc tour", patchedState.CurrentGoal);
        Assert.Equal("4", patchedState.ConfirmedFacts["pax"]);
    }
}

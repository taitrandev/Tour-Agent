using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.Guardrails;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Application.Agent.Harness.StatusBar;
using TourAgent.Application.Agent.Services;
using TourAgent.Application.Agent.Tools;
using TourAgent.Application.UnitTests.Common;
using TourAgent.Domain.Conversations;
using TourAgent.Domain.Tours;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Các bài kiểm tra đơn vị cho AgentLoop - Trình điều phối chu trình suy luận đa lượt tự động (Multi-Turn).
/// </summary>
public class AgentLoopTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext Db, List<IAgentTool> Tools)> SetupContextAndToolsAsync()
    {
        var db = CreateInMemoryDbContext();

        var tour = Tour.Create(
            code: "JP-TOK-01",
            name: "Cung Đường Vàng Nhật Bản 6N5Đ",
            destination: "Tokyo - Kyoto",
            country: "Nhật Bản",
            departureCity: "TP.HCM",
            durationDays: 6,
            durationNights: 5,
            priceFrom: 35990000m,
            id: 1L).Value;

        db.Tours.Add(tour);

        var conv = Conversation.Create("web", "guest_test", id: 20L).Value;
        db.Conversations.Add(conv);

        await db.SaveChangesAsync();

        var tools = new List<IAgentTool>
        {
            new SearchTravelsTool(db),
            new CheckDeparturesTool(db),
            new GetTravelItineraryTool(db),
            new GetTravelPoliciesTool(db),
            new EscalateToHumanTool(db)
        };

        return (db, tools);
    }

    /// <summary>
    /// Hàm tiện ích khởi tạo AgentLoop đầy đủ các tầng bảo vệ Harness phục vụ kiểm thử.
    /// </summary>
    private static AgentLoop CreateAgentLoop(ILlmClient llmClient, AppDbContext dbContext, IEnumerable<IAgentTool> tools)
    {
        var stateStore = new DatabaseAgentStateStore(dbContext);
        var statusBarBuilder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var guardrailPipeline = new ToolGuardrailPipeline(
            tools,
            new SchemaToolValidator(),
            new DefaultPostConditionVerifier());

        return new AgentLoop(llmClient, dbContext, tools, stateStore, statusBarBuilder, guardrailPipeline);
    }

    /// <summary>
    /// Kiểm tra trường hợp AI trả lời trực tiếp trong 1 lượt (không cần kích hoạt tool).
    /// </summary>
    [Fact]
    public async Task RunAsync_DirectResponse_ShouldCompleteInSingleTurn()
    {
        // Arrange
        var (db, tools) = await SetupContextAndToolsAsync();
        await using (db)
        {
            var fakeLlm = new FakeLlmClient();
            fakeLlm.EnqueueTextResponse("Dạ em chào anh/chị, em có thể hỗ trợ anh/chị tìm kiếm tour du lịch nào ạ?");

            var loop = CreateAgentLoop(fakeLlm, db, tools);
            var input = new AgentChatInput(20L, "Chào em!");

            // Act
            var result = await loop.RunAsync(input);

            // Assert
            Assert.Equal(1, result.TurnsCount);
            Assert.Empty(result.ToolsExecuted);
            Assert.Contains("hỗ trợ anh/chị", result.ResponseText);

            var messages = await db.Messages.Where(m => m.ConversationId == 20L).ToListAsync();
            Assert.Equal(2, messages.Count); // 1 User + 1 Assistant
            Assert.Equal(MessageRole.User, messages[0].Role);
            Assert.Equal(MessageRole.Assistant, messages[1].Role);

            var llmCalls = await db.LlmCalls.Where(l => l.ConversationId == 20L).ToListAsync();
            Assert.Single(llmCalls);
        }
    }

    /// <summary>
    /// Kiểm tra chu trình đa lượt tự động: AI gọi công cụ ở lượt 1, sau đó tổng hợp câu trả lời ở lượt 2.
    /// </summary>
    [Fact]
    public async Task RunAsync_WithToolCalling_ShouldExecuteToolAndReturnSynthesizedResponse()
    {
        // Arrange
        var (db, tools) = await SetupContextAndToolsAsync();
        await using (db)
        {
            var fakeLlm = new FakeLlmClient();
            // Lượt 1: AI muốn gọi công cụ search_travels
            fakeLlm.EnqueueToolCallResponse("search_travels", "{\"country\": \"Nhật Bản\"}", "call_search_01");
            // Lượt 2: Sau khi có kết quả từ tool, AI trả lời khách hàng
            fakeLlm.EnqueueTextResponse("Dạ hiện bên em đang có tour Cung Đường Vàng Nhật Bản 6N5Đ khởi hành từ TP.HCM với giá từ 35.990.000 VNĐ ạ!");

            var loop = CreateAgentLoop(fakeLlm, db, tools);
            var input = new AgentChatInput(20L, "Bên em có tour Nhật Bản không?");

            // Act
            var result = await loop.RunAsync(input);

            // Assert
            Assert.Equal(2, result.TurnsCount);
            Assert.Single(result.ToolsExecuted);
            Assert.Equal("search_travels", result.ToolsExecuted[0]);
            Assert.Contains("35.990.000 VNĐ", result.ResponseText);

            // Kiểm tra tin nhắn trong DB: User -> Tool -> Assistant
            var messages = await db.Messages.Where(m => m.ConversationId == 20L).ToListAsync();
            Assert.Equal(3, messages.Count);
            Assert.Equal(MessageRole.User, messages[0].Role);
            Assert.Equal(MessageRole.Tool, messages[1].Role);
            Assert.Equal("search_travels", messages[1].ToolName);
            Assert.Equal(MessageRole.Assistant, messages[2].Role);

            // Kiểm tra bản ghi kiểm toán LlmCalls
            var llmCalls = await db.LlmCalls.Where(l => l.ConversationId == 20L).ToListAsync();
            Assert.Equal(2, llmCalls.Count);
        }
    }

    /// <summary>
    /// Kiểm tra cơ chế tự bảo vệ chống lặp vô hạn (Max Turns Guardrail).
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenToolCallsLoopEndlessly_ShouldHaltAtMaxTurns()
    {
        // Arrange
        var (db, tools) = await SetupContextAndToolsAsync();
        await using (db)
        {
            var fakeLlm = new FakeLlmClient();
            // Bơm liên tục 10 yêu cầu gọi tool
            for (int i = 0; i < 10; i++)
            {
                fakeLlm.EnqueueToolCallResponse("search_travels", "{}", $"call_loop_{i}");
            }

            var loop = CreateAgentLoop(fakeLlm, db, tools);
            var input = new AgentChatInput(20L, "Tìm tour");

            // Act
            var result = await loop.RunAsync(input);

            // Assert
            Assert.Equal(5, result.TurnsCount); // Dừng chính xác tại MaxTurns = 5
            Assert.NotEmpty(result.ResponseText); // Trả lời thông báo an toàn chứ không bị exception treo tiến trình
        }
    }

    /// <summary>
    /// Kiểm tra hệ thống tự động nhận diện và trích xuất số lượng khách (group_size) và SĐT vào ConfirmedFacts.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenUserMentionsGroupSizeAndPhone_ShouldExtractConfirmedFacts()
    {
        // Arrange
        var (db, tools) = await SetupContextAndToolsAsync();
        await using (db)
        {
            var fakeLlm = new FakeLlmClient();
            fakeLlm.EnqueueTextResponse("Dạ em đã ghi nhận đoàn 5 người của anh/chị ạ!");

            var loop = CreateAgentLoop(fakeLlm, db, tools);
            var input = new AgentChatInput(20L, "mình đi 5 người, đoàn 4 lớn 1 bé nhé, sđt 0909123456");

            // Act
            var result = await loop.RunAsync(input);

            // Assert
            var stateStore = new DatabaseAgentStateStore(db);
            var state = await stateStore.GetAsync(20L);

            Assert.Equal("0909123456", state.ConfirmedFacts["customer_phone"]);
            Assert.Equal("5 (4 lớn, 1 bé)", state.ConfirmedFacts["group_size"]);
            Assert.Equal("4", state.ConfirmedFacts["pax_adults"]);
            Assert.Equal("1", state.ConfirmedFacts["pax_children"]);
        }
    }

    /// <summary>
    /// Kiểm tra cơ chế phòng vệ chống ảo giác (Phantom Escalation Interceptor):
    /// Khi AI tự bịa ra mã phiếu #ESC-99 mà không gọi tool, Backend tự động kích hoạt tạo ticket thật vào DB.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenLlmHallucinatesTicket_ShouldAutoInterceptAndCreateRealTicket()
    {
        // Arrange
        var (db, tools) = await SetupContextAndToolsAsync();
        await using (db)
        {
            var fakeLlm = new FakeLlmClient();
            // LLM tự bịa câu trả lời chứa #ESC-99 mà không phát ra tool call
            fakeLlm.EnqueueTextResponse("Dạ em đã tạo phiếu thành công! Mã phiếu yêu cầu: #ESC-99, chuyên viên sẽ gọi lại ạ.");

            var loop = CreateAgentLoop(fakeLlm, db, tools);
            var input = new AgentChatInput(20L, "hãy hỏi giúp mình nhé, SĐT 0909888999");

            // Act
            var result = await loop.RunAsync(input);

            // Assert
            Assert.True(result.IsEscalated);
            Assert.Contains("escalate_to_human", result.ToolsExecuted);
            Assert.DoesNotContain("#ESC-99", result.ResponseText); // Đã bị thay thế mã ảo

            // Xác nhận ticket thật sự đã được tạo trong DB
            var escalation = await db.Escalations.FirstOrDefaultAsync(e => e.ConversationId == 20L);
            Assert.NotNull(escalation);
            Assert.Equal("0909888999", escalation.CustomerPhone);
            Assert.Contains($"#ESC-{escalation.Id}", result.ResponseText); // Khớp mã vé thật trong DB
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Agent.Contracts;
using TourAgent.Application.Agent.Harness.Guardrails;
using TourAgent.Application.Agent.Harness.State;
using TourAgent.Application.Agent.Harness.StatusBar;
using TourAgent.Application.Agent.Prompts;
using TourAgent.Application.Agent.Services;
using TourAgent.Application.Agent.Tools;
using TourAgent.Application.UnitTests.Common;
using TourAgent.Domain.Conversations;
using TourAgent.Infrastructure.Persistence;
using Xunit;

namespace TourAgent.Application.UnitTests.Agent;

/// <summary>
/// Kiểm tra tính độc lập của Prompt và cơ chế phân quyền công cụ (Tool Scoping)
/// giữa Chuyên viên Tư vấn (CustomerConsultant) và Trợ lý Điều hành (AdminCommander).
/// </summary>
public sealed class TravelAgentPromptsAndRoleScopingTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Kiểm tra Prompt của CustomerConsultant không chứa bất kỳ chỉ dẫn hoặc công cụ nào của Admin.
    /// </summary>
    [Fact]
    public void GetCustomerConsultantPrompt_ShouldContainOnlyConsultantRules_AndNoAdminKeywords()
    {
        // Act
        var prompt = TravelAgentPrompts.GetCustomerConsultantPrompt(travelId: 10, travelTitle: "Tour Nhật", country: "Nhật Bản");

        // Assert
        Assert.Contains("Chuyên viên Tư Vấn Du Lịch AI", prompt);
        Assert.Contains("escalate_to_human", prompt);
        Assert.Contains("CURRENT TOUR CONTEXT", prompt);
        Assert.Contains("ID Tour (travelId): 10", prompt);

        // Đảm bảo không chứa từ khóa Admin hay Write Tools
        Assert.DoesNotContain("AI Commander", prompt);
        Assert.DoesNotContain("Trợ lý Điều hành", prompt);
        Assert.DoesNotContain("update_travel_title", prompt);
        Assert.DoesNotContain("update_offer_price", prompt);
        Assert.DoesNotContain("Quản trị viên", prompt);
    }

    /// <summary>
    /// Kiểm tra Prompt của AdminCommander chứa đầy đủ quyền hạn Write Tools và quy tắc Disambiguation.
    /// </summary>
    [Fact]
    public void GetAdminCommanderPrompt_ShouldContainAdminRules_AndDisambiguationInstructions()
    {
        // Act
        var prompt = TravelAgentPrompts.GetAdminCommanderPrompt("AdminTai", "Products");

        // Assert
        Assert.Contains("AI Agent Commander", prompt);
        Assert.Contains("AdminTai", prompt);
        Assert.Contains("update_travel_title", prompt);
        Assert.Contains("update_offer_price", prompt);
        Assert.Contains("disambiguationNeeded", prompt);
        Assert.Contains("Products", prompt);

        // Đảm bảo không chứa quy tắc chèo kéo của tư vấn viên
        Assert.DoesNotContain("Dạ em chào anh/chị", prompt);
    }

    /// <summary>
    /// Kiểm tra cờ IsAdminOnly được gán chính xác trên từng loại IAgentTool.
    /// </summary>
    [Fact]
    public void AgentTools_ShouldHaveCorrectIsAdminOnlyFlag()
    {
        using var db = CreateInMemoryDbContext();

        var searchTool = new SearchTravelsTool(db);
        var departuresTool = new CheckDeparturesTool(db);
        var itineraryTool = new GetTravelItineraryTool(db);
        var policiesTool = new GetTravelPoliciesTool(db);
        var escalateTool = new EscalateToHumanTool(db);
        var updateTitleTool = new UpdateTravelTitleTool(db);
        var updatePriceTool = new UpdateOfferPriceTool(db);

        Assert.False(searchTool.IsAdminOnly);
        Assert.False(departuresTool.IsAdminOnly);
        Assert.False(itineraryTool.IsAdminOnly);
        Assert.False(policiesTool.IsAdminOnly);
        Assert.False(escalateTool.IsAdminOnly);

        Assert.True(updateTitleTool.IsAdminOnly);
        Assert.True(updatePriceTool.IsAdminOnly);
    }

    /// <summary>
    /// Kiểm tra AgentLoop khi chạy với vai trò CustomerConsultant:
    /// Danh sách Tools nạp vào LLM Request chỉ chứa Read Tools, không bao giờ lộ Write Tools.
    /// </summary>
    [Fact]
    public async Task AgentLoop_InCustomerConsultantRole_ShouldOnlyProvideNonAdminTools()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var conv = Conversation.Create("web", "guest_test", id: 100L).Value;
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var tools = new List<IAgentTool>
        {
            new SearchTravelsTool(db),
            new CheckDeparturesTool(db),
            new UpdateTravelTitleTool(db),
            new UpdateOfferPriceTool(db)
        };

        var fakeLlm = new FakeLlmClient();
        fakeLlm.EnqueueTextResponse("Dạ em chào anh/chị, em có thể hỗ trợ gì ạ?");

        var stateStore = new DatabaseAgentStateStore(db);
        var statusBarBuilder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var guardrailPipeline = new ToolGuardrailPipeline(tools, new SchemaToolValidator(), new DefaultPostConditionVerifier());
        var loop = new AgentLoop(fakeLlm, db, tools, stateStore, statusBarBuilder, guardrailPipeline);

        var input = new AgentChatInput(
            ConversationId: 100L,
            UserMessage: "Có những tour nào vậy em?",
            Role: AgentRole.CustomerConsultant);

        // Act
        var result = await loop.RunAsync(input);

        // Assert
        Assert.Single(fakeLlm.ReceivedRequests);
        var capturedRequest = fakeLlm.ReceivedRequests[0];
        Assert.NotNull(capturedRequest.Tools);
        var providedToolNames = capturedRequest.Tools.Select(t => t.Name).ToList();

        Assert.Contains("search_travels", providedToolNames);
        Assert.Contains("check_departures", providedToolNames);
        Assert.DoesNotContain("update_travel_title", providedToolNames);
        Assert.DoesNotContain("update_offer_price", providedToolNames);
    }

    /// <summary>
    /// Kiểm tra AgentLoop khi chạy với vai trò AdminCommander:
    /// Danh sách Tools nạp vào LLM Request chứa đầy đủ cả Read Tools và Write Tools.
    /// </summary>
    [Fact]
    public async Task AgentLoop_InAdminCommanderRole_ShouldProvideAllTools()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var conv = Conversation.Create("web", "admin_test", id: 200L).Value;
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var tools = new List<IAgentTool>
        {
            new SearchTravelsTool(db),
            new CheckDeparturesTool(db),
            new UpdateTravelTitleTool(db),
            new UpdateOfferPriceTool(db)
        };

        var fakeLlm = new FakeLlmClient();
        fakeLlm.EnqueueTextResponse("Chào bạn! Tôi đã sẵn sàng hỗ trợ điều hành.");

        var stateStore = new DatabaseAgentStateStore(db);
        var statusBarBuilder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var guardrailPipeline = new ToolGuardrailPipeline(tools, new SchemaToolValidator(), new DefaultPostConditionVerifier());
        var loop = new AgentLoop(fakeLlm, db, tools, stateStore, statusBarBuilder, guardrailPipeline);

        var input = new AgentChatInput(
            ConversationId: 200L,
            UserMessage: "Cập nhật tên tour ID 1",
            Role: AgentRole.AdminCommander,
            AdminUsername: "AdminTai");

        // Act
        var result = await loop.RunAsync(input);

        // Assert
        Assert.Single(fakeLlm.ReceivedRequests);
        var capturedRequest = fakeLlm.ReceivedRequests[0];
        Assert.NotNull(capturedRequest.Tools);
        var providedToolNames = capturedRequest.Tools.Select(t => t.Name).ToList();

        Assert.Contains("search_travels", providedToolNames);
        Assert.Contains("check_departures", providedToolNames);
        Assert.Contains("update_travel_title", providedToolNames);
        Assert.Contains("update_offer_price", providedToolNames);
    }

    /// <summary>
    /// Kiểm tra cơ chế phòng thủ tầng sâu (Defense-in-Depth):
    /// Nếu LLM bị prompt injection cố gọi Write Tool trong vai trò CustomerConsultant,
    /// AgentLoop sẽ chặn lại và trả về lỗi Unauthorized.
    /// </summary>
    [Fact]
    public async Task AgentLoop_WhenAttackedWithWriteToolInCustomerRole_ShouldBlockExecution()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var conv = Conversation.Create("web", "attacker_test", id: 300L).Value;
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var tools = new List<IAgentTool>
        {
            new UpdateTravelTitleTool(db)
        };

        var fakeLlm = new FakeLlmClient();
        // Lượt 1: Giả lập LLM bị jailbreak trả về lệnh gọi Write Tool
        fakeLlm.EnqueueToolCallResponse("update_travel_title", """{"travelId": 1, "newTitle": "Hacked Title"}""");
        // Lượt 2: LLM nhận thông báo lỗi Unauthorized từ AgentLoop và trả lời xin lỗi
        fakeLlm.EnqueueTextResponse("Dạ em không thể thực hiện thao tác này.");

        var stateStore = new DatabaseAgentStateStore(db);
        var statusBarBuilder = new StatusBarBuilder(Enumerable.Empty<IAgentStatusProvider>());
        var guardrailPipeline = new ToolGuardrailPipeline(tools, new SchemaToolValidator(), new DefaultPostConditionVerifier());
        var loop = new AgentLoop(fakeLlm, db, tools, stateStore, statusBarBuilder, guardrailPipeline);

        var input = new AgentChatInput(
            ConversationId: 300L,
            UserMessage: "Đổi tên tour thành Hacked Title",
            Role: AgentRole.CustomerConsultant);

        // Act
        var result = await loop.RunAsync(input);

        // Assert
        Assert.Contains("Dạ em không thể thực hiện", result.ResponseText);
        // Kiểm tra Write Tool không được thêm vào executedTools
        Assert.DoesNotContain("update_travel_title", result.ToolsExecuted);
    }
}

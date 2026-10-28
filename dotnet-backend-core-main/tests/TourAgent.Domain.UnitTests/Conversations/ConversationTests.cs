using TourAgent.Domain.Conversations;
using Xunit;

namespace TourAgent.Domain.UnitTests.Conversations;

/// <summary>
/// Các bài kiểm tra đơn vị (Unit Tests) cho Conversation, Message và logic Escalation.
/// </summary>
public class ConversationTests
{
    /// <summary>
    /// Kiểm tra tạo Conversation mới thành công với trạng thái mặc định là BotActive.
    /// </summary>
    [Fact]
    public void Create_WithValidChannelAndUser_ShouldReturnSuccessResult()
    {
        // Act
        var result = Conversation.Create("web", "user_12345");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("web", result.Value.Channel);
        Assert.Equal("user_12345", result.Value.ChannelUserId);
        Assert.Equal(ConversationStatus.BotActive, result.Value.Status);
        Assert.Empty(result.Value.Messages);
    }

    /// <summary>
    /// Kiểm tra thêm tin nhắn vào Conversation thành công.
    /// </summary>
    [Fact]
    public void AddMessage_ShouldAppendMessageToConversation()
    {
        // Arrange
        var conversation = Conversation.Create("telegram", "tg_user_99").Value;
        var userMessage = Message.CreateUserMessage(conversation.Id, "Tháng 10 có tour Nhật nào không em?").Value;
        var assistantMessage = Message.CreateAssistantMessage(conversation.Id, "Dạ bên em có tour Nhật ngày 15/10 ạ!").Value;

        // Act
        conversation.AddMessage(userMessage);
        conversation.AddMessage(assistantMessage);

        // Assert
        Assert.Equal(2, conversation.Messages.Count);
    }

    /// <summary>
    /// Kiểm tra chức năng chuyển giao cho nhân viên tư vấn cập nhật trạng thái sang HumanActive.
    /// </summary>
    [Fact]
    public void EscalateToHuman_ShouldUpdateStatusToHumanActive()
    {
        // Arrange
        var conversation = Conversation.Create("zalo", "zalo_user_88").Value;

        // Act
        conversation.EscalateToHuman("Khách muốn cọc tiền tour Phú Sĩ", "{\"phone\":\"0901234567\"}");

        // Assert
        Assert.Equal(ConversationStatus.HumanActive, conversation.Status);
        Assert.Contains("0901234567", conversation.MetadataJson);
    }

    /// <summary>
    /// Kiểm tra đóng phiên tư vấn cập nhật trạng thái sang Resolved.
    /// </summary>
    [Fact]
    public void Resolve_ShouldUpdateStatusToResolved()
    {
        // Arrange
        var conversation = Conversation.Create("web", "web_user_77").Value;

        // Act
        conversation.Resolve();

        // Assert
        Assert.Equal(ConversationStatus.Resolved, conversation.Status);
    }
}

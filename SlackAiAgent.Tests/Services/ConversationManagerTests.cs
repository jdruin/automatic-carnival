using FluentAssertions;
using SlackAiAgent.Services;
using Xunit;

namespace SlackAiAgent.Tests.Services;

public class ConversationManagerTests
{
    private const int MaxHistoryMessages = 10;
    private const string SystemPrompt = "You are a test assistant.";

    [Fact]
    public void GetOrCreateContext_ShouldCreateNewContext_WhenContextDoesNotExist()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var channelId = "C123456";
        var threadTs = "1234567890.123456";

        // Act
        var context = manager.GetOrCreateContext(channelId, threadTs);

        // Assert
        context.Should().NotBeNull();
        context.ChannelId.Should().Be(channelId);
        context.ThreadId.Should().Be(threadTs);
        context.ChatHistory.Should().NotBeNull();
        context.ChatHistory.Count.Should().Be(1); // System message
    }

    [Fact]
    public void GetOrCreateContext_ShouldReturnSameContext_WhenCalledMultipleTimes()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var channelId = "C123456";
        var threadTs = "1234567890.123456";

        // Act
        var context1 = manager.GetOrCreateContext(channelId, threadTs);
        var context2 = manager.GetOrCreateContext(channelId, threadTs);

        // Assert
        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public void GetOrCreateContext_ShouldCreateSeparateContexts_ForDifferentThreads()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var channelId = "C123456";
        var threadTs1 = "1234567890.111111";
        var threadTs2 = "1234567890.222222";

        // Act
        var context1 = manager.GetOrCreateContext(channelId, threadTs1);
        var context2 = manager.GetOrCreateContext(channelId, threadTs2);

        // Assert
        context1.Should().NotBeSameAs(context2);
        context1.ThreadId.Should().Be(threadTs1);
        context2.ThreadId.Should().Be(threadTs2);
    }

    [Fact]
    public void GetOrCreateContext_ShouldUseChannelIdAsThreadId_WhenThreadTsIsNull()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var channelId = "C123456";

        // Act
        var context = manager.GetOrCreateContext(channelId, null);

        // Assert
        context.ThreadId.Should().Be(channelId);
    }

    [Fact]
    public void AddUserMessage_ShouldAddMessageToHistory()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var context = manager.GetOrCreateContext("C123456", "1234567890.123456");
        var message = "Hello, bot!";

        // Act
        manager.AddUserMessage(context, message);

        // Assert
        context.ChatHistory.Count.Should().Be(2); // System + User message
        context.ChatHistory[1].Content.Should().Be(message);
        context.ChatHistory[1].Role.Label.Should().Be("user");
    }

    [Fact]
    public void AddAssistantMessage_ShouldAddMessageToHistory()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var context = manager.GetOrCreateContext("C123456", "1234567890.123456");
        var message = "Hello, user!";

        // Act
        manager.AddAssistantMessage(context, message);

        // Assert
        context.ChatHistory.Count.Should().Be(2); // System + Assistant message
        context.ChatHistory[1].Content.Should().Be(message);
        context.ChatHistory[1].Role.Label.Should().Be("assistant");
    }

    [Fact]
    public void AddUserMessage_ShouldUpdateLastActivity()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var context = manager.GetOrCreateContext("C123456", "1234567890.123456");
        var initialTime = context.LastActivity;

        // Wait a tiny bit to ensure time difference
        Thread.Sleep(10);

        // Act
        manager.AddUserMessage(context, "Test message");

        // Assert
        context.LastActivity.Should().BeAfter(initialTime);
    }

    [Fact]
    public void TrimHistory_ShouldLimitMessageCount_WhenExceedingMaxHistory()
    {
        // Arrange
        var maxMessages = 5;
        var manager = new ConversationManager(maxMessages, SystemPrompt);
        var context = manager.GetOrCreateContext("C123456", "1234567890.123456");

        // Act - Add more messages than the limit
        for (int i = 0; i < 10; i++)
        {
            manager.AddUserMessage(context, $"Message {i}");
        }

        // Assert - Should have system message + maxMessages (total = maxMessages + 1)
        context.ChatHistory.Count.Should().Be(maxMessages + 1);
        // First message should be system message
        context.ChatHistory[0].Role.Label.Should().Be("system");
    }

    [Fact]
    public void TrimHistory_ShouldKeepSystemMessage_WhenTrimming()
    {
        // Arrange
        var maxMessages = 3;
        var manager = new ConversationManager(maxMessages, SystemPrompt);
        var context = manager.GetOrCreateContext("C123456", "1234567890.123456");

        // Act - Add messages beyond limit
        for (int i = 0; i < 5; i++)
        {
            manager.AddUserMessage(context, $"Message {i}");
            manager.AddAssistantMessage(context, $"Response {i}");
        }

        // Assert
        context.ChatHistory[0].Role.Label.Should().Be("system");
        context.ChatHistory[0].Content.Should().Be(SystemPrompt);
    }

    [Fact]
    public void CleanupOldConversations_ShouldRemoveOldContexts()
    {
        // Arrange
        var manager = new ConversationManager(MaxHistoryMessages, SystemPrompt);
        var oldContext = manager.GetOrCreateContext("C123456", "1234567890.111111");
        var newContext = manager.GetOrCreateContext("C123456", "1234567890.222222");

        // Manually set old context's last activity to 25 hours ago
        var oldTime = DateTime.UtcNow.AddHours(-25);
        typeof(SlackAiAgent.Models.ConversationContext)
            .GetProperty("LastActivity")!
            .SetValue(oldContext, oldTime);

        // Act
        manager.CleanupOldConversations();

        // Get contexts again to check if old one was removed
        var retrievedOldContext = manager.GetOrCreateContext("C123456", "1234567890.111111");
        var retrievedNewContext = manager.GetOrCreateContext("C123456", "1234567890.222222");

        // Assert
        // Old context should be a new instance (original was cleaned up)
        retrievedOldContext.Should().NotBeSameAs(oldContext);
        retrievedOldContext.LastActivity.Should().BeAfter(oldTime);

        // New context should be the same instance
        retrievedNewContext.Should().BeSameAs(newContext);
    }
}

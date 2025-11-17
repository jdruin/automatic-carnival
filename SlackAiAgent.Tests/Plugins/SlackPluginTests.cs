using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlackAiAgent.Plugins;
using SlackNet;
using SlackNet.Events;
using Xunit;

namespace SlackAiAgent.Tests.Plugins;

public class SlackPluginTests
{
    private readonly Mock<ISlackApiClient> _mockSlackClient;
    private readonly Mock<ILogger<SlackPlugin>> _mockLogger;
    private readonly SlackPlugin _plugin;

    public SlackPluginTests()
    {
        _mockSlackClient = new Mock<ISlackApiClient>();
        _mockLogger = new Mock<ILogger<SlackPlugin>>();
        _plugin = new SlackPlugin(_mockSlackClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ListChannels_ShouldReturnChannelList_WhenChannelsExist()
    {
        // Arrange
        var channels = new ConversationList
        {
            Channels = new List<Conversation>
            {
                new() { Name = "general", Purpose = new Purpose { Value = "General discussion" }, IsArchived = false },
                new() { Name = "random", Purpose = new Purpose { Value = "Random stuff" }, IsArchived = false }
            }
        };

        _mockSlackClient
            .Setup(x => x.Conversations.List(
                It.IsAny<string[]>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels);

        // Act
        var result = await _plugin.ListChannels();

        // Assert
        result.Should().Contain("general");
        result.Should().Contain("random");
        result.Should().Contain("General discussion");
    }

    [Fact]
    public async Task ListChannels_ShouldReturnNoChannelsMessage_WhenNoChannelsExist()
    {
        // Arrange
        var channels = new ConversationList
        {
            Channels = new List<Conversation>()
        };

        _mockSlackClient
            .Setup(x => x.Conversations.List(
                It.IsAny<string[]>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels);

        // Act
        var result = await _plugin.ListChannels();

        // Assert
        result.Should().Contain("No public channels found");
    }

    [Fact]
    public async Task ListChannels_ShouldFilterArchivedChannels()
    {
        // Arrange
        var channels = new ConversationList
        {
            Channels = new List<Conversation>
            {
                new() { Name = "active", Purpose = new Purpose { Value = "Active channel" }, IsArchived = false },
                new() { Name = "archived", Purpose = new Purpose { Value = "Archived channel" }, IsArchived = true }
            }
        };

        _mockSlackClient
            .Setup(x => x.Conversations.List(
                It.IsAny<string[]>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels);

        // Act
        var result = await _plugin.ListChannels();

        // Assert
        result.Should().Contain("active");
        result.Should().NotContain("archived");
    }

    [Fact]
    public async Task GetChannelMembers_ShouldReturnMemberCount_WhenChannelFound()
    {
        // Arrange
        var channelId = "C123456";
        var members = new List<string> { "U1", "U2", "U3" };

        _mockSlackClient
            .Setup(x => x.Conversations.Members(channelId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        // Act
        var result = await _plugin.GetChannelMembers(channelId);

        // Assert
        result.Should().Contain("3 members");
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnUserInformation()
    {
        // Arrange
        var userId = "U123456";
        var user = new User
        {
            Name = "testuser",
            RealName = "Test User",
            Profile = new UserProfile
            {
                DisplayName = "TestUser",
                StatusText = "Working"
            }
        };

        _mockSlackClient
            .Setup(x => x.Users.Info(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _plugin.GetUserInfo(userId);

        // Assert
        result.Should().Contain("testuser");
        result.Should().Contain("Test User");
        result.Should().Contain("TestUser");
        result.Should().Contain("Working");
    }

    [Fact]
    public async Task SearchMessages_ShouldReturnMatches_WhenFound()
    {
        // Arrange
        var query = "test query";
        var searchResults = new SearchResult
        {
            Messages = new MessageSearchResult
            {
                Matches = new List<MessageMatch>
                {
                    new() { Username = "user1", Text = "This is a test message" },
                    new() { Username = "user2", Text = "Another test message" }
                }
            }
        };

        _mockSlackClient
            .Setup(x => x.Search.Messages(query, It.IsAny<SearchSort>(), It.IsAny<SearchSortDirection>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _plugin.SearchMessages(query, 5);

        // Assert
        result.Should().Contain("user1");
        result.Should().Contain("test message");
        result.Should().Contain("2 messages");
    }

    [Fact]
    public async Task SearchMessages_ShouldReturnNoResults_WhenNoneFound()
    {
        // Arrange
        var query = "nonexistent";
        var searchResults = new SearchResult
        {
            Messages = new MessageSearchResult
            {
                Matches = new List<MessageMatch>()
            }
        };

        _mockSlackClient
            .Setup(x => x.Search.Messages(query, It.IsAny<SearchSort>(), It.IsAny<SearchSortDirection>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _plugin.SearchMessages(query);

        // Assert
        result.Should().Contain("No messages found");
        result.Should().Contain(query);
    }
}

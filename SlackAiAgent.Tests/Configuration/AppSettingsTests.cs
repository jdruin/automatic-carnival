using FluentAssertions;
using SlackAiAgent.Configuration;
using Xunit;

namespace SlackAiAgent.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_ShouldInitializeWithDefaultValues()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.Slack.Should().NotBeNull();
        settings.AI.Should().NotBeNull();
        settings.Agent.Should().NotBeNull();
    }

    [Fact]
    public void SlackSettings_ShouldInitializeWithEmptyStrings()
    {
        // Act
        var settings = new SlackSettings();

        // Assert
        settings.AppToken.Should().BeEmpty();
        settings.BotToken.Should().BeEmpty();
    }

    [Fact]
    public void AISettings_ShouldDefaultToOllama()
    {
        // Act
        var settings = new AISettings();

        // Assert
        settings.Provider.Should().Be("Ollama");
        settings.Ollama.Should().NotBeNull();
        settings.Bedrock.Should().NotBeNull();
    }

    [Fact]
    public void OllamaSettings_ShouldHaveDefaultValues()
    {
        // Act
        var settings = new OllamaSettings();

        // Assert
        settings.Endpoint.Should().Be("http://localhost:11434");
        settings.ModelId.Should().Be("llama3.2");
    }

    [Fact]
    public void BedrockSettings_ShouldHaveDefaultValues()
    {
        // Act
        var settings = new BedrockSettings();

        // Assert
        settings.Region.Should().Be("us-east-1");
        settings.ModelId.Should().Be("anthropic.claude-3-5-sonnet-20241022-v2:0");
    }

    [Fact]
    public void AgentSettings_ShouldHaveDefaultValues()
    {
        // Act
        var settings = new AgentSettings();

        // Assert
        settings.SystemPrompt.Should().Be("You are a helpful AI assistant.");
        settings.MaxHistoryMessages.Should().Be(10);
    }

    [Fact]
    public void AppSettings_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var settings = new AppSettings
        {
            Slack = new SlackSettings
            {
                AppToken = "xapp-test",
                BotToken = "xoxb-test"
            },
            AI = new AISettings
            {
                Provider = "Bedrock",
                Bedrock = new BedrockSettings
                {
                    Region = "us-west-2",
                    ModelId = "test-model"
                }
            },
            Agent = new AgentSettings
            {
                SystemPrompt = "Custom prompt",
                MaxHistoryMessages = 20
            }
        };

        // Assert
        settings.Slack.AppToken.Should().Be("xapp-test");
        settings.Slack.BotToken.Should().Be("xoxb-test");
        settings.AI.Provider.Should().Be("Bedrock");
        settings.AI.Bedrock.Region.Should().Be("us-west-2");
        settings.AI.Bedrock.ModelId.Should().Be("test-model");
        settings.Agent.SystemPrompt.Should().Be("Custom prompt");
        settings.Agent.MaxHistoryMessages.Should().Be(20);
    }
}

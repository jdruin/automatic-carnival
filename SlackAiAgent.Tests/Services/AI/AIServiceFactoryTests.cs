using FluentAssertions;
using SlackAiAgent.Configuration;
using SlackAiAgent.Services.AI;
using Xunit;

namespace SlackAiAgent.Tests.Services.AI;

public class AIServiceFactoryTests
{
    [Fact]
    public void CreateChatCompletionService_ShouldCreateOllamaService_WhenProviderIsOllama()
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = "Ollama",
                Ollama = new OllamaSettings
                {
                    Endpoint = "http://localhost:11434",
                    ModelId = "llama3.2"
                }
            }
        };

        // Act
        var service = AIServiceFactory.CreateChatCompletionService(settings);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<OllamaChatCompletion>();
    }

    [Fact]
    public void CreateChatCompletionService_ShouldCreateBedrockService_WhenProviderIsBedrock()
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = "Bedrock",
                Bedrock = new BedrockSettings
                {
                    Region = "us-east-1",
                    ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0"
                }
            }
        };

        // Act
        var service = AIServiceFactory.CreateChatCompletionService(settings);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<BedrockChatCompletion>();
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("OLLAMA")]
    [InlineData("Ollama")]
    public void CreateChatCompletionService_ShouldBeCaseInsensitive_ForOllama(string provider)
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = provider,
                Ollama = new OllamaSettings
                {
                    Endpoint = "http://localhost:11434",
                    ModelId = "llama3.2"
                }
            }
        };

        // Act
        var service = AIServiceFactory.CreateChatCompletionService(settings);

        // Assert
        service.Should().BeOfType<OllamaChatCompletion>();
    }

    [Theory]
    [InlineData("bedrock")]
    [InlineData("BEDROCK")]
    [InlineData("Bedrock")]
    public void CreateChatCompletionService_ShouldBeCaseInsensitive_ForBedrock(string provider)
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = provider,
                Bedrock = new BedrockSettings
                {
                    Region = "us-east-1",
                    ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0"
                }
            }
        };

        // Act
        var service = AIServiceFactory.CreateChatCompletionService(settings);

        // Assert
        service.Should().BeOfType<BedrockChatCompletion>();
    }

    [Fact]
    public void CreateChatCompletionService_ShouldThrowException_WhenProviderIsUnsupported()
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = "UnsupportedProvider"
            }
        };

        // Act
        Action act = () => AIServiceFactory.CreateChatCompletionService(settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported AI provider*");
    }
}

using Microsoft.SemanticKernel.ChatCompletion;
using SlackAiAgent.Configuration;

namespace SlackAiAgent.Services.AI;

/// <summary>
/// Factory for creating AI chat completion services based on configuration
/// </summary>
public static class AIServiceFactory
{
    public static IChatCompletionService CreateChatCompletionService(AppSettings settings)
    {
        return settings.AI.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaChatCompletion(
                settings.AI.Ollama.Endpoint,
                settings.AI.Ollama.ModelId),

            "bedrock" => new BedrockChatCompletion(
                settings.AI.Bedrock.Region,
                settings.AI.Bedrock.ModelId),

            _ => throw new InvalidOperationException(
                $"Unsupported AI provider: {settings.AI.Provider}. " +
                "Supported providers: Ollama, Bedrock")
        };
    }
}

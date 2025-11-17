using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SlackAiAgent.Services.AI;

/// <summary>
/// Amazon Bedrock chat completion service implementation for Semantic Kernel
/// Supports Claude models via the Messages API
/// </summary>
public class BedrockChatCompletion : IChatCompletionService
{
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private readonly string _modelId;

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public BedrockChatCompletion(string region, string modelId)
    {
        _modelId = modelId;
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        _bedrockClient = new AmazonBedrockRuntimeClient(regionEndpoint);
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var response = await GetChatCompletionAsync(chatHistory, cancellationToken);
        return new[] { new ChatMessageContent(AuthorRole.Assistant, response) };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in GetStreamingChatCompletionAsync(chatHistory, cancellationToken))
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk);
        }
    }

    private async Task<string> GetChatCompletionAsync(ChatHistory chatHistory, CancellationToken cancellationToken)
    {
        var (system, messages) = ConvertChatHistory(chatHistory);

        var request = new BedrockClaudeRequest
        {
            AnthropicVersion = "bedrock-2023-05-31",
            MaxTokens = 4096,
            System = system,
            Messages = messages
        };

        var requestJson = JsonSerializer.Serialize(request);
        var invokeRequest = new InvokeModelRequest
        {
            ModelId = _modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        var response = await _bedrockClient.InvokeModelAsync(invokeRequest, cancellationToken);

        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<BedrockClaudeResponse>(responseJson);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    private async IAsyncEnumerable<string> GetStreamingChatCompletionAsync(
        ChatHistory chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (system, messages) = ConvertChatHistory(chatHistory);

        var request = new BedrockClaudeRequest
        {
            AnthropicVersion = "bedrock-2023-05-31",
            MaxTokens = 4096,
            System = system,
            Messages = messages
        };

        var requestJson = JsonSerializer.Serialize(request);
        var invokeRequest = new InvokeModelWithResponseStreamRequest
        {
            ModelId = _modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        var response = await _bedrockClient.InvokeModelWithResponseStreamAsync(invokeRequest, cancellationToken);

        foreach (var payloadPart in response.Body)
        {
            if (payloadPart is PayloadPart chunk)
            {
                using var chunkReader = new StreamReader(chunk.Bytes);
                var chunkJson = await chunkReader.ReadToEndAsync(cancellationToken);

                var streamEvent = JsonSerializer.Deserialize<BedrockStreamEvent>(chunkJson);

                if (streamEvent?.Type == "content_block_delta" &&
                    streamEvent?.Delta?.Text != null)
                {
                    yield return streamEvent.Delta.Text;
                }
            }
        }
    }

    private (string system, List<BedrockMessage> messages) ConvertChatHistory(ChatHistory chatHistory)
    {
        var systemMessage = string.Empty;
        var messages = new List<BedrockMessage>();

        foreach (var msg in chatHistory)
        {
            if (msg.Role.Label.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                systemMessage = msg.Content ?? string.Empty;
            }
            else
            {
                messages.Add(new BedrockMessage
                {
                    Role = msg.Role.Label.ToLowerInvariant(),
                    Content = new List<BedrockContent>
                    {
                        new() { Type = "text", Text = msg.Content ?? string.Empty }
                    }
                });
            }
        }

        return (systemMessage, messages);
    }

    private class BedrockClaudeRequest
    {
        [JsonPropertyName("anthropic_version")]
        public string AnthropicVersion { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<BedrockMessage> Messages { get; set; } = new();
    }

    private class BedrockMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<BedrockContent> Content { get; set; } = new();
    }

    private class BedrockContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class BedrockClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<BedrockContent>? Content { get; set; }
    }

    private class BedrockStreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public BedrockDelta? Delta { get; set; }
    }

    private class BedrockDelta
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

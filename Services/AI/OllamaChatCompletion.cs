using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SlackAiAgent.Services.AI;

/// <summary>
/// Ollama chat completion service implementation for Semantic Kernel
/// </summary>
public class OllamaChatCompletion : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private readonly string _endpoint;

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public OllamaChatCompletion(string endpoint, string modelId, HttpClient? httpClient = null)
    {
        _endpoint = endpoint.TrimEnd('/');
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
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
        var messages = ConvertChatHistory(chatHistory);
        var request = new OllamaChatRequest
        {
            Model = _modelId,
            Messages = messages,
            Stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_endpoint}/api/chat",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);

        return result?.Message?.Content ?? string.Empty;
    }

    private async IAsyncEnumerable<string> GetStreamingChatCompletionAsync(
        ChatHistory chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = ConvertChatHistory(chatHistory);
        var request = new OllamaChatRequest
        {
            Model = _modelId,
            Messages = messages,
            Stream = true
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{_endpoint}/api/chat",
            requestContent,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message?.Content != null)
            {
                yield return chunk.Message.Content;
            }
        }
    }

    private List<OllamaMessage> ConvertChatHistory(ChatHistory chatHistory)
    {
        return chatHistory.Select(msg => new OllamaMessage
        {
            Role = msg.Role.Label.ToLowerInvariant(),
            Content = msg.Content ?? string.Empty
        }).ToList();
    }

    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SlackNet;
using SlackAiAgent.Configuration;
using SlackAiAgent.Models;
using SlackAiAgent.Plugins;
using SlackAiAgent.Services.AI;

namespace SlackAiAgent.Services;

/// <summary>
/// Orchestrates the AI agent with tool/function calling capabilities
/// </summary>
public class AgentOrchestrator
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly ILogger<SlackPlugin> _slackPluginLogger;
    private readonly AppSettings _settings;
    private bool _slackPluginRegistered = false;

    public AgentOrchestrator(
        AppSettings settings,
        IChatCompletionService chatService,
        ILogger<AgentOrchestrator> logger,
        ILogger<SlackPlugin> slackPluginLogger)
    {
        _settings = settings;
        _chatService = chatService;
        _logger = logger;
        _slackPluginLogger = slackPluginLogger;

        // Create kernel builder
        var builder = Kernel.CreateBuilder();

        // Add chat completion service
        builder.Services.AddSingleton(chatService);

        // Build kernel
        _kernel = builder.Build();

        // Register basic plugins (Slack plugin will be registered later)
        RegisterBasicPlugins();
    }

    /// <summary>
    /// Registers basic plugins (non-Slack)
    /// </summary>
    private void RegisterBasicPlugins()
    {
        try
        {
            // Register DateTime plugin
            _kernel.Plugins.AddFromObject(new DateTimePlugin(), "DateTime");
            _logger.LogInformation("Registered DateTime plugin");

            // Register Calculator plugin
            _kernel.Plugins.AddFromObject(new CalculatorPlugin(), "Calculator");
            _logger.LogInformation("Registered Calculator plugin");

            // Register Text Utility plugin
            _kernel.Plugins.AddFromObject(new TextUtilityPlugin(), "TextUtility");
            _logger.LogInformation("Registered TextUtility plugin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering basic plugins");
        }
    }

    /// <summary>
    /// Registers Slack plugin after Slack client is available
    /// </summary>
    public void RegisterSlackPlugin(ISlackApiClient slackClient)
    {
        if (_slackPluginRegistered)
        {
            _logger.LogWarning("Slack plugin already registered");
            return;
        }

        try
        {
            _kernel.Plugins.AddFromObject(new SlackPlugin(slackClient, _slackPluginLogger), "Slack");
            _logger.LogInformation("Registered Slack plugin");
            _slackPluginRegistered = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering Slack plugin");
        }
    }

    /// <summary>
    /// Gets AI response with automatic tool calling
    /// </summary>
    public async Task<string> GetResponseAsync(ConversationContext context)
    {
        try
        {
            // Configure execution settings for automatic function calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 2000
            };

            // Get response with automatic tool invocation
            var response = await _chatService.GetChatMessageContentsAsync(
                context.ChatHistory,
                executionSettings,
                _kernel);

            var result = response.FirstOrDefault()?.Content ?? "I'm sorry, I couldn't generate a response.";

            // Log if any tools were called
            var lastMessage = response.FirstOrDefault();
            if (lastMessage?.Metadata?.ContainsKey("ToolCalls") == true)
            {
                _logger.LogInformation("Agent used tools to generate response");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response with tool calling");
            return "I'm sorry, I encountered an error generating a response.";
        }
    }

    /// <summary>
    /// Gets AI response with streaming and automatic tool calling
    /// </summary>
    public async IAsyncEnumerable<string> GetStreamingResponseAsync(ConversationContext context)
    {
        var fullResponse = new System.Text.StringBuilder();

       
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 2000
            };

            await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
                context.ChatHistory,
                executionSettings,
                _kernel))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    fullResponse.Append(chunk.Content);
                    yield return chunk.Content;
                }
            }
    }

    /// <summary>
    /// Gets list of available tools/functions
    /// </summary>
    public IEnumerable<string> GetAvailableTools()
    {
        var tools = new List<string>();

        foreach (var plugin in _kernel.Plugins)
        {
            foreach (var function in plugin)
            {
                tools.Add($"{plugin.Name}.{function.Name}: {function.Description}");
            }
        }

        return tools;
    }
}

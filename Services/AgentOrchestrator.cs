using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SlackNet;
using SlackAiAgent.Configuration;
using SlackAiAgent.Models;
using SlackAiAgent.Plugins;
using SlackAiAgent.Services.AI;

namespace SlackAiAgent.Services;

/// <summary>
/// Orchestrates the AI agent using Microsoft Semantic Kernel's native agent framework
/// </summary>
public class AgentOrchestrator
{
    private readonly Kernel _kernel;
    private ChatCompletionAgent? _agent;
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

        // Create the Semantic Kernel agent
        InitializeAgent();
    }

    /// <summary>
    /// Initializes the Semantic Kernel ChatCompletionAgent
    /// </summary>
    private void InitializeAgent()
    {
        _agent = new ChatCompletionAgent
        {
            Name = "SlackAssistant",
            Instructions = _settings.Agent.SystemPrompt,
            Kernel = _kernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 2000
            })
        };

        _logger.LogInformation("Initialized Semantic Kernel ChatCompletionAgent");
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
    /// Gets AI response using Semantic Kernel ChatCompletionAgent with automatic tool calling
    /// </summary>
    public async Task<string> GetResponseAsync(ConversationContext context)
    {
        if (_agent == null)
        {
            _logger.LogError("Agent not initialized");
            return "I'm sorry, the agent is not properly initialized.";
        }

        try
        {
            // Get the last user message from chat history
            var lastUserMessage = context.ChatHistory.LastOrDefault(m => m.Role.Label == "user");
            if (lastUserMessage == null)
            {
                return "I'm sorry, I didn't receive a message.";
            }

            // Invoke the agent with the chat history
            var agentResponse = await _agent.InvokeAsync(context.ChatHistory).ConfigureAwait(false);

            // Extract the response content
            var responseContent = agentResponse.Content ?? "I'm sorry, I couldn't generate a response.";

            // Log tool usage
            _logger.LogInformation("Agent response generated. Length: {Length} characters", responseContent.Length);

            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response from Semantic Kernel agent");
            return "I'm sorry, I encountered an error generating a response.";
        }
    }

    /// <summary>
    /// Gets AI response with streaming using Semantic Kernel agent
    /// </summary>
    public async IAsyncEnumerable<string> GetStreamingResponseAsync(ConversationContext context)
    {
        if (_agent == null)
        {
            _logger.LogError("Agent not initialized");
            yield return "I'm sorry, the agent is not properly initialized.";
            yield break;
        }

        try
        {
            // Stream the agent's response
            await foreach (var content in _agent.InvokeStreamingAsync(context.ChatHistory))
            {
                if (!string.IsNullOrEmpty(content.Content))
                {
                    yield return content.Content;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streaming AI response from agent");
            yield return "I'm sorry, I encountered an error generating a response.";
        }
    }

    /// <summary>
    /// Gets AI response with thinking/reasoning process logged (for thinking models like o1)
    /// </summary>
    public async Task<(string response, string? thinking)> GetResponseWithThinkingAsync(ConversationContext context)
    {
        if (_agent == null)
        {
            _logger.LogError("Agent not initialized");
            return ("I'm sorry, the agent is not properly initialized.", null);
        }

        try
        {
            // Invoke the agent with the chat history
            var agentResponse = await _agent.InvokeAsync(context.ChatHistory).ConfigureAwait(false);

            // Extract the response content
            var responseContent = agentResponse.Content ?? "I'm sorry, I couldn't generate a response.";

            // Try to extract thinking/reasoning from metadata
            string? thinkingProcess = null;

            // Check if the response has metadata with thinking information
            if (agentResponse.Metadata != null)
            {
                // For models that support reasoning (like OpenAI o1), check for reasoning content
                if (agentResponse.Metadata.TryGetValue("Reasoning", out var reasoning))
                {
                    thinkingProcess = reasoning?.ToString();
                }
                else if (agentResponse.Metadata.TryGetValue("ThoughtProcess", out var thought))
                {
                    thinkingProcess = thought?.ToString();
                }

                // Log the thinking process if available
                if (!string.IsNullOrEmpty(thinkingProcess))
                {
                    _logger.LogInformation("Agent thinking process: {Thinking}", thinkingProcess);
                }
            }

            return (responseContent, thinkingProcess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response with thinking from agent");
            return ("I'm sorry, I encountered an error generating a response.", null);
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

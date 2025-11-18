using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Events;
using SlackNet.Interaction;
using SlackNet.SocketMode;

namespace SlackAiAgent.Services;

/// <summary>
/// Handles Slack integration and message processing
/// </summary>
public class SlackService : IEventHandler
{
    private readonly ConversationManager _conversationManager;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly ISlackApiClient _slackClient;
    private readonly ISlackSocketModeClient _socketModeClient;
    private readonly ILogger<SlackService> _logger;
    private string? _botUserId;

    public ISlackApiClient SlackClient => _slackClient;

    public SlackService(
        ConversationManager conversationManager,
        AgentOrchestrator agentOrchestrator,
        ISlackApiClient slackClient,
        ISlackSocketModeClient socketModeClient,
        ILogger<SlackService> logger)
    {
        _conversationManager = conversationManager;
        _agentOrchestrator = agentOrchestrator;
        _slackClient = slackClient;
        _socketModeClient = socketModeClient;
        _logger = logger;
    }

    /// <summary>
    /// Starts the Slack Socket Mode connection
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Slack service...");

        // Get bot user ID
        var authTest = await _slackClient.Auth.Test(cancellationToken);
        _botUserId = authTest.UserId;
        _logger.LogInformation("Bot authenticated as {BotUserId}", _botUserId);

        // Register Slack plugin now that client is available
        _agentOrchestrator.RegisterSlackPlugin(_slackClient);

        // Start Socket Mode connection
        await _socketModeClient.Connect(cancellationToken);
        _logger.LogInformation("Connected to Slack Socket Mode");

        // Start background task to cleanup old conversations
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                _conversationManager.CleanupOldConversations();
                _logger.LogDebug("Cleaned up old conversations");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// SlackNet event handler interface implementation
    /// </summary>
    public async Task Handle(EventCallback eventCallback)
    {
        if (eventCallback.Event is MessageEvent messageEvent)
        {
            await HandleMessageEventAsync(messageEvent);
        }
    }

    /// <summary>
    /// Handles incoming Slack message events
    /// </summary>
    private async Task HandleMessageEventAsync(MessageEvent messageEvent)
    {
        try
        {
            // Ignore messages from the bot itself
            if (messageEvent.User == _botUserId)
                return;

            // Ignore messages without text
            if (string.IsNullOrWhiteSpace(messageEvent.Text))
                return;

            // Only respond to direct mentions or DMs
            if (!IsBotMentioned(messageEvent) && messageEvent.ChannelType != "im")
                return;

            _logger.LogInformation(
                "Processing message from {User} in {Channel} (thread: {Thread})",
                messageEvent.User,
                messageEvent.Channel,
                messageEvent.ThreadTs ?? "none");

            // Get or create conversation context for this thread
            var context = _conversationManager.GetOrCreateContext(
                messageEvent.Channel,
                messageEvent.ThreadTs);

            // Clean the message text (remove bot mention)
            var cleanedMessage = CleanMessageText(messageEvent.Text);

            // Add user message to history
            _conversationManager.AddUserMessage(context, cleanedMessage);

            // Show typing indicator
            await _slackClient.Chat.PostMessage(new Message
            {
                Channel = messageEvent.Channel,
                ThreadTs = messageEvent.ThreadTs ?? messageEvent.Ts,
                Text = "_Thinking..._"
            });

            // Get AI response
            var response = await GetAIResponseAsync(context);

            // Send response to Slack
            await _slackClient.Chat.PostMessage(new Message
            {
                Channel = messageEvent.Channel,
                ThreadTs = messageEvent.ThreadTs ?? messageEvent.Ts, // Reply in thread
                Text = response
            });

            // Add assistant response to history
            _conversationManager.AddAssistantMessage(context, response);

            _logger.LogInformation("Sent response to {Channel}", messageEvent.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message event");

            // Send error message to user
            if (messageEvent != null)
            {
                await _slackClient.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    ThreadTs = messageEvent.ThreadTs ?? messageEvent.Ts,
                    Text = "Sorry, I encountered an error processing your message. Please try again."
                });
            }
        }
    }

    /// <summary>
    /// Gets AI response using the agent orchestrator with tool calling
    /// </summary>
    private async Task<string> GetAIResponseAsync(Models.ConversationContext context)
    {
        try
        {
            return await _agentOrchestrator.GetResponseAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response");
            return "I'm sorry, I encountered an error generating a response.";
        }
    }

    /// <summary>
    /// Checks if the bot is mentioned in the message
    /// </summary>
    private bool IsBotMentioned(MessageEvent messageEvent)
    {
        if (string.IsNullOrEmpty(_botUserId))
            return false;

        return messageEvent.Text?.Contains($"<@{_botUserId}>") == true;
    }

    /// <summary>
    /// Removes bot mention from message text
    /// </summary>
    private string CleanMessageText(string text)
    {
        if (string.IsNullOrEmpty(_botUserId))
            return text;

        return text.Replace($"<@{_botUserId}>", "").Trim();
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using SlackNet;
using SlackNet.Events;
using SlackNet.Interaction;
using SlackNet.SocketMode;
using SlackAiAgent.Configuration;
using SlackAiAgent.Services.AI;

namespace SlackAiAgent.Services;

/// <summary>
/// Handles Slack integration and message processing
/// </summary>
public class SlackService : IEventHandler
{
    private readonly AppSettings _settings;
    private readonly ConversationManager _conversationManager;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<SlackService> _logger;
    private ISlackApiClient? _slackClient;
    private string? _botUserId;

    public SlackService(
        AppSettings settings,
        ConversationManager conversationManager,
        IChatCompletionService chatService,
        ILogger<SlackService> logger)
    {
        _settings = settings;
        _conversationManager = conversationManager;
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Starts the Slack Socket Mode connection
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Slack service...");

        // Create Slack client
        _slackClient = new SlackServiceBuilder()
            .UseApiToken(_settings.Slack.BotToken)
            .GetApiClient();

        // Get bot user ID
        var authTest = await _slackClient.Auth.Test(cancellationToken);
        _botUserId = authTest.UserId;
        _logger.LogInformation("Bot authenticated as {BotUserId}", _botUserId);

        // Start Socket Mode connection
        var socketModeClient = new SlackServiceBuilder()
            .UseApiToken(_settings.Slack.BotToken)
            .UseAppLevelToken(_settings.Slack.AppToken)
            .RegisterEventHandler(ctx => this)
            .GetSocketModeClient();

        await socketModeClient.Connect(cancellationToken);
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
            if (_slackClient != null)
            {
                await _slackClient.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    ThreadTs = messageEvent.ThreadTs ?? messageEvent.Ts,
                    Text = "_Thinking..._"
                });
            }

            // Get AI response
            var response = await GetAIResponseAsync(context);

            // Send response to Slack
            if (_slackClient != null)
            {
                await _slackClient.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    ThreadTs = messageEvent.ThreadTs ?? messageEvent.Ts, // Reply in thread
                    Text = response
                });
            }

            // Add assistant response to history
            _conversationManager.AddAssistantMessage(context, response);

            _logger.LogInformation("Sent response to {Channel}", messageEvent.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message event");

            // Send error message to user
            if (_slackClient != null && messageEvent != null)
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
    /// Gets AI response using the chat completion service
    /// </summary>
    private async Task<string> GetAIResponseAsync(Models.ConversationContext context)
    {
        try
        {
            var responses = await _chatService.GetChatMessageContentsAsync(
                context.ChatHistory);

            return responses.FirstOrDefault()?.Content ?? "I'm sorry, I couldn't generate a response.";
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

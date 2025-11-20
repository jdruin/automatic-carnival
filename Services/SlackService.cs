using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Events;
using SlackNet.Interaction;
using SlackNet.SocketMode;
using SlackAiAgent.Configuration;

namespace SlackAiAgent.Services;

/// <summary>
/// Handles Slack integration and message processing
/// </summary>
public class SlackService : IEventHandler
{
    private readonly AppSettings _settings;
    private readonly ConversationManager _conversationManager;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly ISlackApiClient _slackClient;
    private readonly ISlackSocketModeClient _socketModeClient;
    private readonly ILogger<SlackService> _logger;
    private string? _botUserId;

    public ISlackApiClient SlackClient => _slackClient;

    public SlackService(
        AppSettings settings,
        ConversationManager conversationManager,
        AgentOrchestrator agentOrchestrator,
        ISlackApiClient slackClient,
        ISlackSocketModeClient socketModeClient,
        ILogger<SlackService> logger)
    {
        _settings = settings;
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

            // If context is new and rebuild is enabled, try to rebuild from Slack history
            if (context.ChatHistory.Count <= 1 && // Only system message
                _settings.Agent.RebuildContextFromSlack &&
                !string.IsNullOrEmpty(messageEvent.ThreadTs))
            {
                // Exclude the current message since we'll add it separately
                await RebuildContextFromSlackAsync(context, messageEvent.Channel, messageEvent.ThreadTs, messageEvent.Ts);
            }

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
    /// Gets AI response using the Semantic Kernel agent with tool calling
    /// </summary>
    private async Task<string> GetAIResponseAsync(Models.ConversationContext context)
    {
        try
        {
            // Check if thinking mode logging is enabled
            if (_settings.Agent.LogThinking)
            {
                var (response, thinking) = await _agentOrchestrator.GetResponseWithThinkingAsync(context);

                // If thinking process is available, log it
                if (!string.IsNullOrEmpty(thinking))
                {
                    _logger.LogInformation("=== Agent Thinking Process ===");
                    _logger.LogInformation("{Thinking}", thinking);
                    _logger.LogInformation("=== End Thinking Process ===");
                }

                return response;
            }
            else
            {
                // Standard response without thinking logging
                return await _agentOrchestrator.GetResponseAsync(context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI response");
            return "I'm sorry, I encountered an error generating a response.";
        }
    }

    /// <summary>
    /// Rebuilds conversation context from Slack thread history
    /// </summary>
    /// <param name="context">The conversation context to populate</param>
    /// <param name="channelId">The Slack channel ID</param>
    /// <param name="threadTs">The thread timestamp</param>
    /// <param name="excludeMessageTs">Optional: timestamp of message to exclude (e.g., current incoming message)</param>
    private async Task RebuildContextFromSlackAsync(Models.ConversationContext context, string channelId, string threadTs, string? excludeMessageTs = null)
    {
        try
        {
            _logger.LogInformation("Attempting to rebuild context from Slack thread {ThreadTs} in channel {Channel}", threadTs, channelId);

            // Fetch conversation replies from Slack
            var replies = await _slackClient.Conversations.Replies(channelId, threadTs);

            if (replies?.Messages == null || replies.Messages.Count == 0)
            {
                _logger.LogWarning("No messages found in thread {ThreadTs}", threadTs);
                return;
            }

            _logger.LogInformation("Found {Count} messages in thread {ThreadTs}", replies.Messages.Count, threadTs);

            // Sort messages by timestamp (chronological order)
            var sortedMessages = replies.Messages.OrderBy(m => m.Ts).ToList();

            // Get the maximum number of messages to rebuild (respecting MaxHistoryMessages)
            // We'll take the most recent messages up to the limit
            var maxMessages = _settings.Agent.MaxHistoryMessages;
            var messagesToProcess = sortedMessages.TakeLast(maxMessages).ToList();

            // Process messages and rebuild chat history
            var messagesAdded = 0;
            foreach (var message in messagesToProcess)
            {
                // Skip the excluded message (current incoming message)
                if (!string.IsNullOrEmpty(excludeMessageTs) && message.Ts == excludeMessageTs)
                    continue;

                // Skip messages without text
                if (string.IsNullOrWhiteSpace(message.Text))
                    continue;

                // Skip the "Thinking..." placeholder messages
                if (message.Text.Trim() == "_Thinking..._")
                    continue;

                // Determine if message is from bot or user
                var isFromBot = message.User == _botUserId || message.BotId != null;

                if (isFromBot)
                {
                    // Add bot message as assistant message
                    _conversationManager.AddAssistantMessage(context, message.Text);
                    messagesAdded++;
                }
                else
                {
                    // Clean user message (remove bot mentions) and add as user message
                    var cleanedText = CleanMessageText(message.Text);
                    _conversationManager.AddUserMessage(context, cleanedText);
                    messagesAdded++;
                }
            }

            _logger.LogInformation(
                "Successfully rebuilt context from Slack history: added {Count} messages to thread {ThreadTs}",
                messagesAdded,
                threadTs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding context from Slack thread {ThreadTs}", threadTs);
            // Don't throw - continue with empty context if rebuild fails
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

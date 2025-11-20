using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using SlackAiAgent.Models;
using SlackAiAgent.Services.Storage;

namespace SlackAiAgent.Services;

/// <summary>
/// Manages multiple conversation contexts for different Slack threads with Redis persistence
/// </summary>
public class ConversationManager
{
    private readonly ConcurrentDictionary<string, ConversationContext> _localCache = new();
    private readonly IConversationStore _store;
    private readonly ILogger<ConversationManager> _logger;
    private readonly int _maxHistoryMessages;
    private readonly string _systemPrompt;

    public ConversationManager(
        int maxHistoryMessages,
        string systemPrompt,
        IConversationStore store,
        ILogger<ConversationManager> logger)
    {
        _maxHistoryMessages = maxHistoryMessages;
        _systemPrompt = systemPrompt;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a conversation context for a specific thread
    /// </summary>
    public ConversationContext GetOrCreateContext(string channelId, string? threadTs)
    {
        // Use thread timestamp as unique identifier, or channel if no thread
        var threadId = threadTs ?? channelId;
        var contextKey = $"{channelId}:{threadId}";

        // Check local cache first
        if (_localCache.TryGetValue(contextKey, out var cachedContext))
        {
            return cachedContext;
        }

        // Try to load from persistent store
        var storedContext = _store.GetAsync(contextKey).GetAwaiter().GetResult();
        if (storedContext != null)
        {
            _logger.LogInformation("Loaded conversation from store: {Key}", contextKey);
            _localCache[contextKey] = storedContext;
            return storedContext;
        }

        // Create new context
        var context = new ConversationContext
        {
            ThreadId = threadId,
            ChannelId = channelId,
            ChatHistory = new ChatHistory(_systemPrompt)
        };

        _localCache[contextKey] = context;
        _logger.LogDebug("Created new conversation: {Key}", contextKey);

        return context;
    }

    /// <summary>
    /// Adds a user message to the conversation history
    /// </summary>
    public void AddUserMessage(ConversationContext context, string message)
    {
        context.ChatHistory.AddUserMessage(message);
        context.LastActivity = DateTime.UtcNow;
        TrimHistory(context);
        PersistContext(context);
    }

    /// <summary>
    /// Adds an assistant message to the conversation history
    /// </summary>
    public void AddAssistantMessage(ConversationContext context, string message)
    {
        context.ChatHistory.AddAssistantMessage(message);
        context.LastActivity = DateTime.UtcNow;
        TrimHistory(context);
        PersistContext(context);
    }

    /// <summary>
    /// Persists a conversation context to the store
    /// </summary>
    private void PersistContext(ConversationContext context)
    {
        var contextKey = $"{context.ChannelId}:{context.ThreadId}";
        _ = Task.Run(async () =>
        {
            try
            {
                await _store.SaveAsync(contextKey, context);
                _logger.LogDebug("Persisted conversation: {Key}", contextKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist conversation: {Key}", contextKey);
            }
        });
    }

    /// <summary>
    /// Trims conversation history to max allowed messages
    /// </summary>
    private void TrimHistory(ConversationContext context)
    {
        // Keep system message + max history messages
        var maxTotal = _maxHistoryMessages + 1; // +1 for system message

        while (context.ChatHistory.Count > maxTotal)
        {
            // Remove the oldest message (but keep the system message at index 0)
            context.ChatHistory.RemoveAt(1);
        }
    }

    /// <summary>
    /// Cleans up old conversations from cache and storage
    /// </summary>
    public async Task CleanupOldConversationsAsync()
    {
        // Clean up local cache
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var keysToRemove = _localCache
            .Where(kvp => kvp.Value.LastActivity < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _localCache.TryRemove(key, out _);
            _logger.LogDebug("Removed expired conversation from cache: {Key}", key);
        }

        // Clean up persistent store
        try
        {
            await _store.CleanupExpiredAsync();
            _logger.LogDebug("Cleaned up expired conversations from store");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired conversations from store");
        }
    }

    /// <summary>
    /// Synchronous wrapper for CleanupOldConversationsAsync (for backward compatibility)
    /// </summary>
    public void CleanupOldConversations()
    {
        CleanupOldConversationsAsync().GetAwaiter().GetResult();
    }
}

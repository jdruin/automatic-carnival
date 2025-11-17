using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;
using SlackAiAgent.Models;

namespace SlackAiAgent.Services;

/// <summary>
/// Manages multiple conversation contexts for different Slack threads
/// </summary>
public class ConversationManager
{
    private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();
    private readonly int _maxHistoryMessages;
    private readonly string _systemPrompt;

    public ConversationManager(int maxHistoryMessages, string systemPrompt)
    {
        _maxHistoryMessages = maxHistoryMessages;
        _systemPrompt = systemPrompt;
    }

    /// <summary>
    /// Gets or creates a conversation context for a specific thread
    /// </summary>
    public ConversationContext GetOrCreateContext(string channelId, string? threadTs)
    {
        // Use thread timestamp as unique identifier, or channel if no thread
        var threadId = threadTs ?? channelId;
        var contextKey = $"{channelId}:{threadId}";

        return _conversations.GetOrAdd(contextKey, key =>
        {
            var context = new ConversationContext
            {
                ThreadId = threadId,
                ChannelId = channelId,
                ChatHistory = new ChatHistory(_systemPrompt)
            };
            return context;
        });
    }

    /// <summary>
    /// Adds a user message to the conversation history
    /// </summary>
    public void AddUserMessage(ConversationContext context, string message)
    {
        context.ChatHistory.AddUserMessage(message);
        context.LastActivity = DateTime.UtcNow;
        TrimHistory(context);
    }

    /// <summary>
    /// Adds an assistant message to the conversation history
    /// </summary>
    public void AddAssistantMessage(ConversationContext context, string message)
    {
        context.ChatHistory.AddAssistantMessage(message);
        context.LastActivity = DateTime.UtcNow;
        TrimHistory(context);
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
    /// Cleans up old conversations (older than 24 hours)
    /// </summary>
    public void CleanupOldConversations()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var keysToRemove = _conversations
            .Where(kvp => kvp.Value.LastActivity < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _conversations.TryRemove(key, out _);
        }
    }
}

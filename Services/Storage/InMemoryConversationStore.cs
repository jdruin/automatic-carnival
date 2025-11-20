using System.Collections.Concurrent;
using SlackAiAgent.Models;

namespace SlackAiAgent.Services.Storage;

/// <summary>
/// In-memory conversation storage (fallback when Redis is disabled)
/// </summary>
public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();

    public Task<ConversationContext?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(key, out var context);
        return Task.FromResult(context);
    }

    public Task SaveAsync(string key, ConversationContext context, CancellationToken cancellationToken = default)
    {
        _conversations[key] = context;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _conversations.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
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

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllKeysAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<string>>(_conversations.Keys.ToList());
    }
}

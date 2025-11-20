using SlackAiAgent.Models;

namespace SlackAiAgent.Services.Storage;

/// <summary>
/// Interface for conversation context storage
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Gets a conversation context by key
    /// </summary>
    Task<ConversationContext?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a conversation context
    /// </summary>
    Task SaveAsync(string key, ConversationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation context
    /// </summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all expired conversations
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all conversation keys
    /// </summary>
    Task<IEnumerable<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);
}

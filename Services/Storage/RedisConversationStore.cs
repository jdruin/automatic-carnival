using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using StackExchange.Redis;
using SlackAiAgent.Configuration;
using SlackAiAgent.Models;

namespace SlackAiAgent.Services.Storage;

/// <summary>
/// Redis-based conversation storage for persistence across restarts
/// </summary>
public class RedisConversationStore : IConversationStore
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisConversationStore> _logger;
    private readonly int _expirationHours;
    private const string KeyPrefix = "slack:conversation:";

    public RedisConversationStore(
        IConnectionMultiplexer redis,
        RedisSettings settings,
        ILogger<RedisConversationStore> logger)
    {
        _database = redis.GetDatabase(settings.DatabaseNumber);
        _expirationHours = settings.ExpirationHours;
        _logger = logger;
    }

    public async Task<ConversationContext?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisKey = GetRedisKey(key);
            var value = await _database.StringGetAsync(redisKey);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredConversationContext>(value!);
            if (stored == null)
            {
                return null;
            }

            return RestoreContext(stored);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation from Redis: {Key}", key);
            return null;
        }
    }

    public async Task SaveAsync(string key, ConversationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisKey = GetRedisKey(key);
            var stored = StoreContext(context);
            var json = JsonSerializer.Serialize(stored);

            var expiration = TimeSpan.FromHours(_expirationHours);
            await _database.StringSetAsync(redisKey, json, expiration);

            _logger.LogDebug("Saved conversation to Redis: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation to Redis: {Key}", key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisKey = GetRedisKey(key);
            await _database.KeyDeleteAsync(redisKey);
            _logger.LogDebug("Deleted conversation from Redis: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation from Redis: {Key}", key);
        }
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Redis handles expiration automatically via TTL
        // This method is here for interface compatibility
        _logger.LogDebug("Redis handles expiration automatically via TTL");
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: KeyPrefix + "*");
            return keys.Select(k => k.ToString().Replace(KeyPrefix, "")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all keys from Redis");
            return Enumerable.Empty<string>();
        }
    }

    private string GetRedisKey(string key) => KeyPrefix + key;

    private StoredConversationContext StoreContext(ConversationContext context)
    {
        return new StoredConversationContext
        {
            ThreadId = context.ThreadId,
            ChannelId = context.ChannelId,
            LastActivity = context.LastActivity,
            Messages = context.ChatHistory.Select(m => new StoredChatMessage
            {
                Role = m.Role.Label,
                Content = m.Content ?? string.Empty
            }).ToList()
        };
    }

    private ConversationContext RestoreContext(StoredConversationContext stored)
    {
        var chatHistory = new ChatHistory();

        foreach (var msg in stored.Messages)
        {
            if (msg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddSystemMessage(msg.Content);
            }
            else if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        return new ConversationContext
        {
            ThreadId = stored.ThreadId,
            ChannelId = stored.ChannelId,
            ChatHistory = chatHistory,
            LastActivity = stored.LastActivity
        };
    }

    /// <summary>
    /// Serializable representation of ConversationContext
    /// </summary>
    private class StoredConversationContext
    {
        public string ThreadId { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
        public List<StoredChatMessage> Messages { get; set; } = new();
    }

    /// <summary>
    /// Serializable representation of ChatMessageContent
    /// </summary>
    private class StoredChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}

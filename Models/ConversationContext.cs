using Microsoft.SemanticKernel.ChatCompletion;

namespace SlackAiAgent.Models;

/// <summary>
/// Represents a conversation context for a Slack thread
/// </summary>
public class ConversationContext
{
    public string ThreadId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public ChatHistory ChatHistory { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

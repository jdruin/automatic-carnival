using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SlackNet;
using SlackNet.WebApi;

namespace SlackAiAgent.Plugins;

/// <summary>
/// Plugin that provides Slack-specific operations
/// </summary>
public class SlackPlugin
{
    private readonly ISlackApiClient? _slackClient;
    private readonly ILogger<SlackPlugin> _logger;

    public SlackPlugin(ISlackApiClient? slackClient, ILogger<SlackPlugin> logger)
    {
        _slackClient = slackClient;
        _logger = logger;
    }

    [KernelFunction("list_channels")]
    [Description("Lists all public channels in the Slack workspace")]
    public async Task<string> ListChannels()
    {
        try
        {
            if (_slackClient == null)
            {
                return "Error: Slack client not initialized";
            }

            var channels = await _slackClient.Conversations.List(
                types: new[] { ConversationType.PublicChannel },
                limit: 20);

            if (channels.Channels.Count == 0)
            {
                return "No public channels found";
            }

            var channelList = string.Join("\n", channels.Channels
                .Where(c => !c.IsArchived)
                .Select(c => $"- #{c.Name}: {c.Purpose?.Value ?? "No description"}"));

            return $"Public channels:\n{channelList}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing channels");
            return "Error: Could not list channels";
        }
    }

    [KernelFunction("get_channel_members")]
    [Description("Gets the list of members in a specific channel")]
    public async Task<string> GetChannelMembers(
        [Description("The channel ID or name (without #)")] string channel)
    {
        try
        {
            if (_slackClient == null)
            {
                return "Error: Slack client not initialized";
            }

            // If channel doesn't start with C (channel ID), try to find it by name
            string channelId = channel;
            if (!channel.StartsWith("C"))
            {
                var channels = await _slackClient.Conversations.List(
                    types: new[] { ConversationType.PublicChannel, ConversationType.PrivateChannel });
                var targetChannel = channels.Channels
                    .FirstOrDefault(c => c.Name.Equals(channel.TrimStart('#'), StringComparison.OrdinalIgnoreCase));

                if (targetChannel == null)
                {
                    return $"Error: Channel '{channel}' not found";
                }
                channelId = targetChannel.Id;
            }

            var members = await _slackClient.Conversations.Members(channelId);
            return $"Channel has {members.Members.Count} members";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel members");
            return "Error: Could not get channel members";
        }
    }

    [KernelFunction("get_user_info")]
    [Description("Gets information about a Slack user")]
    public async Task<string> GetUserInfo(
        [Description("The user ID")] string userId)
    {
        try
        {
            if (_slackClient == null)
            {
                return "Error: Slack client not initialized";
            }

            var user = await _slackClient.Users.Info(userId);

            var info = $"User: {user.Name}\n";
            info += $"Real Name: {user.RealName}\n";
            info += $"Display Name: {user.Profile.DisplayName}\n";
            info += $"Status: {user.Profile.StatusText}";

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            return "Error: Could not get user information";
        }
    }

    [KernelFunction("search_messages")]
    [Description("Searches for messages in Slack")]
    public async Task<string> SearchMessages(
        [Description("The search query")] string query,
        [Description("Maximum number of results to return (default 5)")] int count = 5)
    {
        try
        {
            if (_slackClient == null)
            {
                return "Error: Slack client not initialized";
            }

            var results = await _slackClient.Search.Messages(query, count: count);

            if (results.Messages.Matches.Count == 0)
            {
                return $"No messages found matching '{query}'";
            }

            var messages = string.Join("\n\n", results.Messages.Matches.Select(m =>
                $"From {m.Username}: {m.Text}"));

            return $"Found {results.Messages.Matches.Count} messages:\n{messages}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages");
            return "Error: Could not search messages";
        }
    }
}

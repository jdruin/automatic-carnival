namespace SlackAiAgent.Configuration;

public class AppSettings
{
    public SlackSettings Slack { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public AgentSettings Agent { get; set; } = new();
}

public class SlackSettings
{
    public string AppToken { get; set; } = string.Empty;
    public string BotToken { get; set; } = string.Empty;
}

public class AISettings
{
    public string Provider { get; set; } = "Ollama"; // "Ollama" or "Bedrock"
    public OllamaSettings Ollama { get; set; } = new();
    public BedrockSettings Bedrock { get; set; } = new();
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelId { get; set; } = "llama3.2";
}

public class BedrockSettings
{
    public string Region { get; set; } = "us-east-1";
    public string ModelId { get; set; } = "anthropic.claude-3-5-sonnet-20241022-v2:0";
}

public class AgentSettings
{
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public int MaxHistoryMessages { get; set; } = 10;
}

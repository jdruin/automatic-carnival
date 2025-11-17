# Slack AI Agent with Microsoft Semantic Kernel

A powerful Slack bot that uses **Microsoft Semantic Kernel** (Microsoft Agent Framework) to provide AI-powered assistance in your Slack workspace. The agent supports both **Amazon Bedrock** and **Ollama** as LLM providers and can maintain multiple independent conversations in the same Slack channel using thread tracking.

## Features

- **Microsoft Semantic Kernel Integration**: Built on Microsoft's Agent Framework for robust AI orchestration
- **Multiple LLM Providers**:
  - Amazon Bedrock (Claude models)
  - Ollama (local LLM deployment)
- **Multi-Conversation Support**: Maintains separate conversation contexts for different threads in the same channel
- **Thread-Aware**: Automatically tracks and maintains conversation history per thread
- **Real-time Communication**: Uses Slack Socket Mode for instant message handling
- **Conversation Memory**: Configurable message history with automatic cleanup
- **Tool/Function Calling**: Agent can automatically invoke tools to enhance capabilities
- **No Bot Framework**: Pure Semantic Kernel implementation without Microsoft Bot Framework

## Available Tools & Capabilities

The agent comes with built-in plugins that extend its capabilities through automatic function calling:

### 🗓️ DateTime Plugin
- **get_current_time**: Get current time in any timezone
- **get_current_date**: Get today's date
- **get_day_of_week**: Find what day of the week a date falls on
- **calculate_date_difference**: Calculate days between two dates

Example: *"What time is it in Tokyo?"* or *"How many days until Christmas?"*

### 🔢 Calculator Plugin
- **add**, **subtract**, **multiply**, **divide**: Basic arithmetic
- **power**: Raise numbers to a power
- **square_root**: Calculate square roots
- **percentage**: Calculate percentages

Example: *"What's 15% of 250?"* or *"Calculate 2 to the power of 10"*

### 📝 Text Utility Plugin
- **count_words**: Count words in text
- **reverse_text**: Reverse character order
- **to_uppercase**, **to_lowercase**, **to_title_case**: Text formatting
- **encode_base64**, **decode_base64**: Base64 encoding/decoding
- **generate_random_string**: Generate random strings

Example: *"Count the words in this message"* or *"Convert this to base64"*

### 💬 Slack Plugin
- **list_channels**: List all public channels
- **get_channel_members**: Get member count for a channel
- **get_user_info**: Get information about a user
- **search_messages**: Search for messages in Slack

Example: *"List all channels"* or *"How many members are in #general?"*

The agent automatically determines when to use these tools based on your questions and can chain multiple tool calls together to solve complex tasks.

## Architecture

```
┌─────────────────┐
│  Slack Client   │
└────────┬────────┘
         │ Socket Mode
         │
┌────────▼────────────────────────────────┐
│         SlackService                     │
│  - Event handling                        │
│  - Message routing                       │
│  - Thread management                     │
└────────┬────────────────────────────────┘
         │
┌────────▼────────────────────────────────┐
│    ConversationManager                   │
│  - Thread-based context tracking         │
│  - Chat history management               │
│  - Automatic cleanup                     │
└────────┬────────────────────────────────┘
         │
┌────────▼────────────────────────────────┐
│      AgentOrchestrator                   │
│  - Semantic Kernel setup                 │
│  - Plugin registration                   │
│  - Auto function calling                 │
└────────┬────────────────────────────────┘
         │
         ├─────────────────────────┐
         │                         │
┌────────▼────────────┐   ┌────────▼────────────┐
│  LLM Providers      │   │  Tool Plugins       │
│  - Ollama           │   │  - DateTime         │
│  - Bedrock (Claude) │   │  - Calculator       │
└─────────────────────┘   │  - TextUtility      │
                          │  - Slack            │
                          └─────────────────────┘
```

## Prerequisites

- .NET 8.0 SDK or later
- One of the following:
  - **Ollama** (for local LLM deployment)
  - **AWS Account** with Bedrock access (for cloud-based LLM)
- Slack workspace with admin access

## Slack App Setup

1. **Create a Slack App**:
   - Go to https://api.slack.com/apps
   - Click "Create New App" → "From scratch"
   - Name your app and select your workspace

2. **Enable Socket Mode**:
   - Go to "Socket Mode" in the left sidebar
   - Toggle "Enable Socket Mode"
   - Generate an app-level token with `connections:write` scope
   - Save this as your `SLACK_APP_TOKEN`

3. **Add Bot Scopes**:
   - Go to "OAuth & Permissions"
   - Add the following Bot Token Scopes:
     - `app_mentions:read`
     - `chat:write`
     - `im:history`
     - `im:read`
     - `im:write`

4. **Subscribe to Events**:
   - Go to "Event Subscriptions"
   - Toggle "Enable Events"
   - Under "Subscribe to bot events", add:
     - `app_mention`
     - `message.im`
     - `message.channels`

5. **Install App to Workspace**:
   - Go to "Install App"
   - Click "Install to Workspace"
   - Save the "Bot User OAuth Token" as your `SLACK_BOT_TOKEN`

## Configuration

### Option 1: Using appsettings.json

Edit `appsettings.json`:

```json
{
  "Slack": {
    "AppToken": "xapp-1-xxxxx-xxxxx-xxxxx",
    "BotToken": "xoxb-xxxxx-xxxxx-xxxxx"
  },
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    },
    "Bedrock": {
      "Region": "us-east-1",
      "ModelId": "anthropic.claude-3-5-sonnet-20241022-v2:0"
    }
  },
  "Agent": {
    "SystemPrompt": "You are a helpful AI assistant in a Slack workspace. Be concise and professional.",
    "MaxHistoryMessages": 10
  }
}
```

### Option 2: Using Environment Variables

```bash
export Slack__AppToken="xapp-1-xxxxx-xxxxx-xxxxx"
export Slack__BotToken="xoxb-xxxxx-xxxxx-xxxxx"
export AI__Provider="Ollama"
export AI__Ollama__Endpoint="http://localhost:11434"
export AI__Ollama__ModelId="llama3.2"
```

### Configuration Parameters

#### Slack Settings
- `Slack:AppToken`: Your Slack app-level token (starts with `xapp-`)
- `Slack:BotToken`: Your bot user OAuth token (starts with `xoxb-`)

#### AI Provider Settings
- `AI:Provider`: Choose `"Ollama"` or `"Bedrock"`

**For Ollama:**
- `AI:Ollama:Endpoint`: Ollama server URL (default: `http://localhost:11434`)
- `AI:Ollama:ModelId`: Model name (e.g., `llama3.2`, `mistral`, `codellama`)

**For Bedrock:**
- `AI:Bedrock:Region`: AWS region (e.g., `us-east-1`, `us-west-2`)
- `AI:Bedrock:ModelId`: Bedrock model ID (e.g., `anthropic.claude-3-5-sonnet-20241022-v2:0`)

#### Agent Settings
- `Agent:SystemPrompt`: The system prompt that defines the agent's behavior
- `Agent:MaxHistoryMessages`: Maximum number of messages to keep in conversation history (default: 10)

## Installation & Running

### Using Ollama (Local)

1. **Install Ollama**:
   ```bash
   # macOS/Linux
   curl -fsSL https://ollama.ai/install.sh | sh

   # Or visit https://ollama.ai for other platforms
   ```

2. **Pull a model**:
   ```bash
   ollama pull llama3.2
   ```

3. **Build and run the agent**:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

### Using Amazon Bedrock

1. **Configure AWS credentials**:
   ```bash
   aws configure
   # Or set environment variables:
   export AWS_ACCESS_KEY_ID="your-key"
   export AWS_SECRET_ACCESS_KEY="your-secret"
   export AWS_REGION="us-east-1"
   ```

2. **Ensure Bedrock model access**:
   - Go to AWS Console → Bedrock → Model access
   - Request access to Claude models if needed

3. **Update configuration**:
   - Set `AI:Provider` to `"Bedrock"`
   - Configure `AI:Bedrock:Region` and `AI:Bedrock:ModelId`

4. **Build and run the agent**:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

## Usage

### Direct Messages
Simply send a message to the bot in a DM, and it will respond.

### Channel Mentions
In a channel where the bot is a member, mention the bot:
```
@YourBotName What is the capital of France?
```

### Threaded Conversations
The bot automatically maintains separate conversation contexts for different threads:

1. **Thread A**: User asks about Python
   - Bot remembers this conversation context in this thread

2. **Thread B** (different thread, same channel): User asks about JavaScript
   - Bot maintains a separate conversation context here

3. Each thread maintains its own conversation history independently

### Example Conversations

**Basic Conversation:**
```
User: @Bot What is Semantic Kernel?
Bot: Semantic Kernel is Microsoft's Agent Framework that allows you to
     orchestrate AI services and integrate them with conventional programming...

User (in same thread): Can you give me an example in C#?
Bot: Sure! Here's a simple example using Semantic Kernel in C#...
     [Bot remembers the context about Semantic Kernel from previous message]
```

**Tool Calling Examples:**
```
User: @Bot What time is it in New York?
Bot: [Automatically calls get_current_time("America/New_York")]
     Current time in Eastern Standard Time: 2025-01-15 14:30:00

User: @Bot Calculate 15% of 250
Bot: [Automatically calls percentage(15, 250)]
     37.50

User: @Bot How many days until Christmas?
Bot: [Automatically calls get_current_date() and calculate_date_difference()]
     There are 343 days between today and 2025-12-25.

User: @Bot List all channels
Bot: [Automatically calls list_channels()]
     Public channels:
     - #general: Company-wide announcements
     - #random: Water cooler conversations
     - #engineering: Engineering discussions
```

The agent automatically determines which tools to use based on your question and can chain multiple tool calls together for complex requests.

## Project Structure

```
SlackAiAgent/
├── Program.cs                          # Application entry point
├── SlackAiAgent.csproj                # Project file
├── appsettings.json                   # Configuration
├── Configuration/
│   └── AppSettings.cs                 # Configuration models
├── Models/
│   └── ConversationContext.cs         # Conversation state models
├── Services/
│   ├── AgentOrchestrator.cs           # AI agent with tool calling
│   ├── ConversationManager.cs         # Multi-thread conversation tracking
│   ├── SlackService.cs                # Slack integration
│   └── AI/
│       ├── AIServiceFactory.cs        # LLM provider factory
│       ├── OllamaChatCompletion.cs    # Ollama connector
│       └── BedrockChatCompletion.cs   # Bedrock connector
├── Plugins/
│   ├── DateTimePlugin.cs              # Date/time operations
│   ├── CalculatorPlugin.cs            # Mathematical calculations
│   ├── TextUtilityPlugin.cs           # Text manipulation
│   └── SlackPlugin.cs                 # Slack API operations
```

## Key Components

### AgentOrchestrator
Orchestrates the AI agent with automatic tool calling:
- Sets up Semantic Kernel with chat completion service
- Registers and manages all plugins/tools
- Configures automatic function calling behavior
- Handles tool invocation and response streaming

### ConversationManager
Manages multiple independent conversation contexts based on Slack thread IDs:
- Maintains separate chat histories per thread
- Automatically trims old messages based on `MaxHistoryMessages`
- Cleans up inactive conversations after 24 hours

### SlackService
Handles Slack integration:
- Connects via Socket Mode for real-time events
- Routes messages to appropriate conversation contexts
- Manages bot mentions and direct messages
- Provides Slack client to plugins

### AI Service Implementations
Both Ollama and Bedrock connectors implement `IChatCompletionService` from Semantic Kernel:
- Support for streaming responses
- Automatic message format conversion
- Error handling and retry logic

### Plugins
Extensible tool system for expanding agent capabilities:
- **DateTimePlugin**: Time and date operations
- **CalculatorPlugin**: Mathematical calculations
- **TextUtilityPlugin**: String and text manipulation
- **SlackPlugin**: Slack workspace operations

Each plugin uses Semantic Kernel's `[KernelFunction]` attributes for automatic discovery and invocation.

## Troubleshooting

### Bot doesn't respond
- Check that the bot is invited to the channel: `/invite @BotName`
- Verify Socket Mode is enabled in Slack app settings
- Ensure event subscriptions are configured correctly

### "Configuration validation failed"
- Verify all required settings in `appsettings.json`
- Check environment variables are set correctly
- Ensure tokens start with correct prefixes (`xapp-` and `xoxb-`)

### Ollama connection errors
- Verify Ollama is running: `ollama list`
- Check endpoint URL matches Ollama server
- Ensure firewall allows connections to Ollama port

### Bedrock authorization errors
- Verify AWS credentials are configured
- Check IAM permissions for Bedrock access
- Ensure model access is granted in AWS Console

## Development

### Adding New Features

1. **Custom Prompts**: Modify `Agent:SystemPrompt` in configuration
2. **New LLM Providers**: Implement `IChatCompletionService` interface
3. **Enhanced Context**: Extend `ConversationContext` model
4. **Slack Features**: Add handlers in `SlackService`

### Testing Locally

```bash
# Run with verbose logging
dotnet run --environment Development

# Test with different models
# Edit appsettings.json and change AI:Ollama:ModelId
dotnet run
```

## Performance Considerations

- **Memory**: Each conversation context stores up to `MaxHistoryMessages` in memory
- **Cleanup**: Inactive conversations (>24 hours) are automatically cleaned
- **Threading**: Socket Mode runs message handlers concurrently
- **Rate Limits**: Respects Slack API rate limits automatically

## Security Notes

- Never commit `appsettings.json` with real tokens to source control
- Use environment variables for production deployments
- Rotate Slack tokens periodically
- For Bedrock, use IAM roles instead of access keys when possible

## License

This project is provided as-is for educational and commercial use.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Support

For issues related to:
- **Semantic Kernel**: https://github.com/microsoft/semantic-kernel
- **Slack API**: https://api.slack.com/support
- **Ollama**: https://github.com/ollama/ollama
- **AWS Bedrock**: AWS Support

## Acknowledgments

- Built with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- Slack integration via [SlackNet](https://github.com/soxtoby/SlackNet)
- Supports [Ollama](https://ollama.ai) and [AWS Bedrock](https://aws.amazon.com/bedrock/)

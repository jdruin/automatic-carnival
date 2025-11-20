# Slack AI Agent with Microsoft Semantic Kernel

A powerful Slack bot that uses **Microsoft Semantic Kernel** (Microsoft Agent Framework) to provide AI-powered assistance in your Slack workspace. The agent supports both **Amazon Bedrock** and **Ollama** as LLM providers and can maintain multiple independent conversations in the same Slack channel using thread tracking.

## Features

- **Native Semantic Kernel Agent**: Uses `ChatCompletionAgent` from Microsoft Semantic Kernel for true agentic behavior
- **Thinking Mode Support**: Optional logging of reasoning/thinking process for supported models (like OpenAI o1)
- **Multiple LLM Providers**:
  - Amazon Bedrock (Claude models)
  - Ollama (local LLM deployment)
- **Multi-Conversation Support**: Maintains separate conversation contexts for different threads in the same channel
- **Thread-Aware**: Automatically tracks and maintains conversation history per thread
- **Real-time Communication**: Uses Slack Socket Mode for instant message handling
- **Conversation Memory**: Configurable message history with automatic cleanup
- **Redis Persistence**: Optional Redis integration to persist conversations across agent restarts
- **Context Rebuild**: Automatically rebuild conversation context from Slack thread history when missing
- **Tool/Function Calling**: Agent can automatically invoke tools to enhance capabilities
- **Pure Semantic Kernel**: 100% Microsoft Semantic Kernel implementation, no Bot Framework dependency

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
│  - Local cache + persistence             │
└────────┬────────────────────────────────┘
         │
         ├─────────────────────────┐
         │                         │
┌────────▼────────────┐   ┌────────▼────────────┐
│  Storage Layer      │   │ AgentOrchestrator   │
│  - Redis (optional) │   │ - SK setup          │
│  - In-Memory        │   │ - Plugin registry   │
│  - Auto failover    │   │ - Auto tool call    │
└─────────────────────┘   └────────┬────────────┘
                                   │
                 ├─────────────────┴────────────┐
                 │                              │
        ┌────────▼────────────┐   ┌─────────▼──────────┐
        │  LLM Providers      │   │  Tool Plugins      │
        │  - Ollama           │   │  - DateTime        │
        │  - Bedrock (Claude) │   │  - Calculator      │
        └─────────────────────┘   │  - TextUtility     │
                                  │  - Slack           │
                                  └────────────────────┘
```

## Prerequisites

- .NET 8.0 SDK or later
- One of the following:
  - **Ollama** (for local LLM deployment)
  - **AWS Account** with Bedrock access (for cloud-based LLM)
- Slack workspace with admin access
- **Redis** (optional, for conversation persistence across restarts)

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
    "MaxHistoryMessages": 10,
    "LogThinking": false,
    "RebuildContextFromSlack": true
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": "localhost:6379",
    "DatabaseNumber": 0,
    "ExpirationHours": 24
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
export Redis__Enabled="true"
export Redis__ConnectionString="localhost:6379"
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
- `Agent:LogThinking`: Enable logging of agent's thinking/reasoning process (default: false)
  - Useful for debugging and understanding agent decisions
  - Supported by thinking models like OpenAI o1
  - Logs appear in console output with clear markers
- `Agent:RebuildContextFromSlack`: Rebuild conversation context from Slack thread history when missing (default: true)
  - Automatically fetches and reconstructs conversation history from Slack when context is lost
  - Useful after agent restarts when using in-memory storage
  - Respects `MaxHistoryMessages` limit when rebuilding
  - Skips "_Thinking..._" placeholder messages

#### Redis Settings (Optional)
- `Redis:Enabled`: Enable Redis for conversation persistence (default: false)
- `Redis:ConnectionString`: Redis connection string (default: "localhost:6379")
- `Redis:DatabaseNumber`: Redis database number to use (default: 0)
- `Redis:ExpirationHours`: Hours before conversations expire (default: 24)

**Why use Redis?**
- **Persistence**: Conversations survive agent restarts and deployments
- **Scalability**: Run multiple agent instances sharing the same conversation state
- **Reliability**: Automatic expiration prevents memory leaks from abandoned conversations

**When Redis is disabled:**
- Conversations are stored in-memory only
- All conversation history is lost when the agent restarts
- Suitable for development and testing

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

### Using Redis for Persistence (Optional)

Redis provides conversation persistence across agent restarts. This is especially useful for production deployments.

1. **Install Redis**:
   ```bash
   # macOS (using Homebrew)
   brew install redis
   brew services start redis

   # Ubuntu/Debian
   sudo apt-get install redis-server
   sudo systemctl start redis

   # Docker
   docker run -d -p 6379:6379 redis:latest

   # Or use a managed Redis service (AWS ElastiCache, Azure Cache, etc.)
   ```

2. **Enable Redis in configuration**:
   ```json
   {
     "Redis": {
       "Enabled": true,
       "ConnectionString": "localhost:6379",
       "DatabaseNumber": 0,
       "ExpirationHours": 24
     }
   }
   ```

3. **Verify Redis connection**:
   When you start the agent with Redis enabled, you'll see:
   ```
   Connecting to Redis: localhost:6379
   Redis conversation persistence enabled
   ```

4. **Test persistence**:
   - Start a conversation with the bot in Slack
   - Restart the agent (Ctrl+C and `dotnet run`)
   - Continue the conversation - the bot will remember the context!

**Redis Connection Strings:**
- Local: `localhost:6379`
- Remote: `redis.example.com:6379`
- With password: `redis.example.com:6379,password=yourpassword`
- SSL: `redis.example.com:6380,ssl=true,password=yourpassword`

### Context Rebuild from Slack History

When conversation context is missing (e.g., after restart with in-memory storage or Redis data loss), the agent can automatically rebuild it from Slack's thread history.

**How it works:**
1. Agent detects that a conversation context only contains the system message
2. If in a thread and `RebuildContextFromSlack` is enabled, fetches thread history from Slack
3. Reconstructs the conversation by processing messages in chronological order
4. Distinguishes between user and bot messages to rebuild chat history correctly
5. Respects `MaxHistoryMessages` limit - takes the most recent messages

**Example scenario:**
```
1. User starts thread: "What is Semantic Kernel?"
2. Bot responds: "Semantic Kernel is..."
3. User continues: "Can you give an example?"
4. Agent restarts (context lost)
5. User asks: "What about in Python?"
6. Agent fetches messages 1-3 from Slack, rebuilds context
7. Agent responds with context from the entire conversation
```

**Configuration:**
```json
{
  "Agent": {
    "RebuildContextFromSlack": true
  }
}
```

Set to `false` to disable automatic rebuild (conversation will start fresh after context loss).

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
│   ├── AI/
│   │   ├── AIServiceFactory.cs        # LLM provider factory
│   │   ├── OllamaChatCompletion.cs    # Ollama connector
│   │   └── BedrockChatCompletion.cs   # Bedrock connector
│   └── Storage/
│       ├── IConversationStore.cs      # Storage interface
│       ├── RedisConversationStore.cs  # Redis persistence
│       └── InMemoryConversationStore.cs # In-memory fallback
├── Plugins/
│   ├── DateTimePlugin.cs              # Date/time operations
│   ├── CalculatorPlugin.cs            # Mathematical calculations
│   ├── TextUtilityPlugin.cs           # Text manipulation
│   └── SlackPlugin.cs                 # Slack API operations
└── SlackAiAgent.Tests/                # Unit tests
    ├── SlackAiAgent.Tests.csproj      # Test project file
    ├── Configuration/
    │   └── AppSettingsTests.cs        # Configuration tests
    ├── Services/
    │   ├── ConversationManagerTests.cs # Conversation tests
    │   └── AI/
    │       └── AIServiceFactoryTests.cs # AI service tests
    └── Plugins/
        ├── DateTimePluginTests.cs     # DateTime plugin tests
        ├── CalculatorPluginTests.cs   # Calculator plugin tests
        ├── TextUtilityPluginTests.cs  # Text utility tests
        └── SlackPluginTests.cs        # Slack plugin tests
```

## Key Components

### AgentOrchestrator
Orchestrates the AI agent using Microsoft Semantic Kernel's native `ChatCompletionAgent`:
- Creates and configures a `ChatCompletionAgent` with system instructions
- Sets up Semantic Kernel kernel with chat completion service
- Registers and manages all plugins/tools
- Provides automatic tool invocation via `ToolCallBehavior.AutoInvokeKernelFunctions`
- Supports thinking mode to capture reasoning process from thinking models
- Handles both standard and streaming agent responses

### ConversationManager
Manages multiple independent conversation contexts based on Slack thread IDs:
- Maintains separate chat histories per thread
- Automatically trims old messages based on `MaxHistoryMessages`
- Cleans up inactive conversations after 24 hours
- Uses two-tier caching: local in-memory cache + persistent storage
- Automatically persists changes to storage layer asynchronously

### SlackService
Handles Slack integration:
- Connects via Socket Mode for real-time events
- Routes messages to appropriate conversation contexts
- Manages bot mentions and direct messages
- Automatically rebuilds conversation context from Slack thread history when missing
- Provides Slack client to plugins

### Storage Implementations
Two storage backends are available for conversation persistence:

**RedisConversationStore**:
- Persists conversations to Redis with JSON serialization
- Automatic TTL-based expiration (configurable via `ExpirationHours`)
- Converts `ChatHistory` to serializable format for storage
- Used when `Redis:Enabled` is `true`

**InMemoryConversationStore**:
- Fallback implementation using `ConcurrentDictionary`
- No persistence across restarts
- Automatic cleanup of conversations older than 24 hours
- Used when `Redis:Enabled` is `false`

The ConversationManager abstracts storage through the `IConversationStore` interface, making it easy to add additional storage backends (e.g., SQL, CosmosDB) in the future.

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

### Redis connection errors
- Verify Redis is running: `redis-cli ping` (should return "PONG")
- Check Redis connection string is correct
- Ensure firewall allows connections to Redis port
- If Redis is unavailable, agent will fall back to in-memory storage
- Check agent console logs for "Using in-memory conversation storage" message

## Development

### Adding New Features

1. **Custom Prompts**: Modify `Agent:SystemPrompt` in configuration
2. **New LLM Providers**: Implement `IChatCompletionService` interface
3. **Enhanced Context**: Extend `ConversationContext` model
4. **Slack Features**: Add handlers in `SlackService`
5. **New Plugins**: Create classes with `[KernelFunction]` attributes

### Running Tests

The project includes comprehensive unit tests using xUnit, Moq, and FluentAssertions.

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~DateTimePluginTests"

# Run tests in watch mode (auto-rerun on changes)
dotnet watch test
```

**Test Coverage:**
- **ConversationManager**: Thread management, message history, cleanup
- **Plugins**: All plugin functions (DateTime, Calculator, TextUtility, Slack)
- **AI Services**: Service factory and provider creation
- **Configuration**: Settings validation and defaults

### Testing Locally

```bash
# Run with verbose logging
dotnet run --environment Development

# Test with different models
# Edit appsettings.json and change AI:Ollama:ModelId
dotnet run
```

### Adding New Plugins

To add a new plugin:

1. Create a new class in the `Plugins/` directory
2. Add methods with `[KernelFunction]` and `[Description]` attributes
3. Register the plugin in `AgentOrchestrator.RegisterBasicPlugins()`
4. Write unit tests in `SlackAiAgent.Tests/Plugins/`

Example:
```csharp
public class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [Description("Gets the current weather for a location")]
    public string GetWeather(
        [Description("The city name")] string city)
    {
        // Implementation
        return $"Weather in {city}: Sunny, 72°F";
    }
}
```

## Performance Considerations

- **Memory**: Each conversation context stores up to `MaxHistoryMessages` in memory
- **Storage**: Conversations are persisted asynchronously to avoid blocking message handling
- **Caching**: Two-tier cache (local memory + Redis) minimizes storage round-trips
- **Cleanup**: Inactive conversations (>24 hours) are automatically cleaned from both cache and storage
- **Threading**: Socket Mode runs message handlers concurrently
- **Rate Limits**: Respects Slack API rate limits automatically
- **Redis**: When enabled, provides horizontal scalability for multiple agent instances

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

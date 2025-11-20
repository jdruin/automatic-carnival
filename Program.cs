using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SlackNet.Extensions.DependencyInjection;
using StackExchange.Redis;
using SlackAiAgent.Configuration;
using SlackAiAgent.Services;
using SlackAiAgent.Services.AI;
using SlackAiAgent.Services.Storage;

namespace SlackAiAgent;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Slack AI Agent...");
        Console.WriteLine();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Bind configuration
                var appSettings = new AppSettings();
                context.Configuration.Bind(appSettings);

                // Validate configuration
                ValidateConfiguration(appSettings);

                // Register services
                services.AddSingleton(appSettings);

                // Configure Redis and conversation storage
                if (appSettings.Redis.Enabled)
                {
                    services.AddSingleton<IConnectionMultiplexer>(provider =>
                    {
                        var settings = provider.GetRequiredService<AppSettings>();
                        Console.WriteLine($"Connecting to Redis: {settings.Redis.ConnectionString}");
                        return ConnectionMultiplexer.Connect(settings.Redis.ConnectionString);
                    });
                    services.AddSingleton<IConversationStore, RedisConversationStore>();
                    Console.WriteLine("Redis conversation persistence enabled");
                }
                else
                {
                    services.AddSingleton<IConversationStore, InMemoryConversationStore>();
                    Console.WriteLine("Using in-memory conversation storage (data will not persist across restarts)");
                }

                // Configure ConversationManager
                services.AddSingleton(provider =>
                {
                    var settings = provider.GetRequiredService<AppSettings>();
                    var store = provider.GetRequiredService<IConversationStore>();
                    var logger = provider.GetRequiredService<ILogger<ConversationManager>>();
                    return new ConversationManager(
                        settings.Agent.MaxHistoryMessages,
                        settings.Agent.SystemPrompt,
                        store,
                        logger);
                });

                // Configure AI Chat Completion Service
                services.AddSingleton(provider =>
                {
                    var settings = provider.GetRequiredService<AppSettings>();
                    return AIServiceFactory.CreateChatCompletionService(settings);
                });

                // Configure AgentOrchestrator
                services.AddSingleton<AgentOrchestrator>();

                // Configure SlackNet with Socket Mode
                services.AddSlackNet(s => s
                    .UseApiToken(appSettings.Slack.BotToken)
                    .UseAppLevelToken(appSettings.Slack.AppToken)
                    .RegisterEventHandler(ctx => ctx.ServiceProvider.GetRequiredService<SlackService>()));

                // Register SlackService
                services.AddSingleton<SlackService>();

                services.AddHttpClient();
                services.AddHostedService<SlackAgentHostedService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        await host.RunAsync();
    }

    private static void ValidateConfiguration(AppSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.Slack.AppToken))
            errors.Add("Slack:AppToken is required");

        if (string.IsNullOrWhiteSpace(settings.Slack.BotToken))
            errors.Add("Slack:BotToken is required");

        if (string.IsNullOrWhiteSpace(settings.AI.Provider))
            errors.Add("AI:Provider is required");

        if (settings.AI.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.AI.Ollama.Endpoint))
                errors.Add("AI:Ollama:Endpoint is required when using Ollama provider");
            if (string.IsNullOrWhiteSpace(settings.AI.Ollama.ModelId))
                errors.Add("AI:Ollama:ModelId is required when using Ollama provider");
        }
        else if (settings.AI.Provider.Equals("Bedrock", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.AI.Bedrock.Region))
                errors.Add("AI:Bedrock:Region is required when using Bedrock provider");
            if (string.IsNullOrWhiteSpace(settings.AI.Bedrock.ModelId))
                errors.Add("AI:Bedrock:ModelId is required when using Bedrock provider");
        }
        else
        {
            errors.Add($"Unknown AI provider: {settings.AI.Provider}. Supported: Ollama, Bedrock");
        }

        if (errors.Any())
        {
            Console.WriteLine("Configuration validation failed:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            Console.WriteLine();
            Console.WriteLine("Please update appsettings.json or set environment variables.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Configuration validated successfully:");
        Console.WriteLine($"  AI Provider: {settings.AI.Provider}");
        if (settings.AI.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  Ollama Endpoint: {settings.AI.Ollama.Endpoint}");
            Console.WriteLine($"  Ollama Model: {settings.AI.Ollama.ModelId}");
        }
        else if (settings.AI.Provider.Equals("Bedrock", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  Bedrock Region: {settings.AI.Bedrock.Region}");
            Console.WriteLine($"  Bedrock Model: {settings.AI.Bedrock.ModelId}");
        }
        Console.WriteLine($"  Max History Messages: {settings.Agent.MaxHistoryMessages}");
        Console.WriteLine($"  Tool Calling: Enabled");
        Console.WriteLine($"  Available Plugins: DateTime, Calculator, TextUtility, Slack");
        Console.WriteLine();
    }
}

/// <summary>
/// Hosted service to run the Slack agent
/// </summary>
public class SlackAgentHostedService : IHostedService
{
    private readonly SlackService _slackService;
    private readonly ILogger<SlackAgentHostedService> _logger;

    public SlackAgentHostedService(
        SlackService slackService,
        ILogger<SlackAgentHostedService> logger)
    {
        _slackService = slackService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Slack Agent...");
        await _slackService.StartAsync(cancellationToken);
        _logger.LogInformation("Slack Agent is running. Press Ctrl+C to stop.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Slack Agent...");
        return Task.CompletedTask;
    }
}

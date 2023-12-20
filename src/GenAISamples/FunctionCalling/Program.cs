using Azure.Identity;
using FunctionCalling.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Before running this sample, you must add the following secrets to the project via SecretManager:
// - AzureOpenAI:DeploymentName
// - AzureOpenAI:Endpoint
// - BingCustomSearch:SubscriptionKey
// - BingCustomSearch:CustomConfigId
// Command to add them: dotnet user-secrets set "AzureOpenAI:DeploymentName" "your deployment name"
// Documentation: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows

// Create the configuration object to read the user secrets from the local machine
var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

// Get the values from the user secrets
var deploymentName = configuration["AzureOpenAI:DeploymentName"];
var endpoint = configuration["AzureOpenAI:Endpoint"];
var bingSubscriptionKey = configuration["BingCustomSearch:SubscriptionKey"];
var bingCustomConfigId = configuration["BingCustomSearch:CustomConfigId"];

// Create the kernel and configure it
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, new DefaultAzureCredential());
builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

// Add the Bing Custom Search plugin
kernel.ImportPluginFromObject(new WebSearchEnginePlugin(bingSubscriptionKey, bingCustomConfigId), "bing");

// Create chat history
ChatHistory history = [];

// Get chat completion service
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Start the conversation
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    // Get user input
    Console.Write("User > ");
    history.AddUserMessage(Console.ReadLine()!);

    // Enable auto function calling
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // Get the response from the AI
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);


    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine();

    // Stream the results
    string fullMessage = "";
    var first = true;
    await foreach (var content in result)
    {
        if (content.Role.HasValue && first)
        {
            Console.Write("Assistant > ");
            first = false;
        }
        Console.Write(content.Content);
        fullMessage += content.Content;
    }
    Console.WriteLine();
    Console.WriteLine();

    // Add the message from the agent to the chat history
    history.AddAssistantMessage(fullMessage);
}
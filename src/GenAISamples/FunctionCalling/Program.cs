using FunctionCalling.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;

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
var openAIKey = configuration["OpenAI:ApiKey"];
var sqlConnectionString = configuration["SQL:ConnectionString"];

// Create the kernel and configure it
var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, new DefaultAzureCredential());
//builder.AddOpenAIChatCompletion("gpt-4o", openAIKey);
var aiEndpoint = new Uri("https://Meta-Llama-3-70B-Instruct-sgwjq.swedencentral.models.ai.azure.com/v1/chat/completions");
#pragma warning disable SKEXP0010 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.
builder.AddOpenAIChatCompletion("llama-3", aiEndpoint, "x0winCGjG18YQktwBkZ90bipczotPhTG");
//builder.AddMistralChatCompletion("llama-3", "gGNNs1nLrD5ZVzgGJlG1UZcggXH8fXS6", aiEndpoint);
#pragma warning restore SKEXP0010 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.
builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

//Add the Bing Custom Search plugin
kernel.ImportPluginFromObject(new WebSearchEnginePlugin(bingSubscriptionKey, bingCustomConfigId), "bing");
// Add the Leave plugin
kernel.ImportPluginFromObject(new LeavePlugin(), "leave");
// Add the SQL plugin
kernel.ImportPluginFromObject(new SQLPlugin(sqlConnectionString), "sql");
#pragma warning disable SKEXP0050
kernel.ImportPluginFromType<TimePlugin>();
#pragma warning restore SKEXP0050 

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
    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // Get the response from the AI
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        history,
        executionSettings: settings,
        kernel: kernel);


    Console.ForegroundColor = ConsoleColor.White;
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
    //AnsiConsole.MarkupLine(fullMessage);
    Console.WriteLine();
    Console.WriteLine();

    // Add the message from the agent to the chat history
    history.AddAssistantMessage(fullMessage);
}
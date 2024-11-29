using AIAgentsHelloWorld;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Spectre.Console;
using System.Text.Json;
using System.Text.RegularExpressions;

Settings settings = new();

IKernelBuilder builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion(
    settings.AzureOpenAI.ChatModelDeployment,
    settings.AzureOpenAI.Endpoint,
    settings.AzureOpenAI.ApiKey);



Kernel kernel = builder.Build();
Kernel toolKernel = kernel.Clone();

const string WriterName = "Writer";
const string CriticName = "Critic";

ChatCompletionAgent agentWriter =
    new()
    {
        Name = WriterName,
        Instructions =
            """
            You are a writer. You write engaging and concise 
            blogpost (with title) on given topics. You must polish your
            writing based on the feedback you receive and give a refined
            version. Only return your final work without additional comments.
            """,
        Kernel = toolKernel,
        Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
    };

ChatCompletionAgent agentCritic =
    new()
    {
        Name = CriticName,
        Instructions =
            """
            You are a critic. You review the work of
            the writer and provide constructive
            feedback to help improve the quality of the content.
            Never directly perform the correction or provide example.

            RULES:
            - Only identify suggestions that are specific and actionable.
            - Verify previous suggestions have been addressed.
            - Never repeat previous suggestions.
            """,
        Kernel = kernel
    };

KernelFunction selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the provided RESPONSE and choose the next participant.
        State only the name of the chosen participant without explanation.
        Never choose the participant named in the RESPONSE.

        Choose only from these participants:
        - {{{CriticName}}}
        - {{{WriterName}}}

        Always follow these rules when choosing the next participant:
        - If RESPONSE is user input, it is {{{WriterName}}}'s turn.
        - If RESPONSE is by {{{WriterName}}}, it is {{{CriticName}}}'s turn.
        - If RESPONSE is by {{{CriticName}}}, it is {{{WriterName}}}'s turn.


        RESPONSE:
        {{$lastmessage}}
        """,
        safeParameterNames: "lastmessage");

const string TerminationToken = "yes";

KernelFunction terminationFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the RESPONSE and determine whether the content has been deemed satisfactory.
        If content is satisfactory, respond with a single word without explanation: {{{TerminationToken}}}.
        If specific suggestions are being provided, it is not satisfactory.
        If no correction is suggested, it is satisfactory.

        RESPONSE:
        {{$lastmessage}}
        """,
        safeParameterNames: "lastmessage");

ChatHistoryTruncationReducer historyReducer = new(2);

AgentGroupChat chat =
    new(agentWriter, agentCritic)
    {
        ExecutionSettings = new AgentGroupChatSettings
        {
            SelectionStrategy =
                new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                {
                    // Always start with the editor agent.
                    InitialAgent = agentWriter,
                    // Save tokens by only including the final response
                    HistoryReducer = historyReducer,
                    // The prompt variable name for the history argument.
                    HistoryVariableName = "lastmessage",
                    // Returns the entire result value as a string.
                    ResultParser = (result) => result.GetValue<string>() ?? agentWriter.Name
                },
            TerminationStrategy =
                new KernelFunctionTerminationStrategy(terminationFunction, kernel)
                {
                    // Only evaluate for editor's response
                    Agents = [agentCritic],
                    // Save tokens by only including the final response
                    HistoryReducer = historyReducer,
                    // The prompt variable name for the history argument.
                    HistoryVariableName = "lastmessage",
                    // Limit total number of turns
                    MaximumIterations = 10
                    ,
                    // Customer result parser to determine if the response is "yes"
                    ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false
                }
        }
    };

bool isComplete = false;
AnsiConsole.Write(
         new FigletText("AI Agents")
             .Centered()
             .Color(Spectre.Console.Color.Cyan1));
AnsiConsole.WriteLine();
do
{
    var input = AnsiConsole.Prompt(
        new TextPrompt<string>("> ")
            .PromptStyle("green")
           .ValidationErrorMessage("[red]La saisie ne peut pas être vide")
           .Validate(input => !string.IsNullOrWhiteSpace(input))
    );

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }
    input = input.Trim();
    if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        isComplete = true;
        break;
    }

    if (input.Equals("RESET", StringComparison.OrdinalIgnoreCase))
    {
        await chat.ResetAsync();
        AnsiConsole.MarkupLine("[yellow][[La conversation a été réinitialisée]][/]");
        continue;
    }

    if (input.StartsWith("@", StringComparison.Ordinal) && input.Length > 1)
    {
        string filePath = input.Substring(1);
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Unable to access file: {filePath}");
                continue;
            }
            input = File.ReadAllText(filePath);
        }
        catch (Exception)
        {
            Console.WriteLine($"Unable to access file: {filePath}");
            continue;
        }
    }

    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));

    chat.IsComplete = false;

    try
    {
        await foreach (ChatMessageContent response in chat.InvokeAsync())
        {
            Console.WriteLine();

            var color = response.AuthorName == WriterName ? Spectre.Console.Color.Blue : Spectre.Console.Color.Red;

            // Create a panel for each message
            var panel = new Spectre.Console.Panel(ConvertToSpectreMarkup(response.Content))
                .Header($"[bold]{response.AuthorName.ToUpperInvariant()}[/]")
                .HeaderAlignment(Justify.Left)
                .BorderColor(color)
                .Expand();

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception.Message);
        if (exception.InnerException != null)
        {
            Console.WriteLine(exception.InnerException.Message);
            if (exception.InnerException.Data.Count > 0)
            {
                Console.WriteLine(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
            }
        }
    }
} while (!isComplete);

static string ConvertToSpectreMarkup(string input)
{
    // Échapper les caractères de balisage pour éviter les conflits
    input = Markup.Escape(input);

    // Remplacer **texte** par [bold]texte[/]
    input = Regex.Replace(input, @"\*\*(.+?)\*\*", "[bold]$1[/]");

    // Remplacer *texte* par [italic]texte[/]
    input = Regex.Replace(input, @"\*(.+?)\*", "[italic]$1[/]");

    // Remplacer ### Titre par [underline bold]Titre[/]
    input = Regex.Replace(input, @"^###\s+(.+)$", "[underline bold]$1[/]", RegexOptions.Multiline);

    // Remplacer ## Titre par [bold]Titre[/]
    input = Regex.Replace(input, @"^##\s+(.+)$", "[bold]$1[/]", RegexOptions.Multiline);

    // Remplacer # Titre par [underline]Titre[/]
    input = Regex.Replace(input, @"^#\s+(.+)$", "[underline]$1[/]", RegexOptions.Multiline);

    return input;
}
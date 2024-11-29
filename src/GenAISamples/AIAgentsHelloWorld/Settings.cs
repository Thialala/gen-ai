using Microsoft.Extensions.Configuration;
using System.Reflection;


namespace AIAgentsHelloWorld;

public class Settings
{
    private readonly IConfigurationRoot _configRoot;

    private AzureOpenAISettings _azureOpenAI;
    private OpenAISettings _openAI;

    public AzureOpenAISettings AzureOpenAI => _azureOpenAI ??= GetSettings<AzureOpenAISettings>();
    public OpenAISettings OpenAI => _openAI ??= GetSettings<OpenAISettings>();

    public class OpenAISettings
    {
        public string ChatModel { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class AzureOpenAISettings
    {
        public string ChatModelDeployment { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public TSettings GetSettings<TSettings>() =>
        _configRoot.GetRequiredSection(typeof(TSettings).Name).Get<TSettings>()!;

    public Settings()
    {
        _configRoot =
            new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .Build();
    }
}

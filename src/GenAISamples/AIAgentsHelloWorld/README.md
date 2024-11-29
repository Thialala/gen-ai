# AIAgentsHelloWorld

## Description

AIAgentsHelloWorld is a project that uses Microsoft Semantic Kernel to create collaborative chat agents. These agents—a writer and a critic—work together to produce high-quality content. The application also uses Spectre.Console for an interactive command-line user interface.

## Prerequisites

- .NET 8.0
- Visual Studio 2022
- An Azure account with access to Azure OpenAI
- API key and endpoint for Azure OpenAI

## Configuration

1. Set up user secrets for Azure OpenAI:

   ```
   dotnet user-secrets set "AzureOpenAISettings:ChatModelDeployment" "<your_deployment_name>"
   dotnet user-secrets set "AzureOpenAISettings:Endpoint" "<your_endpoint>"
   dotnet user-secrets set "AzureOpenAISettings:ApiKey" "<your_api_key>"
   ```

## Usage

- Type your message and press Enter to interact with the agents.
- Use the `EXIT` command to quit the application.
- Use the `RESET` command to reset the conversation.
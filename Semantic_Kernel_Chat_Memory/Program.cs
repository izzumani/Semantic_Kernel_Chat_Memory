using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
#pragma warning disable SKEXP0003, SKEXP0011, SKEXP0052, SKEXP0001, SKEXP0050 , SKEXP0010
public class Program
{
    private static Kernel _kernel;
    private static SecretClient keyVaultClient;

    public async static Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
                     .AddUserSecrets<Program>()
                     .Build();

        string? appTenant = config["appTenant"];
        string? appId = config["appId"] ?? null;
        string? appPassword = config["appPassword"] ?? null;
        string? keyVaultName = config["KeyVault"] ?? null;

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        ClientSecretCredential credential = new ClientSecretCredential(appTenant, appId, appPassword);
        keyVaultClient = new SecretClient(keyVaultUri, credential);
        string? apiKey = keyVaultClient.GetSecret("OpenAIapiKey").Value.Value;
        string? orgId = keyVaultClient.GetSecret("OpenAIorgId").Value.Value;

        var _builder = Kernel.CreateBuilder()
           //.AddOpenAIChatCompletion("gpt-3.5-turbo", apiKey, orgId, serviceId: "gpt35")
            .AddOpenAIChatCompletion("gpt-4", apiKey, orgId, serviceId: "gpt4");

        _kernel = _builder.Build();
        _kernel.ImportPluginFromObject(new ConversationSummaryPlugin());
        /*
        const string prompt = @"
                                Chat history:
                                {{$history}}
                                User: {{$userInput}}
                                Assistant:";
        */
        const string prompt = @"
                                Chat history:
                                {{ConversationSummaryPlugin.SummarizeConversation $history}}
                                User: {{$userInput}}
                            ChatBot:";
        var executionSettings = new OpenAIPromptExecutionSettings { MaxTokens = 2000, Temperature = 0.8, };
        var chatFunction = _kernel.CreateFunctionFromPrompt(prompt, executionSettings);
        var history = "";
        var arguments = new KernelArguments();
        arguments["history"] = history;

        var chatting = true;
        while (chatting)
        {
            Console.Write("User: ");
            var input = Console.ReadLine();
            if (input == null) { break; }
            input = input.Trim();
            if (input == "exit") { break; }
            arguments["userInput"] = input;
            var answer = await chatFunction.InvokeAsync(_kernel, arguments);
            var result = $"\nUser: {input}\nAssistant: {answer}\n";
            history += result;
            arguments["history"] = history;
            // Show the bot response
            Console.WriteLine(result);
        }

        Console.ReadLine();

    }

}
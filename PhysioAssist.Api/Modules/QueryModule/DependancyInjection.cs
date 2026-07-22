using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient.Options;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;
using PhysioAssist.Api.Modules.QueryModule.Plugin;
using PhysioAssist.Api.Modules.QueryModule.Prompts;
using PhysioAssist.Api.Modules.QueryModule.Services;
using PhysioAssist.Api.Shared.Options;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Modules.QueryModule;

public static class DependancyInjection
{
    public static IServiceCollection AddQueryModuleConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TavilyOptions>(
            configuration.GetRequiredSection(TavilyOptions.SectionName));

        services.AddSingleton<IChatHistoryStore, SessionChatHistoryStore>();

        services.AddKeyedSingleton<IChatCompletionService>("summarizationAI", (sp, _) =>
        {
            var summarizationAI = sp.GetRequiredService<IOptions<GitHubModelsDocumentationOptions>>().Value;

            #pragma warning disable SKEXP0010
            return new OpenAIChatCompletionService(
                    modelId: summarizationAI.ChatModel,
                    apiKey: summarizationAI.Token,
                    endpoint: new Uri("https://models.inference.ai.azure.com"));
        });

        services.AddHttpClient(nameof(WebSearchPlugin), (sp, client) =>
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        });

        services.AddScoped<PatientLookupPlugin>();
        services.AddScoped<SessionSearchPlugin>();
        services.AddScoped<AnswerTranslationPlugin>();

        services.AddScoped<ChatCompletionAgent>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GitHubModelsChatOptions>>().Value;
            var patientPlugin = sp.GetRequiredService<PatientLookupPlugin>();
            var searchPlugin = sp.GetRequiredService<SessionSearchPlugin>();
            var tavilyOptions = sp.GetRequiredService<IOptions<TavilyOptions>>();
            var tavilyClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WebSearchPlugin));
            var summarizationService = sp.GetRequiredKeyedService<IChatCompletionService>("summarizationAI");
            var TranslationPlugin = sp.GetRequiredService<AnswerTranslationPlugin>();



            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: options.ChatModel,
                    apiKey: options.Token,
                    endpoint: new Uri(options.Endpoint))
                .Build();

            var webSearchPlugin = new WebSearchPlugin(tavilyClient, tavilyOptions);

            kernel.Plugins.AddFromObject(patientPlugin, "PatientLookup");
            kernel.Plugins.AddFromObject(searchPlugin, "SessionSearch");
            kernel.Plugins.AddFromObject(webSearchPlugin, "WebSearch");
            kernel.Plugins.AddFromObject(TranslationPlugin, "AnswerTranslation");

            #pragma warning disable SKEXP0110
            return new ChatCompletionAgent
            {
                Name = "QueryAgent",
                Instructions = QueryAgentPrompts.AgentInstructions,
                Kernel = kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
                //TODO: USE CHEAPER MODEL FOR SUMMARIZATION LIKE GPT4O-MINI or Something
                HistoryReducer = new ChatHistorySummarizationReducer(
                    service: summarizationService,
                    targetCount: 10,
                    thresholdCount: 15)
            };
        });

        return services;
    }
}

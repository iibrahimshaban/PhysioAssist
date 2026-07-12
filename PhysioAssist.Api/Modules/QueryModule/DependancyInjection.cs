using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;
using PhysioAssist.Api.Modules.QueryModule.Plugin;
using PhysioAssist.Api.Modules.QueryModule.Prompts;
using PhysioAssist.Api.Modules.QueryModule.Services;
using PhysioAssist.Api.Shared.Options;
using System.Net.Http.Headers;

namespace PhysioAssist.Api.Modules.QueryModule;

//public class ModelLoggingHandler : DelegatingHandler
//{
//    private readonly ILogger<ModelLoggingHandler> _logger;

//    public ModelLoggingHandler(ILogger<ModelLoggingHandler> logger)
//    {
//        _logger = logger;
//    }

//    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//    {
//        string? model = null;

//        if (request.Content is not null)
//        {
//            var body = await request.Content.ReadAsStringAsync(cancellationToken);
//            try
//            {
//                using var doc = JsonDocument.Parse(body);
//                if (doc.RootElement.TryGetProperty("model", out var modelProp))
//                    model = modelProp.GetString();
//            }
//            catch (JsonException)
//            {
//                // not a JSON body, ignore
//            }
//        }

//        _logger.LogInformation("→ Outgoing request to {Url} using model: {Model}", request.RequestUri, model ?? "unknown");

//        var response = await base.SendAsync(request, cancellationToken);

//        _logger.LogInformation("← Response status {Status} for model: {Model}", (int)response.StatusCode, model ?? "unknown");

//        return response;
//    }
//}

public static class DependancyInjection
{
    public static IServiceCollection AddQueryModuleConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TavilyOptions>(
            // reuse the same config section if Tavily is already configured app-wide,
            // otherwise bind a PhysioAssist-specific section
            configuration.GetRequiredSection(TavilyOptions.SectionName));


        //services.AddTransient<ModelLoggingHandler>();
        //services.AddHttpClient("ModelTrackedClient").AddHttpMessageHandler<ModelLoggingHandler>();


        services.AddSingleton<IChatHistoryStore, SessionChatHistoryStore>();

        services.AddKeyedSingleton<IChatCompletionService>("summarizationAI",(sp,_) =>
        {
            var summarizationAI = sp.GetRequiredService<IOptions<GitHubModelsDocumentationOptions>>().Value;
            //var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ModelTrackedClient");

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

        services.AddScoped<ChatCompletionAgent>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GitHubModelsChatOptions>>().Value;
            var patientPlugin = sp.GetRequiredService<PatientLookupPlugin>();
            var searchPlugin = sp.GetRequiredService<SessionSearchPlugin>();
            var tavilyOptions = sp.GetRequiredService<IOptions<TavilyOptions>>();
            var tavilyClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WebSearchPlugin));
            var summarizationService = sp.GetRequiredKeyedService<IChatCompletionService>("summarizationAI");

            //var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ModelTrackedClient");

            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: options.ChatModel,
                    apiKey: options.Token,
                    endpoint: new Uri("https://models.inference.ai.azure.com"))
                .Build();

            var webSearchPlugin = new WebSearchPlugin(tavilyClient, tavilyOptions);

            kernel.Plugins.AddFromObject(patientPlugin, "PatientLookup");
            kernel.Plugins.AddFromObject(searchPlugin, "SessionSearch");
            kernel.Plugins.AddFromObject(webSearchPlugin, "WebSearch");

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

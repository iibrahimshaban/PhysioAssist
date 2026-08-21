using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PhysioAssist.Api.Infrastructure.Documentation;
using PhysioAssist.Api.Modules.QueryModule.Interfaces;
using PhysioAssist.Api.Modules.QueryModule.Options;
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
        services
            .Configure<TavilyOptions>(
            configuration.GetRequiredSection(TavilyOptions.SectionName));

        services
        .AddOptions<QueryAgentChatOptions>()
        .BindConfiguration(QueryAgentChatOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services
        .AddOptions<SbgQueryAgentChatOptions>()
        .BindConfiguration(SbgQueryAgentChatOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services
        .AddOptions<GeminiQueryAgentChatOptions>()
        .BindConfiguration(GeminiQueryAgentChatOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddHttpClient<SbgChatCompletionService>();

        services.AddSingleton<IChatHistoryStore, SessionChatHistoryStore>();

        services.AddKeyedSingleton<IChatCompletionService>("summarizationAI", (sp, _) =>
        {
            var summarizationAI = sp.GetRequiredService<IOptions<DocumentationChatOptions>>().Value;

            #pragma warning disable SKEXP0010
            return new OpenAIChatCompletionService(
                    modelId: summarizationAI.ChatModel,
                    apiKey: summarizationAI.Token,
                    endpoint: new Uri(summarizationAI.Endpoint));
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
        services.AddScoped<PatientQueryPlugin>();

        services.AddScoped<ChatCompletionAgent>(sp =>
        {
            var patientPlugin = sp.GetRequiredService<PatientLookupPlugin>();
            var searchPlugin = sp.GetRequiredService<SessionSearchPlugin>();
            var tavilyOptions = sp.GetRequiredService<IOptions<TavilyOptions>>();
            var tavilyClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WebSearchPlugin));
            var summarizationService = sp.GetRequiredKeyedService<IChatCompletionService>("summarizationAI");
            var TranslationPlugin = sp.GetRequiredService<AnswerTranslationPlugin>();
            var patientQueryPlugin = sp.GetRequiredService<PatientQueryPlugin>();

            //var kernel = BuildSbgKernel(sp);
            //var kernel = BuildNvidiaKernel(sp);
            var kernel = BuildGeminiKernel(sp);

            var webSearchPlugin = new WebSearchPlugin(tavilyClient, tavilyOptions);

            kernel.Plugins.AddFromObject(patientPlugin, "PatientLookup");
            kernel.Plugins.AddFromObject(searchPlugin, "SessionSearch");
            kernel.Plugins.AddFromObject(webSearchPlugin, "WebSearch");
            kernel.Plugins.AddFromObject(TranslationPlugin, "AnswerTranslation");
            kernel.Plugins.AddFromObject(patientQueryPlugin, "PatientQuery");

#pragma warning disable SKEXP0110, SKEXP0070
            return new ChatCompletionAgent
            {
                Name = "QueryAgent",
                Instructions = QueryAgentPrompts.AgentInstructions,
                Kernel = kernel,
                Arguments = new KernelArguments(new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.EnableKernelFunctions
                }),
            };
#pragma warning restore SKEXP0110, SKEXP0070
        });

        return services;
    }

    private static Kernel BuildNvidiaKernel(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<QueryAgentChatOptions>>().Value;

        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: options.ChatModel,
                apiKey: options.Token,
                endpoint: new Uri(options.Endpoint))
            .Build();
    }

    private static Kernel BuildSbgKernel(IServiceProvider sp)
    {
        var sbgService = sp.GetRequiredService<SbgChatCompletionService>();

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(sbgService);
        return builder.Build();
    }
    private static Kernel BuildGeminiKernel(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<GeminiQueryAgentChatOptions>>().Value;

        #pragma warning disable SKEXP0070 // Google connector is experimental
                return Kernel.CreateBuilder()
                    .AddGoogleAIGeminiChatCompletion(
                        modelId: options.ChatModel,
                        apiKey: options.Token,
                        apiVersion: GoogleAIVersion.V1_Beta) // v1beta is required for thought-signature/thinking support
                    .Build();
        #pragma warning restore SKEXP0070
    }
}
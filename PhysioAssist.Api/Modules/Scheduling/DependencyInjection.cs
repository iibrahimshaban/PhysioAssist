using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PhysioAssist.Api.Infrastructure.GitHubModelsClient;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Plugins;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Modules.Scheduling.Services.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Options;
using PhysioAssist.Api.Shared.QR;

namespace PhysioAssist.Api.Modules.Scheduling;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>()
            .AddScoped<IAppointmentContactResolver, AppointmentContactResolver>()
            .AddScoped<IWorkingScheduleRepository, WorkingScheduleRepository>()
            .AddScoped<IWorkingScheduleDayRepository, WorkingScheduleDayRepository>()
            .AddScoped<IAppointmentValidator, AppointmentValidator>()
            .AddScoped<IAppointmentService, AppointmentService>()
            .AddScoped<IWorkingScheduleService, WorkingScheduleService>()
            .AddScoped<IScheduleSlotQueryService, ScheduleSlotQueryService>()
            .AddScoped<IPatientSessionSchedulingService, PatientSessionSchedulingService>();

        

        services
            .AddSchedulingAgentConfig(configuration);

        return services;
    }

    private static IServiceCollection AddSchedulingAgentConfig(this IServiceCollection services, IConfiguration configuration)
    {
        // Pure, deterministic candidate-building logic — no LLM involved.
        services.AddScoped<IDoctorScheduleRecommendationService, DoctorScheduleRecommendationService>();

        // Thin SK wrapper exposing the recommendation service as a KernelFunction.
        services.AddScoped<DoctorSchedulePlugin>();

        services
            .AddOptions<GroqSchedulingAgentOptions>()
            .BindConfiguration(GroqSchedulingAgentOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddKeyedScoped<ChatCompletionAgent>("DaySchedulerAgent", (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<GroqSchedulingAgentOptions>>().Value;
            var schedulePlugin = sp.GetRequiredService<DoctorSchedulePlugin>();

            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: options.ChatModel,
                    apiKey: options.Token,
                    endpoint: new Uri(options.Endpoint))
                .Build();

            kernel.Plugins.AddFromObject(schedulePlugin, "DoctorSchedule");

#pragma warning disable SKEXP0110
            return new ChatCompletionAgent
            {
                Name = "DaySchedulerAgent",
                Instructions = SchedulingAgentPrompts.AgentInstructions,
                Kernel = kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        });

        return services;
    }
}

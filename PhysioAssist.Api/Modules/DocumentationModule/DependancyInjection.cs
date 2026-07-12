using PhysioAssist.Api.Modules.DocumentationModule.Services;

namespace PhysioAssist.Api.Modules.DocumentationModule;

public static class DependancyInjection
{
    public static IServiceCollection AddDocumentationModule(this IServiceCollection services)
    {
        
        services.AddScoped<IDocumentationTemplateResolver, DocumentationTemplateResolver>();
        services.AddScoped<ISessionProgressNoteExtractionService, SessionProgressNoteExtractionService>();
        services.AddScoped<ISessionProgressNoteService, SessionProgressNoteService>();
        services.AddScoped<ISessionSummaryGenerationService, SessionSummaryGenerationService>();
        services.AddScoped<IDocumentationSummaryPdfService, DocumentationSummaryPdfService>();


        return services;
    }
}

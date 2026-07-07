using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Modules.InitialReportModule.Services;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.InitialReportModule;

public static class DependencyInjection
{
    public static IServiceCollection AddInitialReportModule(this IServiceCollection services)
    {
        services.AddScoped<IInitialReportRepository, InitialReportRepository>();
        services.AddScoped<IReportAttachmentRepository, ReportAttachmentRepository>();

        services.AddScoped<IInitialReportService, InitialReportService>();
        services.AddScoped<IInitialReportQueryService, InitialReportQueryService>();

        return services;
    }
}

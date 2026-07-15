using PhysioAssist.Api.Modules.Intake.Repositories;
using PhysioAssist.Api.Modules.Intake.Services;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.Intake;

public static class DependencyInjection
{
    public static IServiceCollection AddIntakeModule(this IServiceCollection services)
    {
        services.AddScoped<IPatientFormSchemaRepository, PatientFormSchemaRepository>();
        services.AddScoped<IPreVisitIntakeRepository, PreVisitIntakeRepository>();
        services.AddScoped<IDynamicFormValidationService, DynamicFormValidationService>();
        services.AddScoped<IIntakeService, IntakeService>();
        services.AddScoped<IPatientFormSchemaSeedingService, PatientFormSchemaSeedingService>();
        services.AddScoped<IIntakeQueryService, IntakeQueryService>();


        return services;
    }
}

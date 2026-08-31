using PhysioAssist.Api.Modules.PackageModule.Services;

namespace PhysioAssist.Api.Modules.PackageModule;

public static class PackageModuleRegistration
{
    public static IServiceCollection AddPackageModule(this IServiceCollection services)
    {
        services.AddScoped<IPatientSessionPackageService, PatientSessionPackageService>();
        services.AddScoped<ITreatmentSchedulePlanService, TreatmentSchedulePlanService>();
        services.AddScoped<ITreatmentSchedulePlanRepository, TreatmentSchedulePlanRepository>();

        return services;
    }
}

using PhysioAssist.Api.Modules.Intake.QueryServices;
using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Modules.PatientModule.Services;

namespace PhysioAssist.Api.Modules.PatientModule
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPatientModule(this IServiceCollection services)
        {
            services.AddScoped<IPatientRepo, PatientRepo>();
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IDoctorPatientRepo, DoctorPatientRepo>();
            services.AddScoped<IPatientOverviewIntakeQueryService, PatientOverviewIntakeQueryService>();
            services.AddScoped<IPatientOverviewIntakeCommandService, PatientOverviewIntakeCommandService>();
            services.AddScoped<IPatientQueryService, PatientQueryService>();
            services.AddHttpContextAccessor();
          
            return services;
        }
    }
}

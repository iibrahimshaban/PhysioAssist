using PhysioAssist.Api.Modules.PatientModule.Repositories;
using PhysioAssist.Api.Modules.PatientModule.Services;
using PhysioAssist.Api.Shared.Repositories;

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

            return services;
        }
    }
}

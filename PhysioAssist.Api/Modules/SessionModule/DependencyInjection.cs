using PhysioAssist.Api.Modules.SessionModule.Services;

namespace PhysioAssist.Api.Modules.SessionModule
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSessionModule(this IServiceCollection services)
        {
            services.AddScoped<ISessionService, SessionService>();

            return services;
        }
    }
}

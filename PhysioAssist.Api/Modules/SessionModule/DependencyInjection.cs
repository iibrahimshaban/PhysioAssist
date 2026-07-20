using PhysioAssist.Api.Modules.SessionModule.Services;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using PhysioAssist.Api.Shared.Interfaces.Exposed;

namespace PhysioAssist.Api.Modules.SessionModule
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSessionModule(this IServiceCollection services)
        {
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<ISessionQueryService, SessionQueryService>();
            services.AddScoped<ISessionSummaryWriter, SessionSummaryWriter>();

            return services;
        }
    }
}

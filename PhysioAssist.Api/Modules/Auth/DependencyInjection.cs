using PhysioAssist.Api.Modules.Auth.JwtService;
using PhysioAssist.Api.Modules.Auth.Services;

namespace PhysioAssist.Api.Modules.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services,IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

}

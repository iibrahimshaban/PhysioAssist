using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.Auth.JwtService;
using PhysioAssist.Api.Modules.Auth.Services;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Interfaces.Exposed;
using System.Text;

namespace PhysioAssist.Api.Modules.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthOptions(configuration)
            .AddIdentityConfig()
            .AddJwtBearerConfig(configuration);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IAuthQueryService, AuthQueryService>();

        return services;
    }

    private static IServiceCollection AddAuthOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddIdentityConfig(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
        });

        return services;
    }

    private static IServiceCollection AddJwtBearerConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience
            };
        });

        return services;
    }
}
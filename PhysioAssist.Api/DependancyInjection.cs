using CloudinaryDotNet;
using Hangfire;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using PhysioAssist.Api.Infrastructure.CloudinaryClient;
using PhysioAssist.Api.Modules.Auth;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Configuration;
using PhysioAssist.Api.Shared.Email;
using PhysioAssist.Api.Shared.Interfaces;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Reflection;
using PhysioAssist.Api.Modules.SessionModule;

namespace PhysioAssist.Api;

public static class DependancyInjection
{
    public static IServiceCollection AddGlobalServicesRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSwaggerGen()
            .AddHttpContextAccessor()
            .AddFluentValidationConfig()
            .AddMapsterConfiguration()
            .AddDbContextConfiguration(configuration)
            .AddCorsConfiguration(configuration)
            .AddCloudinaryImageHosting(configuration)
            .AddPermissionAuthorization()
            .AddMailConfig(configuration)
            .AddHangfireBGJobs(configuration);

        services.AddAuthModule(configuration);
        services.AddSessionModule();
        return services;
    }

    private static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
            options.AddPolicy("AllowAngular", build =>
                build
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
            )
        );

        return services;
    }

    private static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection AddDbContextConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Default connection string is not found");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    private static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton<IMapper>(new Mapper(mappingConfig));

        return services;
    }

    private static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }

    private static IServiceCollection AddMailConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MailSettings>()
            .BindConfiguration(nameof(MailSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<ICustomEmailService, EmailService>();

        return services;
    }

    private static IServiceCollection AddCloudinaryImageHosting(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudinarySettings = configuration.GetSection("Cloudinary");

        var account = new Account(
            cloudinarySettings["CloudName"],
            cloudinarySettings["ApiKey"],
            cloudinarySettings["ApiSecret"]
        );

        services.AddSingleton(new Cloudinary(account));
        services.AddScoped<IMediaStorageService, CloudinaryService>();

        return services;
    }
    public static IServiceCollection AddHangfireBGJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer();

        return services;
    }
}
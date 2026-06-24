namespace PhysioAssist.Api;

public static class DependancyInjection
{
    public static IServiceCollection AddServicesRegistration(this IServiceCollection services, IConfiguration configuration)
    {

        services
            .AddSwaggerGen()
            .AddCorsConfiguration(configuration);

        return services;

    }

    private static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
            options.AddPolicy("AllowAngular", build =>  // ← named instead of default
                build
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
            )
        );

        return services;
    }
}

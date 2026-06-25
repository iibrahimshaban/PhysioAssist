using CloudinaryDotNet;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Infrastructure;
using PhysioAssist.Api.Interfaces;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Repositories;
using PhysioAssist.Api.Services.Authentication;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Reflection;

namespace PhysioAssist.Api;

public static class DependancyInjection
{
    public static IServiceCollection AddServicesRegistration(this IServiceCollection services, IConfiguration configuration)
    {

        services
            .AddSwaggerGen()
            .AddFluentValidationConfig()
            .AddMapsterConfiguration()
            .AddServicesConfiguration()
            .AddCorsConfiguration(configuration)
            .AddDbContextConfiguration(configuration)
            .AddCloudinaryImageHosting(configuration);

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
    private static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation()
           .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
    private static IServiceCollection AddDbContextConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Default Connection is not found");

        services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 1;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
        });

        services.AddIdentity<ApplicationUser, IdentityRole>()
             .AddEntityFrameworkStores<ApplicationDbContext>()
             .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        return services;
    }
    private static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var MappingConfig = TypeAdapterConfig.GlobalSettings;
        MappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton<IMapper>(new Mapper(MappingConfig));
        return services;
    }
    private static IServiceCollection AddServicesConfiguration(this IServiceCollection services)
    {
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
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

        services.AddScoped<ICloudinaryService, CloudinaryService>();
        return services;
    }
}

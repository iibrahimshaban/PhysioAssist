using CloudinaryDotNet;
using Hangfire;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using PhysioAssist.Api.Infrastructure.AutoComplete;
using PhysioAssist.Api.Infrastructure.CloudinaryClient;
using PhysioAssist.Api.Infrastructure.GeminiClient;
using PhysioAssist.Api.Infrastructure.GroqClient;
using PhysioAssist.Api.Modules.Auth;
<<<<<<< HEAD
using PhysioAssist.Api.Modules.InitialReportModule;
using PhysioAssist.Api.Modules.SessionModule;
=======
using PhysioAssist.Api.Modules.Auth.Services;
using PhysioAssist.Api.Modules.DocumentationModule;
using PhysioAssist.Api.Modules.InitialReportModule;
using PhysioAssist.Api.Modules.Intake;
using PhysioAssist.Api.Modules.PatientModule;
using PhysioAssist.Api.Modules.PatientModule.Services;
using PhysioAssist.Api.Modules.QueryModule;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;
using PhysioAssist.Api.Modules.Scheduling.Services.Implementations;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Modules.SessionModule;
using PhysioAssist.Api.Modules.SessionModule.Services;
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Email;
<<<<<<< HEAD
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.NotificationService;
using PhysioAssist.Api.Shared.PdfService;
using PhysioAssist.Api.Shared.QRService;
=======
using PhysioAssist.Api.Shared.Interfaces.Common;
using PhysioAssist.Api.Shared.Interfaces.Documentation;
using PhysioAssist.Api.Shared.Interfaces.Exposed;
using PhysioAssist.Api.Shared.Interfaces.Ingestion;
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
using PhysioAssist.Api.Shared.Repositories;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Reflection;

namespace PhysioAssist.Api;

public static class DependancyInjection
{
    public static IServiceCollection AddGlobalServicesRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        services
            .AddSwaggerConfiguration()
            .AddHttpContextAccessor()
            .AddFluentValidationConfig()
            .AddMapsterConfiguration()
            .AddPermissionAuthorization()
            .AddMailConfig()
<<<<<<< HEAD
=======
            .AddExposedServicesConfig()
            .AddDocumentationSummarizationConfig()
            .AddAutoCompleteService(configuration)
            .AddEmbeddingConfig()
            .AddAudioTranscriptionConfig()
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
            .AddDbContextConfiguration(configuration)
            .AddCorsConfiguration(configuration)
            .AddCloudinaryImageHosting(configuration)
            .AddAudioTranscriptionConfig()
            .AddHangfireBGJobs(configuration);

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddQrCodeConfig(configuration);
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<INotificationService, PhysioAssist.Api.Shared.NotificationService.NotificationService>();

        services.AddAuthModule(configuration);
        services.AddSessionModule();
        services.AddInitialReportModule();
        return services;
    }

    private static IServiceCollection AddQrCodeConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services
<<<<<<< HEAD
            .AddOptions<QrSettings>()
            .BindConfiguration(QrSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IQRService, QRService>();
=======
           .AddAuthModule(configuration)
           .AddIntakeModule()
           .AddSessionModule()
           .AddQueryModuleConfig(configuration)
           .AddPatientModule()
           .AddDocumentationModule()
           .AddSharedServices(configuration)
           .AddInitialReportModule();
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99

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
    private static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            const string schemeId = "Bearer";

            options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token below (no need to type \"Bearer \" — Swagger adds it automatically)."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(schemeId, document),
                    new List<string>()
                }
            });
        });


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

    private static IServiceCollection AddMailConfig(this IServiceCollection services)
    {
<<<<<<< HEAD
=======
       
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();
        services.AddScoped<IWorkingScheduleRepository, WorkingScheduleRepository>();
        services.AddScoped<IWorkingScheduleDayRepository, WorkingScheduleDayRepository>();
        services.AddScoped<IAppointmentValidator, AppointmentValidator>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IWorkingScheduleService, WorkingScheduleService>();
        services.AddScoped<IScheduleSlotQueryService, ScheduleSlotQueryService>();
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
        services
            .AddOptions<MailSettings>()
            .BindConfiguration(MailSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<ICustomEmailService, EmailService>();

        return services;
    }
<<<<<<< HEAD
=======
    private static IServiceCollection AddDocumentationSummarizationConfig(this IServiceCollection services)
    {

        services.AddOptions<GitHubModelsDocumentationOptions>()
             .BindConfiguration(GitHubModelsDocumentationOptions.SectionName)
             .ValidateDataAnnotations()
             .ValidateOnStart();

        services.AddHttpClient<IDocumentationExtractionService, GitHubModelsDocumentationExtractionService>();

        services.AddOptions<GroqSummarizationOptions>()
            .BindConfiguration(GroqSummarizationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ISessionSummarizationService, GroqSessionSummarizationService>();

        services.AddOptions<GroqRollupSummarizationOptions>()
            .BindConfiguration(GroqRollupSummarizationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IRollupSummarizationService, GroqRollupSummarizationService>();

        return services;
    }
    private static IServiceCollection AddEmbeddingConfig(this IServiceCollection services)
    {
        services.AddOptions<GitHubModelsEmbeddingOptions>()
            .BindConfiguration(GitHubModelsEmbeddingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IEmbeddingService, GitHubModelsEmbeddingService>();
        services.AddScoped<ISessionEmbeddingService, SessionEmbeddingService>();

        services.AddOptions<GitHubModelsChatOptions>()
        .BindConfiguration(GitHubModelsChatOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddHttpClient<ITranscriptChunkingService, GitHubModelsChunkingService>();
        services.AddHttpClient<IQueryTranslationService, GitHubModelsQueryTranslationService>();
        services.AddScoped<ISessionChunkSearchService, SessionChunkSearchService>();



        return services;
    }
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
    private static IServiceCollection AddAudioTranscriptionConfig(this IServiceCollection services)
    {
        services
            .AddOptions<GroqOptions>()
            .BindConfiguration(GroqOptions.SectionName)
            .ValidateDataAnnotations();

        services
        .AddOptions<GeminiOptions>()
        .BindConfiguration(GeminiOptions.SectionName)
        .ValidateDataAnnotations();

        services.AddHttpClient<GroqWhisperClient>();
        services.AddHttpClient<ITranscriptionRefinementService,GroqRefinementClient>();

        services.AddHttpClient<IAudioTranscriptionService, GeminiTranscriptionClient>();

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
<<<<<<< HEAD
=======


    // Autocomplete services
    public static IServiceCollection AddAutoCompleteService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VocabularySources>(configuration.GetSection("VocabularySources"));

        services.AddSingleton<MultiLanguageTrieRegistry>();
        services.AddSingleton<MultiLanguageVocabularyLoader>();

        services.AddHostedService<VocabularyBootstrapService>();

        services.AddSingleton<IAutoCompleteService, AutoCompleteService>();

        services.AddResponseCaching();

        return services;
    }
    private static IServiceCollection AddExposedServicesConfig(this IServiceCollection services)
    {
        services.AddScoped<IPatientQueryService, PatientQueryService>();
        return services;
    }
>>>>>>> be94d86bf95f3c039134e9161e18565aa145bc99
}

using PhysioAssist.Api.Shared.Interfaces.Common;
using PhysioAssist.Api.Shared.QR;
using PhysioAssist.Api.Shared.Repositories;
using IQRService = PhysioAssist.Api.Shared.Interfaces.Common.IQRService;

namespace PhysioAssist.Api.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services
            .AddOptions<QRTokenOptions>()
            .BindConfiguration(QRTokenOptions.SectionName);

        services.AddScoped<IQRService, PhysioAssist.Api.Shared.QR.QRService>();

        return services;
    }
}

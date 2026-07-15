using PhysioAssist.Api.Modules.Notification.Interfaces;
using PhysioAssist.Api.Modules.Notification.Services;

namespace PhysioAssist.Api.Modules.Notification
{
    public static class NotificationModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationModule(this IServiceCollection services)
        {
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<INotificationChannel, EmailNotificationChannel>();
            services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}

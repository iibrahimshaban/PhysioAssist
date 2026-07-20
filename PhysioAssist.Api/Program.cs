using Hangfire;
using HangfireBasicAuthenticationFilter;
using PhysioAssist.Api;
using PhysioAssist.Api.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddGlobalServicesRegistration(builder.Configuration);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}


app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization =
    [
        new HangfireCustomBasicAuthenticationFilter
        {
            User = app.Configuration.GetValue<string>("HangfireSettings:Username"),
            Pass = app.Configuration.GetValue<string>("HangfireSettings:password")
        }
    ],
    DashboardTitle = "PhysioAssist dashboard"
});

app.MapControllers();

await DataSeeder.SeedAsync(app.Services);
await TestDataSeeder.SeedAsync(app.Services);

app.Run();

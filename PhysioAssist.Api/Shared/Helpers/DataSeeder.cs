using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Modules.DocumentationModule.Seed;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Consts;
using System.Security.Claims;

namespace PhysioAssist.Api.Shared.Helpers;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedAdminPermissionsAsync(roleManager);
        await SeedSoloDoctorPermissionsAsync(roleManager);
        // seed receptionist permissions 

        await DocumentationTemplateSeeder.SeedAsync(context);
        
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        IdentityRole[] roles =
        [
            new IdentityRole { Id = DefaultRoles.AdminRoleId, Name = DefaultRoles.Admin },
            new IdentityRole { Id = DefaultRoles.SoloRoleId,  Name = DefaultRoles.SoloDoctor },
            new IdentityRole { Id = DefaultRoles.ReceptionistRoleId, Name = DefaultRoles.Receptionist }
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
                await roleManager.CreateAsync(role);
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByIdAsync(DefaultUsers.UserId) is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = DefaultUsers.UserId,
            Email = DefaultUsers.Email,
            UserName = DefaultUsers.UserName,
            FirstName = DefaultUsers.FirstName,
            LastName = DefaultUsers.LastName,
            ProfilePictureUrl = DefaultUsers.ProfilePhoto,
            IsDisabled = false,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(admin, DefaultUsers.Password);
        await userManager.AddToRoleAsync(admin, DefaultRoles.Admin);
    }
    private static async Task SeedAdminPermissionsAsync(RoleManager<IdentityRole> roleManager)
    {
        var adminRole = await roleManager.FindByNameAsync(DefaultRoles.Admin);

        if (adminRole is null) return;

        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var allPermissions = Permissions.GetAllPermissions();

        foreach (var permission in allPermissions)
        {
            if (string.IsNullOrEmpty(permission)) continue;

            var alreadyExists = existingClaims.Any(c =>
                c.Type == Permissions.Type && c.Value == permission);

            if (!alreadyExists)
                await roleManager.AddClaimAsync(adminRole, new Claim(Permissions.Type, permission));
        }
    }
    private static async Task SeedSoloDoctorPermissionsAsync(RoleManager<IdentityRole> roleManager)
    {
        var soloDoctorRole = await roleManager.FindByNameAsync(DefaultRoles.SoloDoctor);

        if (soloDoctorRole is null) return;

        var existingClaims = await roleManager.GetClaimsAsync(soloDoctorRole);

        var soloDoctorPermissions = new[]
        {
            Permissions.IntakeRead,
            Permissions.IntakeManageForms,
            Permissions.IntakeReview,
            Permissions.IntakeConvert,
            Permissions.QRGenerate,
            Permissions.QRValidate,
            Permissions.GetReceptionist,
            Permissions.CreateReceptionist,
            Permissions.UpdateReceptionist,
            Permissions.CheckInPatients,
            Permissions.ManageSchedule
        };

        foreach (var permission in soloDoctorPermissions)
        {
            if (string.IsNullOrEmpty(permission)) continue;

            var alreadyExists = existingClaims.Any(c =>
                c.Type == Permissions.Type && c.Value == permission);

            if (!alreadyExists)
                await roleManager.AddClaimAsync(soloDoctorRole, new Claim(Permissions.Type, permission));
        }
    }
}

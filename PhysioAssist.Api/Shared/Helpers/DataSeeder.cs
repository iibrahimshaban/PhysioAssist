using Microsoft.AspNetCore.Identity;
using PhysioAssist.Api.Modules.Auth.Entities;
using PhysioAssist.Api.Shared.Consts;

namespace PhysioAssist.Api.Shared.Helpers;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        IdentityRole[] roles =
        [
            new IdentityRole { Id = DefaultRoles.AdminRoleId, Name = DefaultRoles.Admin },
            new IdentityRole { Id = DefaultRoles.SoloRoleId,  Name = DefaultRoles.SoloDoctor }
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
}

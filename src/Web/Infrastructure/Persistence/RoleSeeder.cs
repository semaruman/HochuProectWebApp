using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.Persistence;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await roleManager.RoleExistsAsync(AdminRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = AdminRoles.Admin,
                NormalizedName = AdminRoles.Admin.ToUpperInvariant()
            });
        }

        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TermsAcceptedAt = DateTime.UtcNow,
                PrivacyPolicyAcceptedAt = DateTime.UtcNow
            };
            var create = await userManager.CreateAsync(admin, adminPassword);
            if (!create.Succeeded)
                return;

            if (!await db.Profiles.AnyAsync(p => p.UserId == admin.Id))
                db.Profiles.Add(Profile.Create(admin.Id, "Admin", DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRoles.Admin))
            await userManager.AddToRoleAsync(admin, AdminRoles.Admin);
    }
}

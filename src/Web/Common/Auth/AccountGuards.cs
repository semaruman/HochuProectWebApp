using Microsoft.AspNetCore.Identity;
using Web.Common.Errors;
using Web.Domain.Entities;

namespace Web.Common.Auth;

public static class AccountGuards
{
    public static async Task<ApplicationUser> RequireActiveUserAsync(
        UserManager<ApplicationUser> userManager,
        Guid userId,
        bool requireConfirmedEmail = true,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw AppErrors.Unauthorized();
        if (user.IsBlocked)
            throw AppErrors.Forbidden("Account is blocked.");
        if (requireConfirmedEmail && !user.EmailConfirmed)
            throw AppErrors.Forbidden("Please confirm your email address.");
        return user;
    }
}

public static class AdminRoles
{
    public const string Admin = "Admin";
}

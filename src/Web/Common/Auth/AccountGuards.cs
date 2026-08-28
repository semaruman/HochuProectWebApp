using Microsoft.AspNetCore.Identity;
using Web.Common.Results;
using Web.Domain.Entities;

namespace Web.Common.Auth;

public static class AccountGuards
{
    public static async Task<Result<ApplicationUser>> RequireActiveUserAsync(
        UserManager<ApplicationUser> userManager,
        Guid userId,
        bool requireConfirmedEmail = true,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ResultErrors.Unauthorized();
        if (user.IsBlocked)
            return ResultErrors.Forbidden("Account is blocked.");
        if (requireConfirmedEmail && !user.EmailConfirmed)
            return ResultErrors.Forbidden("Please confirm your email address.");
        return user;
    }
}

public static class AdminRoles
{
    public const string Admin = "Admin";
}

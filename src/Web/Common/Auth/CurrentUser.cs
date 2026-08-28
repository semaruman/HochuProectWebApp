using System.Security.Claims;
using Web.Common.Results;

namespace Web.Common.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? TryGetUserId();
}

public static class CurrentUserExtensions
{
    public static Result<Guid> GetUserId(this ICurrentUser currentUser)
        => currentUser.TryGetUserId() is Guid id ? id : ResultErrors.Unauthorized();
}

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? TryGetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
            return null;

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

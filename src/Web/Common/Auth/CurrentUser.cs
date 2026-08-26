using System.Security.Claims;

namespace Web.Common.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid? TryGetUserId();
}

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId => TryGetUserId() ?? throw new UnauthorizedAccessException();

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

using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Infrastructure.Persistence;

namespace Web.Features.Notifications;

public class NotificationsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var items = await db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Body,
                    n.LinkUrl,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        group.MapPost("/{id:guid}/read", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var item = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
            if (item is null)
                return ResultErrors.NotFound().ToProblemResult();

            item.IsRead = true;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { item.Id, item.IsRead });
        });

        group.MapPost("/read-all", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
            return Results.Ok(new { message = "All read" });
        });
    }
}

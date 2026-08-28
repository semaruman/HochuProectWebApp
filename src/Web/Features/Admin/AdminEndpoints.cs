using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Admin;

public class AdminEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(AdminRoles.Admin);

        group.MapGet("/users", async (AppDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users.AsNoTracking()
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.EmailConfirmed,
                    u.IsBlocked,
                    u.TermsAcceptedAt,
                    DisplayName = db.Profiles.Where(p => p.UserId == u.Id).Select(p => p.DisplayName).FirstOrDefault()
                })
                .Take(200)
                .ToListAsync(ct);
            return Results.Ok(users);
        });

        group.MapPost("/users/{userId:guid}/block", async (
            Guid userId,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResultErrors.NotFound().ToProblemResult();

            user.IsBlocked = true;
            await userManager.UpdateAsync(user);
            return Results.Ok(new { user.Id, user.IsBlocked });
        });

        group.MapPost("/users/{userId:guid}/unblock", async (
            Guid userId,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResultErrors.NotFound().ToProblemResult();

            user.IsBlocked = false;
            await userManager.UpdateAsync(user);
            return Results.Ok(new { user.Id, user.IsBlocked });
        });

        group.MapGet("/projects", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Projects.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Status,
                    p.BuyerId,
                    p.CreatedAt
                })
                .Take(200)
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        group.MapPost("/projects/{id:guid}/hide", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();

            var hideResult = project.Hide(DateTime.UtcNow);
            if (hideResult.IsFailure)
                return hideResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { project.Id, project.Status });
        });

        group.MapPost("/projects/{id:guid}/restore", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();

            var restoreResult = project.RestorePublication(DateTime.UtcNow);
            if (restoreResult.IsFailure)
                return restoreResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { project.Id, project.Status });
        });

        group.MapGet("/deals/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var deal = await db.Deals.AsNoTracking()
                .Include(d => d.Deliverables).ThenInclude(x => x.Files)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (deal is null)
                return ResultErrors.NotFound().ToProblemResult();

            var projectTitle = await db.Projects.Where(p => p.Id == deal.ProjectId).Select(p => p.Title).FirstAsync(ct);
            var buyerEmail = await db.Users.Where(u => u.Id == deal.BuyerId).Select(u => u.Email).FirstAsync(ct);
            var sellerEmail = await db.Users.Where(u => u.Id == deal.SellerId).Select(u => u.Email).FirstAsync(ct);

            return Results.Ok(new
            {
                deal.Id,
                deal.ProjectId,
                projectTitle,
                deal.BuyerId,
                buyerEmail,
                deal.SellerId,
                sellerEmail,
                deal.Amount,
                deal.Status,
                deal.CreatedAt,
                deal.FundedAt,
                deal.SubmittedAt,
                deal.CompletedAt,
                deal.RevisionRequestedAt,
                deal.LastRevisionComment,
                Deliverables = deal.Deliverables.Select(d => new
                {
                    d.Id,
                    d.Message,
                    d.CreatedAt,
                    Files = d.Files.Select(f => new { f.Id, f.FileName, f.SizeBytes })
                })
            });
        });
    }
}

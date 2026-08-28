using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Infrastructure.Files;
using Web.Infrastructure.Persistence;

namespace Web.Features.Profiles;

public record UpdateProfileRequest(string DisplayName, string? Bio);
public record UpdateSkillsRequest(IReadOnlyList<string> Skills);
public record CreatePortfolioRequest(string Title, string? Description, string? Url);

public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Bio).MaximumLength(2000);
    }
}

public class ProfilesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles").WithTags("Profiles");

        group.MapGet("/me", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var profile = await db.Profiles
                .Include(p => p.User)
                .Include(p => p.UserSkills).ThenInclude(us => us.Skill)
                .Include(p => p.PortfolioItems)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null)
                return ResultErrors.NotFound("Profile not found.").ToProblemResult();

            return Results.Ok(MapProfile(profile, includePrivate: true));
        }).RequireAuthorization();

        group.MapPut("/me", async (
            UpdateProfileRequest request,
            IValidator<UpdateProfileRequest> validator,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
                return validation.ToHttpResult(() => Results.Ok());

            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null)
                return ResultErrors.NotFound("Profile not found.").ToProblemResult();

            var updateResult = profile.Update(request.DisplayName, request.Bio, DateTime.UtcNow);
            if (updateResult.IsFailure)
                return updateResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { profile.UserId, profile.DisplayName, profile.Bio });
        }).RequireAuthorization();

        group.MapPut("/me/skills", async (
            UpdateSkillsRequest request,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            if (await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct) is null)
                return ResultErrors.NotFound("Profile not found.").ToProblemResult();

            var names = request.Skills
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();

            var existing = await db.Skills.Where(s => names.Contains(s.Name)).ToListAsync(ct);
            foreach (var name in names.Where(n => existing.All(e => !string.Equals(e.Name, n, StringComparison.OrdinalIgnoreCase))))
            {
                var skill = new Skill { Id = Guid.NewGuid(), Name = name };
                db.Skills.Add(skill);
                existing.Add(skill);
            }

            var current = await db.UserSkills.Where(us => us.UserId == userId).ToListAsync(ct);
            db.UserSkills.RemoveRange(current);
            foreach (var skill in existing.Where(e => names.Any(n => string.Equals(n, e.Name, StringComparison.OrdinalIgnoreCase))))
            {
                db.UserSkills.Add(new UserSkill { UserId = userId, SkillId = skill.Id });
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { skills = names });
        }).RequireAuthorization();

        group.MapPost("/me/portfolio", async (
            CreatePortfolioRequest request,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return ResultErrors.BadRequest("Title is required.").ToProblemResult();

            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var count = await db.PortfolioItems.CountAsync(p => p.UserId == userId, ct);
            if (count >= 10)
                return ResultErrors.Business("Portfolio limit reached (10).").ToProblemResult();

            var item = new PortfolioItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title.Trim(),
                Description = request.Description,
                Url = request.Url,
                CreatedAt = DateTime.UtcNow
            };
            db.PortfolioItems.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/profiles/{userId}", item);
        }).RequireAuthorization();

        group.MapDelete("/me/portfolio/{id:guid}", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var item = await db.PortfolioItems.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
            if (item is null)
                return ResultErrors.NotFound().ToProblemResult();

            db.PortfolioItems.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/me/avatar", async (
            HttpRequest request,
            ICurrentUser currentUser,
            AppDbContext db,
            IFileStorage files,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null)
                return ResultErrors.NotFound().ToProblemResult();

            if (!request.HasFormContentType)
                return ResultErrors.BadRequest("Multipart form expected.").ToProblemResult();

            var file = request.Form.Files.FirstOrDefault();
            if (file is null)
                return ResultErrors.BadRequest("File required.").ToProblemResult();

            await using var stream = file.OpenReadStream();
            var storedResult = await files.SaveAsync(stream, file.FileName, file.ContentType, $"avatars/{userId}", ct);
            if (storedResult.IsFailure)
                return storedResult.ToHttpResult(_ => Results.Ok());

            profile.SetAvatar(storedResult.Value.StorageKey, DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { profile.AvatarPath });
        }).RequireAuthorization();

        group.MapGet("/{userId:guid}", async (Guid userId, AppDbContext db, CancellationToken ct) =>
        {
            var profile = await db.Profiles
                .Include(p => p.UserSkills).ThenInclude(us => us.Skill)
                .Include(p => p.PortfolioItems)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null)
                return ResultErrors.NotFound("Profile not found.").ToProblemResult();

            return Results.Ok(MapProfile(profile, includePrivate: false));
        });
    }

    private static object MapProfile(Profile profile, bool includePrivate) => new
    {
        profile.UserId,
        profile.DisplayName,
        profile.AvatarPath,
        profile.Bio,
        profile.AverageRating,
        profile.ReviewCount,
        Email = includePrivate ? profile.User.Email : null,
        Skills = profile.UserSkills.Select(us => us.Skill.Name).ToList(),
        Portfolio = profile.PortfolioItems.Select(p => new { p.Id, p.Title, p.Description, p.Url, p.CreatedAt })
    };
}

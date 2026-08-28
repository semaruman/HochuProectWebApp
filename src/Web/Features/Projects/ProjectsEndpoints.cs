using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Domain.ValueObjects;
using Web.Infrastructure.Files;
using Web.Infrastructure.Persistence;

namespace Web.Features.Projects;

public record CreateProjectRequest(string Title, string Description, Guid CategoryId, decimal BudgetAmount, DateOnly Deadline);
public record UpdateProjectRequest(string Title, string Description, Guid CategoryId, decimal BudgetAmount, DateOnly Deadline);
public record ProjectLiteDto(
    Guid Id,
    string Title,
    string Description,
    decimal BudgetAmount,
    string Currency,
    DateOnly Deadline,
    ProjectStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid BuyerId,
    Guid CategoryId);

public class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(10000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BudgetAmount).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.Deadline).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Deadline must be today or later.");
    }
}

public class UpdateProjectValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(10000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BudgetAmount).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.Deadline).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Deadline must be today or later.");
    }
}

public class ProjectsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapPost("/", async (
            CreateProjectRequest request,
            IValidator<CreateProjectRequest> validator,
            ICurrentUser currentUser,
            UserManager<ApplicationUser> userManager,
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

            var activeUser = await AccountGuards.RequireActiveUserAsync(userManager, userId, ct: ct);
            if (activeUser.IsFailure)
                return activeUser.ToHttpResult(_ => Results.Ok());

            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
                return ResultErrors.BadRequest("Category not found.").ToProblemResult();

            var budgetResult = Money.Rub(request.BudgetAmount);
            if (budgetResult.IsFailure)
                return budgetResult.ToHttpResult(_ => Results.Ok());

            var projectResult = Project.Create(
                userId,
                request.CategoryId,
                request.Title,
                request.Description,
                budgetResult.Value,
                request.Deadline,
                DateTime.UtcNow);
            if (projectResult.IsFailure)
                return projectResult.ToHttpResult(_ => Results.Ok());

            var project = projectResult.Value;
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{project.Id}", MapLite(project));
        }).RequireAuthorization();

        group.MapGet("/", async (
            AppDbContext db,
            Guid? categoryId,
            string? q,
            ProjectStatus? status,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var filterStatus = status ?? ProjectStatus.Published;

            var query = db.Projects.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Status == filterStatus);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Title, $"%{term}%")
                                      || EF.Functions.ILike(p.Description, $"%{term}%"));
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.BudgetAmount,
                    p.Currency,
                    p.Deadline,
                    p.Status,
                    p.CreatedAt,
                    Category = new { p.Category.Id, p.Category.Name, p.Category.Slug },
                    p.BuyerId
                })
                .ToListAsync(ct);

            return Results.Ok(new { total, page, pageSize, items });
        });

        group.MapGet("/mine", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var items = await db.Projects.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.BuyerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(items.Select(MapLite));
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound("Project not found.").ToProblemResult();

            var userId = currentUser.TryGetUserId();
            if (project.Status == ProjectStatus.Draft && project.BuyerId != userId)
                return ResultErrors.Forbidden().ToProblemResult();

            return Results.Ok(new
            {
                project.Id,
                project.Title,
                project.Description,
                project.BudgetAmount,
                project.Currency,
                project.Deadline,
                project.Status,
                project.CreatedAt,
                project.UpdatedAt,
                project.BuyerId,
                Category = new { project.Category.Id, project.Category.Name, project.Category.Slug },
                Attachments = project.Attachments.Select(a => new { a.Id, a.FileName, a.ContentType, a.SizeBytes })
            });
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProjectRequest request,
            IValidator<UpdateProjectRequest> validator,
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

            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!project.IsOwner(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
                return ResultErrors.BadRequest("Category not found.").ToProblemResult();

            var budgetResult = Money.Rub(request.BudgetAmount);
            if (budgetResult.IsFailure)
                return budgetResult.ToHttpResult(_ => Results.Ok());

            var updateResult = project.UpdateDetails(
                request.Title,
                request.Description,
                request.CategoryId,
                budgetResult.Value,
                request.Deadline,
                DateTime.UtcNow);
            if (updateResult.IsFailure)
                return updateResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(MapLite(project));
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/publish", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!project.IsOwner(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            var publishResult = project.Publish(DateTime.UtcNow);
            if (publishResult.IsFailure)
                return publishResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(MapLite(project));
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/cancel", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!project.IsOwner(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            if (project.Status == ProjectStatus.InProgress)
            {
                var deal = await db.Deals.FirstOrDefaultAsync(d => d.ProjectId == id, ct);
                if (deal is not null && deal.Status is not DealStatus.Cancelled)
                    return ResultErrors.Business("Cancel the deal before cancelling an in-progress project.").ToProblemResult();
            }

            var cancelResult = project.Cancel(DateTime.UtcNow);
            if (cancelResult.IsFailure)
                return cancelResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(MapLite(project));
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/attachments", async (
            Guid id,
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

            var project = await db.Projects.Include(p => p.Attachments).FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!project.IsOwner(userId))
                return ResultErrors.Forbidden().ToProblemResult();
            if (!project.CanAttachFiles())
                return ResultErrors.Business("Cannot attach files to this project.").ToProblemResult();
            if (project.Attachments.Count >= 5)
                return ResultErrors.Business("Maximum 5 attachments.").ToProblemResult();
            if (!request.HasFormContentType)
                return ResultErrors.BadRequest("Multipart form expected.").ToProblemResult();

            var file = request.Form.Files.FirstOrDefault();
            if (file is null)
                return ResultErrors.BadRequest("File required.").ToProblemResult();

            await using var stream = file.OpenReadStream();
            var storedResult = await files.SaveAsync(stream, file.FileName, file.ContentType, $"projects/{id}", ct);
            if (storedResult.IsFailure)
                return storedResult.ToHttpResult(_ => Results.Ok());

            var stored = storedResult.Value;
            var attachment = ProjectAttachment.Create(
                id, stored.StorageKey, stored.FileName, stored.ContentType, stored.SizeBytes, DateTime.UtcNow);
            db.ProjectAttachments.Add(attachment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{id}", new { attachment.Id, attachment.FileName, attachment.SizeBytes });
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (
            Guid id,
            Guid attachmentId,
            ICurrentUser currentUser,
            AppDbContext db,
            IFileStorage files,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!project.IsOwner(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            var attachment = await db.ProjectAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.ProjectId == id, ct);
            if (attachment is null)
                return ResultErrors.NotFound().ToProblemResult();

            await files.DeleteAsync(attachment.StorageKey, ct);
            db.ProjectAttachments.Remove(attachment);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static ProjectLiteDto MapLite(Project p) => new(
        p.Id, p.Title, p.Description, p.BudgetAmount, p.Currency, p.Deadline,
        p.Status, p.CreatedAt, p.UpdatedAt, p.BuyerId, p.CategoryId);
}

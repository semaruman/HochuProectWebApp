using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Infrastructure.Files;
using Web.Infrastructure.Persistence;

namespace Web.Features.Files;

public class FilesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("Files").RequireAuthorization();

        group.MapGet("/deliverable-files/{fileId:guid}", async (
            Guid fileId,
            ICurrentUser currentUser,
            AppDbContext db,
            IFileStorage storage,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var file = await db.DealDeliverableFiles.AsNoTracking()
                .Include(f => f.Deliverable)
                .ThenInclude(d => d.Deal)
                .FirstOrDefaultAsync(f => f.Id == fileId, ct);
            if (file is null)
                return ResultErrors.NotFound("File not found.").ToProblemResult();

            if (!file.Deliverable.Deal.IsParticipant(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            var stream = await storage.OpenReadAsync(file.StorageKey, ct);
            if (stream is null)
                return ResultErrors.NotFound("File content not found.").ToProblemResult();

            return Results.File(stream, file.ContentType, file.FileName);
        });

        group.MapGet("/project-attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            ICurrentUser currentUser,
            AppDbContext db,
            IFileStorage storage,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var attachment = await db.ProjectAttachments.AsNoTracking()
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
            if (attachment is null)
                return ResultErrors.NotFound("Attachment not found.").ToProblemResult();

            var project = attachment.Project;
            var canAccess = project.BuyerId == userId
                || project.Status == Web.Domain.Enums.ProjectStatus.Published
                || await db.Deals.AnyAsync(d => d.ProjectId == project.Id && (d.BuyerId == userId || d.SellerId == userId), ct);

            if (!canAccess)
                return ResultErrors.Forbidden().ToProblemResult();

            var stream = await storage.OpenReadAsync(attachment.StorageKey, ct);
            if (stream is null)
                return ResultErrors.NotFound("File content not found.").ToProblemResult();

            return Results.File(stream, attachment.ContentType, attachment.FileName);
        });
    }
}

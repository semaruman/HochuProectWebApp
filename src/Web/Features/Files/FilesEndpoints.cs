using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Errors;
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
            var file = await db.DealDeliverableFiles.AsNoTracking()
                .Include(f => f.Deliverable)
                .ThenInclude(d => d.Deal)
                .FirstOrDefaultAsync(f => f.Id == fileId, ct)
                ?? throw AppErrors.NotFound("File not found.");

            if (!file.Deliverable.Deal.IsParticipant(currentUser.UserId))
                throw AppErrors.Forbidden();

            var stream = await storage.OpenReadAsync(file.StorageKey, ct)
                ?? throw AppErrors.NotFound("File content not found.");
            return Results.File(stream, file.ContentType, file.FileName);
        });

        group.MapGet("/project-attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            ICurrentUser currentUser,
            AppDbContext db,
            IFileStorage storage,
            CancellationToken ct) =>
        {
            var attachment = await db.ProjectAttachments.AsNoTracking()
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
                ?? throw AppErrors.NotFound("Attachment not found.");

            var userId = currentUser.UserId;
            var project = attachment.Project;
            var canAccess = project.BuyerId == userId
                || project.Status == Web.Domain.Enums.ProjectStatus.Published
                || await db.Deals.AnyAsync(d => d.ProjectId == project.Id && (d.BuyerId == userId || d.SellerId == userId), ct);

            if (!canAccess)
                throw AppErrors.Forbidden();

            var stream = await storage.OpenReadAsync(attachment.StorageKey, ct)
                ?? throw AppErrors.NotFound("File content not found.");
            return Results.File(stream, attachment.ContentType, attachment.FileName);
        });
    }
}

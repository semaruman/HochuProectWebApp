using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Infrastructure.Persistence;

namespace Web.Features.Deals;

public record SubmitWorkRequest(string? Message);
public record RequestRevisionRequest(string Comment);

public class DealsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/deals").WithTags("Deals").RequireAuthorization();

        group.MapGet("/mine", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var deals = await db.Deals.AsNoTracking()
                .Where(d => d.BuyerId == userId || d.SellerId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DealListItemDto(
                    d.Id,
                    d.ProjectId,
                    d.BidId,
                    d.BuyerId,
                    d.SellerId,
                    d.Amount,
                    d.Status,
                    d.CreatedAt,
                    d.FundedAt,
                    d.SubmittedAt,
                    d.CompletedAt,
                    db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Title).FirstOrDefault()))
                .ToListAsync(ct);
            return Results.Ok(deals);
        });

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var deal = await db.Deals.AsNoTracking()
                .Include(d => d.Deliverables).ThenInclude(x => x.Files)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (deal is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (!deal.IsParticipant(userId))
                return ResultErrors.Forbidden().ToProblemResult();

            var projectTitle = await db.Projects.Where(p => p.Id == deal.ProjectId).Select(p => p.Title).FirstAsync(ct);
            return Results.Ok(new DealDetailsDto(
                deal.Id,
                deal.ProjectId,
                projectTitle,
                deal.BidId,
                deal.BuyerId,
                deal.SellerId,
                deal.Amount,
                deal.Status,
                deal.CreatedAt,
                deal.FundedAt,
                deal.SubmittedAt,
                deal.CompletedAt,
                deal.CancelledAt,
                deal.RevisionRequestedAt,
                deal.LastRevisionComment,
                deal.Deliverables.Select(d => new DealDeliverableDto(
                    d.Id,
                    d.Message,
                    d.CreatedAt,
                    d.Files.Select(f => new DealFileDto(f.Id, f.FileName, f.ContentType, f.SizeBytes)).ToList())).ToList()));
        });

        group.MapPost("/{id:guid}/fund", async (
            Guid id,
            ICurrentUser currentUser,
            FundDealHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var result = await handler.HandleAsync(id, userIdResult.Value, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        });

        group.MapPost("/{id:guid}/submit", async (
            Guid id,
            HttpRequest request,
            ICurrentUser currentUser,
            SubmitWorkHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());

            string? message = null;
            List<DeliverableUpload>? uploads = null;
            if (request.HasFormContentType)
            {
                message = request.Form["message"].ToString();
                uploads = request.Form.Files
                    .Select(file => new DeliverableUpload(file.OpenReadStream(), file.FileName, file.ContentType))
                    .ToList();
            }
            else
            {
                var body = await request.ReadFromJsonAsync<SubmitWorkRequest>(ct);
                message = body?.Message;
            }

            var result = await handler.HandleAsync(id, userIdResult.Value, message, uploads, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        });

        group.MapPost("/{id:guid}/accept", async (
            Guid id,
            ICurrentUser currentUser,
            AcceptWorkHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var result = await handler.HandleAsync(id, userIdResult.Value, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        });

        group.MapPost("/{id:guid}/request-revision", async (
            Guid id,
            RequestRevisionRequest request,
            ICurrentUser currentUser,
            RequestRevisionHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var result = await handler.HandleAsync(id, userIdResult.Value, request.Comment, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            ICurrentUser currentUser,
            CancelDealHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var result = await handler.HandleAsync(id, userIdResult.Value, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        });
    }
}

public sealed record DealListItemDto(
    Guid Id,
    Guid ProjectId,
    Guid BidId,
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    Web.Domain.Enums.DealStatus Status,
    DateTime CreatedAt,
    DateTime? FundedAt,
    DateTime? SubmittedAt,
    DateTime? CompletedAt,
    string? ProjectTitle);

public sealed record DealDetailsDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid BidId,
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    Web.Domain.Enums.DealStatus Status,
    DateTime CreatedAt,
    DateTime? FundedAt,
    DateTime? SubmittedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    DateTime? RevisionRequestedAt,
    string? LastRevisionComment,
    IReadOnlyList<DealDeliverableDto> Deliverables);

public sealed record DealDeliverableDto(Guid Id, string? Message, DateTime CreatedAt, IReadOnlyList<DealFileDto> Files);
public sealed record DealFileDto(Guid Id, string FileName, string ContentType, long SizeBytes);

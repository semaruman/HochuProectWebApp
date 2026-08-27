using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Errors;
using Web.Common.Validation;
using Web.Domain.ValueObjects;
using Web.Infrastructure.Persistence;

namespace Web.Features.Bids;

public record CreateBidRequest(decimal Price, int EstimatedDays, string CoverLetter);
public record UpdateBidRequest(decimal Price, int EstimatedDays, string CoverLetter);

public class CreateBidValidator : AbstractValidator<CreateBidRequest>
{
    public CreateBidValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.EstimatedDays).GreaterThan(0).LessThanOrEqualTo(3650);
        RuleFor(x => x.CoverLetter).NotEmpty().MinimumLength(20).MaximumLength(5000);
    }
}

public class UpdateBidValidator : AbstractValidator<UpdateBidRequest>
{
    public UpdateBidValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.EstimatedDays).GreaterThan(0).LessThanOrEqualTo(3650);
        RuleFor(x => x.CoverLetter).NotEmpty().MinimumLength(20).MaximumLength(5000);
    }
}

public class BidsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/bids", async (
            Guid projectId,
            CreateBidRequest request,
            IValidator<CreateBidRequest> validator,
            ICurrentUser currentUser,
            CreateBidHandler handler,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var bid = await handler.HandleAsync(
                projectId, currentUser.UserId, request.Price, request.EstimatedDays, request.CoverLetter, ct);
            return Results.Created($"/api/bids/{bid.Id}", bid);
        }).RequireAuthorization().WithTags("Bids");

        app.MapGet("/api/projects/{projectId:guid}/bids", async (
            Guid projectId,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct)
                ?? throw AppErrors.NotFound();
            if (project.BuyerId != userId)
                throw AppErrors.Forbidden("Only the project owner can view bids.");

            var bids = await db.Bids.AsNoTracking()
                .Where(b => b.ProjectId == projectId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.Id,
                    b.ProjectId,
                    b.SellerId,
                    b.Price,
                    b.EstimatedDays,
                    b.CoverLetter,
                    b.Status,
                    b.CreatedAt,
                    SellerName = db.Profiles.Where(p => p.UserId == b.SellerId).Select(p => p.DisplayName).FirstOrDefault()
                })
                .ToListAsync(ct);
            return Results.Ok(bids);
        }).RequireAuthorization().WithTags("Bids");

        app.MapGet("/api/bids/mine", async (ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            var bids = await db.Bids.AsNoTracking()
                .Where(b => b.SellerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(bids.Select(CreateBidHandler.Map));
        }).RequireAuthorization().WithTags("Bids");

        app.MapPut("/api/bids/{id:guid}", async (
            Guid id,
            UpdateBidRequest request,
            IValidator<UpdateBidRequest> validator,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var userId = currentUser.UserId;
            var bid = await db.Bids.FirstOrDefaultAsync(b => b.Id == id, ct)
                ?? throw AppErrors.NotFound();
            if (bid.SellerId != userId)
                throw AppErrors.Forbidden();

            bid.UpdateOffer(Money.Rub(request.Price), request.EstimatedDays, request.CoverLetter, DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(CreateBidHandler.Map(bid));
        }).RequireAuthorization().WithTags("Bids");

        app.MapPost("/api/bids/{id:guid}/withdraw", async (
            Guid id,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            var bid = await db.Bids.FirstOrDefaultAsync(b => b.Id == id, ct)
                ?? throw AppErrors.NotFound();
            if (bid.SellerId != userId)
                throw AppErrors.Forbidden();
            bid.Withdraw(DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(CreateBidHandler.Map(bid));
        }).RequireAuthorization().WithTags("Bids");

        app.MapPost("/api/bids/{id:guid}/accept", async (
            Guid id,
            ICurrentUser currentUser,
            AcceptBidHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, currentUser.UserId, ct);
            return Results.Ok(result);
        }).RequireAuthorization().WithTags("Bids");
    }
}

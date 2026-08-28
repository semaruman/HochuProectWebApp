using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Common.Validation;
using Web.Domain.Entities;
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
            UserManager<ApplicationUser> userManager,
            CreateBidHandler handler,
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

            var result = await handler.HandleAsync(
                projectId, userId, request.Price, request.EstimatedDays, request.CoverLetter, ct);
            return result.ToHttpResult(bid => Results.Created($"/api/bids/{bid.Id}", bid));
        }).RequireAuthorization().WithTags("Bids");

        app.MapGet("/api/projects/{projectId:guid}/bids", async (
            Guid projectId,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
            if (project is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (project.BuyerId != userId)
                return ResultErrors.Forbidden("Only the project owner can view bids.").ToProblemResult();

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
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

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
            var validation = await validator.ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
                return validation.ToHttpResult(() => Results.Ok());

            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var bid = await db.Bids.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bid is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (bid.SellerId != userId)
                return ResultErrors.Forbidden().ToProblemResult();

            var priceResult = Money.Rub(request.Price);
            if (priceResult.IsFailure)
                return priceResult.ToHttpResult(_ => Results.Ok());

            var updateResult = bid.UpdateOffer(priceResult.Value, request.EstimatedDays, request.CoverLetter, DateTime.UtcNow);
            if (updateResult.IsFailure)
                return updateResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(CreateBidHandler.Map(bid));
        }).RequireAuthorization().WithTags("Bids");

        app.MapPost("/api/bids/{id:guid}/withdraw", async (
            Guid id,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var bid = await db.Bids.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bid is null)
                return ResultErrors.NotFound().ToProblemResult();
            if (bid.SellerId != userId)
                return ResultErrors.Forbidden().ToProblemResult();

            var withdrawResult = bid.Withdraw(DateTime.UtcNow);
            if (withdrawResult.IsFailure)
                return withdrawResult.ToHttpResult(() => Results.Ok());

            await db.SaveChangesAsync(ct);
            return Results.Ok(CreateBidHandler.Map(bid));
        }).RequireAuthorization().WithTags("Bids");

        app.MapPost("/api/bids/{id:guid}/accept", async (
            Guid id,
            ICurrentUser currentUser,
            AcceptBidHandler handler,
            CancellationToken ct) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var result = await handler.HandleAsync(id, userIdResult.Value, ct);
            return result.ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization().WithTags("Bids");
    }
}

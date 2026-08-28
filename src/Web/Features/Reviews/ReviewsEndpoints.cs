using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Common.Validation;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reviews;

public record CreateReviewRequest(int Rating, string Comment);

public class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MinimumLength(5).MaximumLength(2000);
    }
}

public class ReviewsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/deals/{dealId:guid}/reviews", async (
            Guid dealId,
            CreateReviewRequest request,
            IValidator<CreateReviewRequest> validator,
            ICurrentUser currentUser,
            CreateReviewHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
                return validation.ToHttpResult(() => Results.Ok());

            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());

            var result = await handler.HandleAsync(dealId, userIdResult.Value, request.Rating, request.Comment, ct);
            return result.ToHttpResult(review => Results.Created($"/api/profiles/{review.RecipientId}/reviews", review));
        }).RequireAuthorization().WithTags("Reviews");

        app.MapGet("/api/profiles/{userId:guid}/reviews", async (Guid userId, AppDbContext db, CancellationToken ct) =>
        {
            var reviews = await db.Reviews.AsNoTracking()
                .Where(r => r.RecipientId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.DealId,
                    r.AuthorId,
                    AuthorName = db.Profiles.Where(p => p.UserId == r.AuthorId).Select(p => p.DisplayName).FirstOrDefault(),
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync(ct);
            return Results.Ok(reviews);
        }).WithTags("Reviews");
    }
}

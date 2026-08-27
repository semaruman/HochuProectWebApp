using Microsoft.EntityFrameworkCore;
using Web.Common.Errors;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reviews;

public sealed record ReviewDto(
    Guid Id,
    Guid DealId,
    Guid AuthorId,
    Guid RecipientId,
    int Rating,
    string Comment,
    DateTime CreatedAt);

public sealed class CreateReviewHandler(AppDbContext db)
{
    public async Task<ReviewDto> HandleAsync(Guid dealId, Guid authorId, int rating, string comment, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw AppErrors.NotFound();
        if (!deal.IsParticipant(authorId))
            throw AppErrors.Forbidden();
        if (deal.Status != DealStatus.Completed)
            throw AppErrors.Business("Reviews are allowed only after deal completion.");

        var recipientId = deal.BuyerId == authorId ? deal.SellerId : deal.BuyerId;
        if (await db.Reviews.AnyAsync(r => r.DealId == dealId && r.AuthorId == authorId, ct))
            throw AppErrors.Conflict("You already left a review for this deal.");

        var utcNow = DateTime.UtcNow;
        var review = Review.Create(dealId, authorId, recipientId, rating, comment, utcNow);
        db.Reviews.Add(review);

        var profile = await db.Profiles
            .FromSqlInterpolated($@"SELECT * FROM ""Profiles"" WHERE ""UserId"" = {recipientId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
        if (profile is not null)
        {
            var ratings = await db.Reviews
                .Where(r => r.RecipientId == recipientId)
                .Select(r => r.Rating)
                .ToListAsync(ct);
            ratings.Add(review.Rating);
            profile.RecalculateRating(ratings);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ReviewDto(review.Id, review.DealId, review.AuthorId, review.RecipientId, review.Rating, review.Comment, review.CreatedAt);
    }
}

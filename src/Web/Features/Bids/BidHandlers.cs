using Microsoft.EntityFrameworkCore;
using Web.Common.Errors;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Domain.ValueObjects;
using Web.Infrastructure.DomainEvents;
using Web.Infrastructure.Persistence;

namespace Web.Features.Bids;

public sealed record BidDto(
    Guid Id,
    Guid ProjectId,
    Guid SellerId,
    decimal Price,
    int EstimatedDays,
    string CoverLetter,
    BidStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AcceptBidResult(Guid DealId, Guid ProjectId, Guid BidId);

public sealed class CreateBidHandler(AppDbContext db, IDomainEventDispatcher dispatcher)
{
    public async Task<BidDto> HandleAsync(
        Guid projectId,
        Guid sellerId,
        decimal price,
        int estimatedDays,
        string coverLetter,
        CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw AppErrors.NotFound("Project not found.");

        var exists = await db.Bids.AnyAsync(b =>
            b.ProjectId == projectId && b.SellerId == sellerId && b.Status == BidStatus.Pending, ct);
        if (exists)
            throw AppErrors.Conflict("You already have a pending bid on this project.");

        var bid = Bid.Place(project, sellerId, Money.Rub(price), estimatedDays, coverLetter, DateTime.UtcNow);
        db.Bids.Add(bid);
        await db.SaveAndDispatchAsync(dispatcher, ct);
        return Map(bid);
    }

    public static BidDto Map(Bid bid) => new(
        bid.Id, bid.ProjectId, bid.SellerId, bid.Price, bid.EstimatedDays,
        bid.CoverLetter, bid.Status, bid.CreatedAt, bid.UpdatedAt);
}

public sealed class AcceptBidHandler(AppDbContext db, IDomainEventDispatcher dispatcher)
{
    public async Task<AcceptBidResult> HandleAsync(Guid bidId, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var bid = await db.Bids.FirstOrDefaultAsync(b => b.Id == bidId, ct)
            ?? throw AppErrors.NotFound("Bid not found.");
        if (!bid.IsPending)
            throw AppErrors.Conflict("Bid is not pending.");

        var utcNow = DateTime.UtcNow;
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE ""Projects""
SET ""Status"" = {(int)ProjectStatus.InProgress}, ""UpdatedAt"" = {utcNow}, ""RowVersion"" = ""RowVersion"" + 1
WHERE ""Id"" = {bid.ProjectId} AND ""Status"" = {(int)ProjectStatus.Published} AND ""BuyerId"" = {buyerId}", ct);

        if (affected != 1)
            throw AppErrors.Conflict("Project is not available for accepting a bid.");

        var project = await db.Projects.FirstAsync(p => p.Id == bid.ProjectId, ct);
        var otherPending = await db.Bids
            .Where(b => b.ProjectId == bid.ProjectId && b.Id != bid.Id && b.Status == BidStatus.Pending)
            .ToListAsync(ct);

        var deal = project.RecordAcceptedBid(bid, otherPending, utcNow);
        db.Deals.Add(deal);

        try
        {
            await db.SaveAndDispatchAsync(dispatcher, ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            throw AppErrors.Conflict("Another bid was already accepted for this project.");
        }

        return new AcceptBidResult(deal.Id, project.Id, bid.Id);
    }
}

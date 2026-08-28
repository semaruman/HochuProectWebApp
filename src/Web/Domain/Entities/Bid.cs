using Web.Common.Results;
using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.ValueObjects;

namespace Web.Domain.Entities;

public class Bid : Entity
{
    private Bid()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal Price { get; private set; }
    public int EstimatedDays { get; private set; }
    public string CoverLetter { get; private set; } = string.Empty;
    public BidStatus Status { get; private set; } = BidStatus.Pending;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Project Project { get; private set; } = null!;
    public ApplicationUser Seller { get; private set; } = null!;
    public Deal? Deal { get; private set; }

    public Money Offer => Money.FromTrusted(Price, "RUB");
    public bool IsPending => Status == BidStatus.Pending;

    public static Result<Bid> Place(
        Project project,
        Guid sellerId,
        Money price,
        int estimatedDays,
        string coverLetter,
        DateTime utcNow)
    {
        if (project.Status != ProjectStatus.Published)
            return ResultErrors.Business("Bids are only accepted on published projects.");
        if (project.BuyerId == sellerId)
            return ResultErrors.Business("You cannot bid on your own project.");
        if (estimatedDays is <= 0 or > 3650)
            return ResultErrors.Business("Estimated days are out of range.");
        if (string.IsNullOrWhiteSpace(coverLetter) || coverLetter.Trim().Length < 20)
            return ResultErrors.Business("Cover letter is too short.");

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            SellerId = sellerId,
            Price = price.Amount,
            EstimatedDays = estimatedDays,
            CoverLetter = coverLetter.Trim(),
            Status = BidStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
        bid.Raise(new BidPlaced(bid.Id, project.Id, project.BuyerId, sellerId, project.Title, utcNow));
        return bid;
    }

    public Result UpdateOffer(Money price, int estimatedDays, string coverLetter, DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            return ResultErrors.Business("Only pending bids can be edited.");
        if (estimatedDays is <= 0 or > 3650)
            return ResultErrors.Business("Estimated days are out of range.");
        if (string.IsNullOrWhiteSpace(coverLetter) || coverLetter.Trim().Length < 20)
            return ResultErrors.Business("Cover letter is too short.");

        Price = price.Amount;
        EstimatedDays = estimatedDays;
        CoverLetter = coverLetter.Trim();
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Withdraw(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            return ResultErrors.Business("Only pending bids can be withdrawn.");
        Status = BidStatus.Withdrawn;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    internal Result Accept(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            return ResultErrors.Business("Only pending bids can be accepted.");
        Status = BidStatus.Accepted;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    internal Result Reject(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            return ResultErrors.Business("Only pending bids can be rejected.");
        Status = BidStatus.Rejected;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}

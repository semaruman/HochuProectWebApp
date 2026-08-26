using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.Exceptions;
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

    public Money Offer => new(Price, "RUB");
    public bool IsPending => Status == BidStatus.Pending;

    public static Bid Place(
        Project project,
        Guid sellerId,
        Money price,
        int estimatedDays,
        string coverLetter,
        DateTime utcNow)
    {
        if (project.Status != ProjectStatus.Published)
            throw new DomainException("Bids are only accepted on published projects.");
        if (project.BuyerId == sellerId)
            throw new DomainException("You cannot bid on your own project.");
        if (estimatedDays is <= 0 or > 3650)
            throw new DomainException("Estimated days are out of range.");
        if (string.IsNullOrWhiteSpace(coverLetter) || coverLetter.Trim().Length < 20)
            throw new DomainException("Cover letter is too short.");

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

    public void UpdateOffer(Money price, int estimatedDays, string coverLetter, DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            throw new DomainException("Only pending bids can be edited.");
        if (estimatedDays is <= 0 or > 3650)
            throw new DomainException("Estimated days are out of range.");
        if (string.IsNullOrWhiteSpace(coverLetter) || coverLetter.Trim().Length < 20)
            throw new DomainException("Cover letter is too short.");

        Price = price.Amount;
        EstimatedDays = estimatedDays;
        CoverLetter = coverLetter.Trim();
        UpdatedAt = utcNow;
    }

    public void Withdraw(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            throw new DomainException("Only pending bids can be withdrawn.");
        Status = BidStatus.Withdrawn;
        UpdatedAt = utcNow;
    }

    internal void Accept(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            throw new DomainException("Only pending bids can be accepted.");
        Status = BidStatus.Accepted;
        UpdatedAt = utcNow;
    }

    internal void Reject(DateTime utcNow)
    {
        if (Status != BidStatus.Pending)
            throw new DomainException("Only pending bids can be rejected.");
        Status = BidStatus.Rejected;
        UpdatedAt = utcNow;
    }
}

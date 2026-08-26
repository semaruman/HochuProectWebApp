using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.Exceptions;

namespace Web.Domain.Entities;

public class Deal : Entity
{
    private Deal()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BidId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal Amount { get; private set; }
    public DealStatus Status { get; private set; } = DealStatus.Created;
    public DateTime CreatedAt { get; private set; }
    public DateTime? FundedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public long RowVersion { get; private set; }

    public Project Project { get; private set; } = null!;
    public Bid Bid { get; private set; } = null!;
    public ApplicationUser Buyer { get; private set; } = null!;
    public ApplicationUser Seller { get; private set; } = null!;
    public Conversation? Conversation { get; private set; }
    public Payment? Payment { get; private set; }
    public ICollection<DealDeliverable> Deliverables { get; private set; } = new List<DealDeliverable>();
    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    public bool IsParticipant(Guid userId) => userId == BuyerId || userId == SellerId;
    public bool IsFunded => Status == DealStatus.InProgress && FundedAt is not null;
    public bool IsCompleted => Status == DealStatus.Completed;

    public static Deal FromAcceptedBid(Project project, Bid bid, DateTime utcNow)
    {
        if (bid.ProjectId != project.Id)
            throw new DomainException("Bid does not belong to this project.");

        var deal = new Deal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            BidId = bid.Id,
            BuyerId = project.BuyerId,
            SellerId = bid.SellerId,
            Amount = bid.Price,
            Status = DealStatus.Created,
            CreatedAt = utcNow
        };
        deal.Conversation = Conversation.Open(deal.Id, utcNow);
        return deal;
    }

    public void Fund(DateTime utcNow)
    {
        if (Status != DealStatus.Created)
            throw new DomainException("Only created deals can be funded.");
        Status = DealStatus.InProgress;
        FundedAt = utcNow;
        Raise(new DealFunded(Id, BuyerId, SellerId, Amount, utcNow));
    }

    public DealDeliverable SubmitWork(string? message, DateTime utcNow)
    {
        if (Status != DealStatus.InProgress)
            throw new DomainException("Only in-progress deals can be submitted.");
        Status = DealStatus.Submitted;
        SubmittedAt = utcNow;
        var deliverable = DealDeliverable.Create(Id, message, utcNow);
        Raise(new WorkSubmitted(Id, BuyerId, SellerId, deliverable.Id, utcNow));
        return deliverable;
    }

    public void Accept(DateTime utcNow)
    {
        if (Status != DealStatus.Submitted)
            throw new DomainException("Only submitted deals can be accepted.");
        Status = DealStatus.Completed;
        CompletedAt = utcNow;
        Raise(new DealCompleted(Id, BuyerId, SellerId, Payment?.Id, utcNow));
    }

    public void Cancel(Guid actorId, DateTime utcNow)
    {
        if (Status is DealStatus.Completed or DealStatus.Cancelled)
            throw new DomainException("Deal cannot be cancelled in its current status.");
        if (!IsParticipant(actorId))
            throw new DomainException("Only deal participants can cancel the deal.");
        Status = DealStatus.Cancelled;
        CancelledAt = utcNow;
        Raise(new DealCancelled(Id, actorId, BuyerId, SellerId, utcNow));
    }
}

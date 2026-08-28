using Web.Common.Results;
using Web.Domain.Enums;
using Web.Domain.Events;

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
    public DateTime? RevisionRequestedAt { get; private set; }
    public string? LastRevisionComment { get; private set; }
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
    public bool IsWorkStarted => Status is DealStatus.InProgress or DealStatus.Submitted or DealStatus.RevisionRequired;
    public bool IsFunded => FundedAt is not null && IsWorkStarted;
    public bool IsCompleted => Status == DealStatus.Completed;
    public bool CanSubmitWork => Status is DealStatus.InProgress or DealStatus.RevisionRequired;
    public bool AwaitingBuyerReview => Status == DealStatus.Submitted;

    public static Result<Deal> FromAcceptedBid(Project project, Bid bid, DateTime utcNow)
    {
        if (bid.ProjectId != project.Id)
            return ResultErrors.Business("Bid does not belong to this project.");

        var deal = new Deal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            BidId = bid.Id,
            BuyerId = project.BuyerId,
            SellerId = bid.SellerId,
            Amount = bid.Price,
            Status = DealStatus.InProgress,
            CreatedAt = utcNow,
            FundedAt = utcNow
        };
        deal.Conversation = Conversation.Open(deal.Id, utcNow);
        return deal;
    }

    public Result Fund(DateTime utcNow)
    {
        if (Status == DealStatus.InProgress && FundedAt is not null)
            return Result.Success();
        if (Status != DealStatus.Created)
            return ResultErrors.Business("Only created deals can be funded.");
        Status = DealStatus.InProgress;
        FundedAt = utcNow;
        Raise(new DealFunded(Id, BuyerId, SellerId, Amount, utcNow));
        return Result.Success();
    }

    public Result<DealDeliverable> SubmitWork(string? message, DateTime utcNow)
    {
        if (!CanSubmitWork)
            return ResultErrors.Business("Deal is not ready for work submission.");
        Status = DealStatus.Submitted;
        SubmittedAt = utcNow;
        RevisionRequestedAt = null;
        LastRevisionComment = null;
        var deliverable = DealDeliverable.Create(Id, message, utcNow);
        Raise(new WorkSubmitted(Id, BuyerId, SellerId, deliverable.Id, utcNow));
        return deliverable;
    }

    public Result RequestRevision(string comment, DateTime utcNow)
    {
        if (Status != DealStatus.Submitted)
            return ResultErrors.Business("Only submitted deals can be returned for revision.");
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 5)
            return ResultErrors.Business("Revision comment is too short.");
        Status = DealStatus.RevisionRequired;
        RevisionRequestedAt = utcNow;
        LastRevisionComment = comment.Trim();
        Raise(new WorkRevisionRequested(Id, BuyerId, SellerId, LastRevisionComment, utcNow));
        return Result.Success();
    }

    public Result Accept(DateTime utcNow)
    {
        if (Status != DealStatus.Submitted)
            return ResultErrors.Business("Only submitted deals can be accepted.");
        Status = DealStatus.Completed;
        CompletedAt = utcNow;
        Raise(new DealCompleted(Id, BuyerId, SellerId, Payment?.Id, utcNow));
        return Result.Success();
    }

    public Result Cancel(Guid actorId, DateTime utcNow)
    {
        if (Status is DealStatus.Completed or DealStatus.Cancelled)
            return ResultErrors.Business("Deal cannot be cancelled in its current status.");
        if (!IsParticipant(actorId))
            return ResultErrors.Business("Only deal participants can cancel the deal.");
        Status = DealStatus.Cancelled;
        CancelledAt = utcNow;
        Raise(new DealCancelled(Id, actorId, BuyerId, SellerId, utcNow));
        return Result.Success();
    }
}

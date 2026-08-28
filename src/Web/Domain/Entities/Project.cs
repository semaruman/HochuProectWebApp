using Web.Common.Results;
using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.ValueObjects;

namespace Web.Domain.Entities;

public class Project : Entity
{
    private Project()
    {
    }

    public Guid Id { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal BudgetAmount { get; private set; }
    public string Currency { get; private set; } = "RUB";
    public DateOnly Deadline { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public long RowVersion { get; private set; }

    public ApplicationUser Buyer { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public ICollection<ProjectAttachment> Attachments { get; private set; } = new List<ProjectAttachment>();
    public ICollection<Bid> Bids { get; private set; } = new List<Bid>();
    public Deal? Deal { get; private set; }

    public Money Budget => Money.FromTrusted(BudgetAmount, Currency);
    public bool IsOwner(Guid userId) => BuyerId == userId;

    public static Result<Project> Create(
        Guid buyerId,
        Guid categoryId,
        string title,
        string description,
        Money budget,
        DateOnly deadline,
        DateTime utcNow)
    {
        if (buyerId == Guid.Empty)
            return ResultErrors.Business("Buyer is required.");
        if (categoryId == Guid.Empty)
            return ResultErrors.Business("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            return ResultErrors.Business("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            return ResultErrors.Business("Description is too short.");

        return new Project
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description.Trim(),
            BudgetAmount = budget.Amount,
            Currency = budget.Currency,
            Deadline = deadline,
            Status = ProjectStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public Result UpdateDetails(string title, string description, Guid categoryId, Money budget, DateOnly deadline, DateTime utcNow)
    {
        if (Status is not (ProjectStatus.Draft or ProjectStatus.Published))
            return ResultErrors.Business("Project cannot be edited in its current status.");
        if (categoryId == Guid.Empty)
            return ResultErrors.Business("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            return ResultErrors.Business("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            return ResultErrors.Business("Description is too short.");

        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        BudgetAmount = budget.Amount;
        Currency = budget.Currency;
        Deadline = deadline;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Publish(DateTime utcNow)
    {
        if (Status != ProjectStatus.Draft)
            return ResultErrors.Business("Only draft projects can be published.");
        Status = ProjectStatus.Published;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Cancel(DateTime utcNow)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            return ResultErrors.Business("Project cannot be cancelled in its current status.");
        Status = ProjectStatus.Cancelled;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkInProgress(DateTime utcNow)
    {
        if (Status != ProjectStatus.Published)
            return ResultErrors.Business("Only published projects can move to in progress.");
        Status = ProjectStatus.InProgress;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkCompleted(DateTime utcNow)
    {
        if (Status != ProjectStatus.InProgress)
            return ResultErrors.Business("Only in-progress projects can be completed.");
        Status = ProjectStatus.Completed;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Hide(DateTime utcNow)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            return ResultErrors.Business("Project cannot be hidden in its current status.");
        Status = ProjectStatus.Hidden;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result RestorePublication(DateTime utcNow)
    {
        if (Status != ProjectStatus.Hidden)
            return ResultErrors.Business("Only hidden projects can be restored.");
        Status = ProjectStatus.Published;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public bool CanAttachFiles() => Status is not (ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Hidden);

    public Result<Deal> RecordAcceptedBid(Bid bid, IReadOnlyCollection<Bid> otherPending, DateTime utcNow)
    {
        if (bid.ProjectId != Id)
            return ResultErrors.Business("Bid does not belong to this project.");
        if (Status != ProjectStatus.InProgress)
            return ResultErrors.Business("Project is not ready to accept a bid.");

        var accept = bid.Accept(utcNow);
        if (accept.IsFailure) return accept.Error;
        foreach (var other in otherPending)
        {
            var reject = other.Reject(utcNow);
            if (reject.IsFailure) return reject.Error;
        }

        var dealResult = Deal.FromAcceptedBid(this, bid, utcNow);
        if (dealResult.IsFailure) return dealResult.Error;

        Raise(new BidAccepted(Id, bid.Id, dealResult.Value.Id, BuyerId, bid.SellerId, Title, utcNow));
        return dealResult.Value;
    }
}

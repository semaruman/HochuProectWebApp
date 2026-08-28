using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.Exceptions;
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

    public Money Budget => new(BudgetAmount, Currency);
    public bool IsOwner(Guid userId) => BuyerId == userId;

    public static Project Create(
        Guid buyerId,
        Guid categoryId,
        string title,
        string description,
        Money budget,
        DateOnly deadline,
        DateTime utcNow)
    {
        if (buyerId == Guid.Empty)
            throw new DomainException("Buyer is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            throw new DomainException("Description is too short.");

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

    public void UpdateDetails(string title, string description, Guid categoryId, Money budget, DateOnly deadline, DateTime utcNow)
    {
        if (Status is not (ProjectStatus.Draft or ProjectStatus.Published))
            throw new DomainException("Project cannot be edited in its current status.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            throw new DomainException("Description is too short.");

        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        BudgetAmount = budget.Amount;
        Currency = budget.Currency;
        Deadline = deadline;
        UpdatedAt = utcNow;
    }

    public void Publish(DateTime utcNow)
    {
        if (Status != ProjectStatus.Draft)
            throw new DomainException("Only draft projects can be published.");
        Status = ProjectStatus.Published;
        UpdatedAt = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            throw new DomainException("Project cannot be cancelled in its current status.");
        Status = ProjectStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    public void MarkInProgress(DateTime utcNow)
    {
        if (Status != ProjectStatus.Published)
            throw new DomainException("Only published projects can move to in progress.");
        Status = ProjectStatus.InProgress;
        UpdatedAt = utcNow;
    }

    public void MarkCompleted(DateTime utcNow)
    {
        if (Status != ProjectStatus.InProgress)
            throw new DomainException("Only in-progress projects can be completed.");
        Status = ProjectStatus.Completed;
        UpdatedAt = utcNow;
    }

    public void Hide(DateTime utcNow)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            throw new DomainException("Project cannot be hidden in its current status.");
        Status = ProjectStatus.Hidden;
        UpdatedAt = utcNow;
    }

    public void RestorePublication(DateTime utcNow)
    {
        if (Status != ProjectStatus.Hidden)
            throw new DomainException("Only hidden projects can be restored.");
        Status = ProjectStatus.Published;
        UpdatedAt = utcNow;
    }

    public bool CanAttachFiles() => Status is not (ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Hidden);

    public Deal RecordAcceptedBid(Bid bid, IReadOnlyCollection<Bid> otherPending, DateTime utcNow)
    {
        if (bid.ProjectId != Id)
            throw new DomainException("Bid does not belong to this project.");
        if (Status != ProjectStatus.InProgress)
            throw new DomainException("Project is not ready to accept a bid.");

        bid.Accept(utcNow);
        foreach (var other in otherPending)
            other.Reject(utcNow);

        var deal = Deal.FromAcceptedBid(this, bid, utcNow);
        Raise(new BidAccepted(Id, bid.Id, deal.Id, BuyerId, bid.SellerId, Title, utcNow));
        return deal;
    }
}

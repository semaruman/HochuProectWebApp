using Web.Domain.Enums;
using Web.Domain.Exceptions;
using Web.Domain.ValueObjects;

namespace Web.Domain.Entities;

public class Service
{
    private Service()
    {
    }

    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DeliveryDays { get; private set; }
    public ServiceStatus Status { get; private set; } = ServiceStatus.Draft;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ApplicationUser Seller { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    public static Service Create(
        Guid sellerId,
        Guid categoryId,
        string title,
        string description,
        Money price,
        int deliveryDays,
        DateTime utcNow)
    {
        if (sellerId == Guid.Empty)
            throw new DomainException("Seller is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            throw new DomainException("Description is too short.");
        if (deliveryDays is <= 0 or > 3650)
            throw new DomainException("Delivery days are out of range.");

        return new Service
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description.Trim(),
            Price = price.Amount,
            DeliveryDays = deliveryDays,
            Status = ServiceStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Update(string title, string description, Guid categoryId, Money price, int deliveryDays, DateTime utcNow)
    {
        if (Status == ServiceStatus.Archived)
            throw new DomainException("Archived service cannot be edited.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title is too short.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 20)
            throw new DomainException("Description is too short.");
        if (deliveryDays is <= 0 or > 3650)
            throw new DomainException("Delivery days are out of range.");

        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        Price = price.Amount;
        DeliveryDays = deliveryDays;
        UpdatedAt = utcNow;
    }

    public void Publish(DateTime utcNow)
    {
        if (Status == ServiceStatus.Archived)
            throw new DomainException("Archived service cannot be published.");
        Status = ServiceStatus.Published;
        UpdatedAt = utcNow;
    }

    public void Archive(DateTime utcNow)
    {
        Status = ServiceStatus.Archived;
        UpdatedAt = utcNow;
    }
}

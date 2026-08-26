using Web.Domain.Enums;
using Web.Domain.Exceptions;

namespace Web.Domain.Entities;

public class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public string Provider { get; private set; } = "Stub";
    public string ProviderPaymentId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Deal Deal { get; private set; } = null!;

    public static Payment Authorize(
        Guid dealId,
        decimal amount,
        string provider,
        string providerPaymentId,
        DateTime utcNow)
    {
        if (dealId == Guid.Empty)
            throw new DomainException("Deal is required.");
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("Payment provider is required.");
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            throw new DomainException("Provider payment id is required.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Provider = provider.Trim(),
            ProviderPaymentId = providerPaymentId,
            Amount = amount,
            Status = PaymentStatus.Authorized,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void MarkCaptured(DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException("Only authorized payments can be captured.");
        Status = PaymentStatus.Captured;
        UpdatedAt = utcNow;
    }

    public void MarkRefunded(DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException("Only authorized payments can be refunded.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = utcNow;
    }
}

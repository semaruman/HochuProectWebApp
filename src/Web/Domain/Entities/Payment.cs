using Web.Common.Results;
using Web.Domain.Enums;

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

    public static Result<Payment> Authorize(
        Guid dealId,
        decimal amount,
        string provider,
        string providerPaymentId,
        DateTime utcNow)
    {
        if (dealId == Guid.Empty)
            return ResultErrors.Business("Deal is required.");
        if (amount <= 0)
            return ResultErrors.Business("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(provider))
            return ResultErrors.Business("Payment provider is required.");
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return ResultErrors.Business("Provider payment id is required.");

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

    public Result MarkCaptured(DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
            return ResultErrors.Business("Only authorized payments can be captured.");
        Status = PaymentStatus.Captured;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkRefunded(DateTime utcNow)
    {
        if (Status != PaymentStatus.Authorized)
            return ResultErrors.Business("Only authorized payments can be refunded.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}

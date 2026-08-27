namespace Web.Infrastructure.Payments;

public interface IPaymentService
{
    Task<PaymentResult> CreateAndAuthorizeAsync(Guid dealId, decimal amount, CancellationToken ct = default);
    Task<PaymentResult> CaptureAsync(string providerPaymentId, CancellationToken ct = default);
    Task<PaymentResult> RefundAsync(string providerPaymentId, CancellationToken ct = default);
    Task<PaymentStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default);
}

public sealed record PaymentResult(bool Success, string ProviderPaymentId, string Status, string? Error = null);
public sealed record PaymentStatusResult(string ProviderPaymentId, string Status);

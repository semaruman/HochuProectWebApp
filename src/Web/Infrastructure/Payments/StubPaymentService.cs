namespace Web.Infrastructure.Payments;

public class StubPaymentService : IPaymentService
{
    public Task<PaymentResult> CreateAndAuthorizeAsync(Guid dealId, decimal amount, CancellationToken ct = default)
    {
        var id = $"stub_{dealId:N}_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentResult(true, id, "Authorized"));
    }

    public Task<PaymentResult> CaptureAsync(string providerPaymentId, CancellationToken ct = default)
        => Task.FromResult(new PaymentResult(true, providerPaymentId, "Captured"));

    public Task<PaymentResult> RefundAsync(string providerPaymentId, CancellationToken ct = default)
        => Task.FromResult(new PaymentResult(true, providerPaymentId, "Refunded"));

    public Task<PaymentStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
        => Task.FromResult(new PaymentStatusResult(providerPaymentId, "Authorized"));
}

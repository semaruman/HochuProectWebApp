namespace Web.Infrastructure.Payments;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";
    public string Provider { get; set; } = "Stub";
}

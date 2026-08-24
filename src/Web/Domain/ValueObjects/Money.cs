using Web.Domain.Exceptions;

namespace Web.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Rub(decimal amount) => new(amount, "RUB");

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

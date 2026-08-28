using Web.Common.Results;

namespace Web.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> TryCreate(decimal amount, string currency)
    {
        if (amount <= 0)
            return ResultErrors.Business("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            return ResultErrors.Business("Currency must be a 3-letter ISO code.");

        var normalized = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        return Result<Money>.Success(new Money(normalized, currency.Trim().ToUpperInvariant()));
    }

    public static Result<Money> Rub(decimal amount) => TryCreate(amount, "RUB");

    internal static Money FromTrusted(decimal amount, string currency) => new(amount, currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

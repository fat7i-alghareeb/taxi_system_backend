using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class Quote
{
    private Quote() { }

    private Quote(decimal amount, string currency, DateTime expiresAtUtc)
    {
        Amount = amount;
        Currency = currency;
        ExpiresAtUtc = expiresAtUtc;
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;

    public static Result<Quote> Create(decimal amount, string currency, DateTime expiresAtUtc)
    {
        return new Quote(amount, currency, expiresAtUtc);
    }
}

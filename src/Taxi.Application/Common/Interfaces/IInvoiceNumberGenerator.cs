namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Allocates a unique, sequential invoice number for the given issuance time.
/// Implementations must be safe under concurrent issuance.
/// </summary>
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(DateTimeOffset issuedAtUtc, CancellationToken ct);
}

namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>How the passenger chose to pay for a new trip.</summary>
public enum TripPaymentMethod
{
    /// <summary>Pay the full fare with a card via the Stripe sheet (default / existing flow).</summary>
    Card,

    /// <summary>Pay the full fare from the wallet balance (synchronous, no Stripe).</summary>
    Wallet,

    /// <summary>Wallet balance first, remaining fare on the card (hold -> commit on card success).</summary>
    Mixed,
}

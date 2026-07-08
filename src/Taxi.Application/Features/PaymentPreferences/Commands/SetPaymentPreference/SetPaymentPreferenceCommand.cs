using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentPreferences.Commands.SetPaymentPreference;

/// <summary>Sets (or clears, when null) the passenger's preferred trip-booking method type.</summary>
public record SetPaymentPreferenceCommand(string? PreferredMethodType) : IRequest<Result<Success>>;

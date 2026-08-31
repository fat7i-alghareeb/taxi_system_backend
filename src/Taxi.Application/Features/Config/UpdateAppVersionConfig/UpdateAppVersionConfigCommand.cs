using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateAppVersionConfig;

// Flat rather than nested: the controller un-nests the request exactly as it already
// does for company-contact, and a flat shape keeps the cross-field min/latest rules
// expressible as plain RuleFor calls.
public record UpdateAppVersionConfigCommand(
    bool? Enabled,
    string? AndroidLatestVersion,
    string? AndroidMinimumRequiredVersion,
    string? AndroidStoreUrl,
    string? IosLatestVersion,
    string? IosMinimumRequiredVersion,
    string? IosStoreUrl)
    : IRequest<Result<AppVersionConfigDto>>;

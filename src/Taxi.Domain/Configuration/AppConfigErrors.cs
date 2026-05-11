using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Configuration;

public static class AppConfigErrors
{
    public static readonly Error KeyRequired = Error.Validation(
        code: LocalizationKeys.AppConfig.KeyRequired,
        description: "Configuration key is required.");

    public static readonly Error ValueRequired = Error.Validation(
        code: LocalizationKeys.AppConfig.ValueRequired,
        description: "Configuration value is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.AppConfig.NotFound,
        description: "Configuration was not found.");
}

using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.Sms;

public class ConsoleSmsProvider(ILogger<ConsoleSmsProvider> logger) : ISmsProvider
{
    public Task SendSmsAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("--- [SMS SIMULATOR] ---");
        logger.LogInformation("To: {Phone}", phone);
        logger.LogInformation("Message: {Message}", message);
        logger.LogInformation("-----------------------");

        return Task.CompletedTask;
    }
}

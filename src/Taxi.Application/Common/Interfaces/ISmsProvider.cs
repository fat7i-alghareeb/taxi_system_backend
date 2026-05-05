namespace Taxi.Application.Common.Interfaces;

public interface ISmsProvider
{
    Task SendSmsAsync(string phone, string message, CancellationToken cancellationToken = default);
}

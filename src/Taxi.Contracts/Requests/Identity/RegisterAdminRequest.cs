namespace Taxi.Contracts.Requests.Identity;

public record RegisterAdminRequest(
    string Phone,
    string Password,
    string Name,
    string? Email);


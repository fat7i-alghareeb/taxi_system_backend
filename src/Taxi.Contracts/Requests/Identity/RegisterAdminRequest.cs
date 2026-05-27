namespace Taxi.Contracts.Requests.Identity;

public record RegisterAdminRequest(
    string UserName,
    string Password,
    string Name,
    string Email,
    string? Phone1,
    string? Phone2);


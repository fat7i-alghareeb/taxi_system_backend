namespace Taxi.Contracts.Requests.Identity;

public record RegisterAdminRequest(
    string Phone,
    string Password,
    string NameEn,
    string NameAr,
    string NameNl,
    string? Email);


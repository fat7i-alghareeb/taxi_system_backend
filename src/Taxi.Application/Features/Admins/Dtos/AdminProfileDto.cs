namespace Taxi.Application.Features.Admins.Dtos;

public record AdminProfileDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone1,
    string? Phone2,
    bool IsActive);

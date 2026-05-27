namespace Taxi.Application.Features.Auth.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public Guid? DriverId { get; set; }
    public string? ApprovalStatus { get; set; }
    public bool RequiresPasswordReset { get; set; }
}


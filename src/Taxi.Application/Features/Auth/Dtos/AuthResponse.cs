namespace Taxi.Application.Features.Auth.Dtos;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    bool IsNewAccount = false,
    bool AccountAlreadyExists = false);

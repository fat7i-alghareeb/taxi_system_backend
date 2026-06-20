using Microsoft.AspNetCore.Http;

namespace Taxi.Api.Contracts;

public record UpdateUserProfileRequest(
    string? Name,
    string? Email,
    IFormFile? Photo,
    string? HomeAddressLabel,
    decimal? HomeAddressLatitude,
    decimal? HomeAddressLongitude);


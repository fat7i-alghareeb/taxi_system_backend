using Microsoft.AspNetCore.Http;

namespace Taxi.Api.Contracts;

public record UpdateUserProfileRequest(
    string? Name,
    IFormFile? Photo,
    string? HomeAddressLabel,
    decimal? HomeAddressLatitude,
    decimal? HomeAddressLongitude);


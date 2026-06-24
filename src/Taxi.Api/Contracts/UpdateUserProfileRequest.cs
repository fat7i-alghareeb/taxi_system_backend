using Microsoft.AspNetCore.Http;
using Taxi.Application.Features.Users.Commands.UpdateUserProfile;

namespace Taxi.Api.Contracts;

public record UpdateUserProfileRequest(
    string? Name,
    string? Email,
    IFormFile? Photo,
    HomeAddressUpdateMode HomeAddressOperation = HomeAddressUpdateMode.Keep,
    string? HomeAddressLabel = null,
    decimal? HomeAddressLatitude = null,
    decimal? HomeAddressLongitude = null);


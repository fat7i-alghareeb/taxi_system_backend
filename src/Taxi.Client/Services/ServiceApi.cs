using System.Net.Http.Json;
using System.Text.Json;
using Taxi.Contracts.Common;
using Taxi.Contracts.Responses.Vehicles;

namespace Taxi.Client.Services;

public class ServiceApi(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public async Task<ApiResult<List<VehicleTypeDto>>> GetVehicleCatalogAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/vehicle-types");
        return await HandleResponseAsync<List<VehicleTypeDto>>(response);
    }

    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(_options);
            return ApiResult<T>.Success(data!);
        }

        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, _options);
            return ApiResult<T>.Failure(
                problemDetails?.Title ?? "Error",
                problemDetails?.Detail ?? "An unexpected error occurred",
                (int)response.StatusCode);
        }
        catch
        {
            return ApiResult<T>.Failure("Error", "Could not parse error response", (int)response.StatusCode);
        }
    }
}

public record ApiResult<T>(T? Data, bool IsSuccess, string Message, string Detail, int StatusCode)
{
    public static ApiResult<T> Success(T data) => new(data, true, string.Empty, string.Empty, 200);
    public static ApiResult<T> Failure(string message, string detail, int statusCode) => new(default, false, message, detail, statusCode);
}


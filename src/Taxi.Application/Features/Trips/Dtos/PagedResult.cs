namespace Taxi.Application.Features.Trips.Dtos;

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

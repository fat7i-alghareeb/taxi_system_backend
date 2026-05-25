using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetCompensationClaims;

public record GetCompensationClaimsQuery(string? Status = null) : IRequest<Result<List<CompensationClaimDto>>>;

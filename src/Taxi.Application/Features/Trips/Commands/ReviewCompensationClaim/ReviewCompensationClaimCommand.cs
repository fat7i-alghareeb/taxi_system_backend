using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.ReviewCompensationClaim;

public record ReviewCompensationClaimCommand(Guid ClaimId, bool Approved, string? Notes) : IRequest<Result<CompensationClaimDto>>;

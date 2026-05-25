using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.SubmitCompensationClaim;

public record SubmitCompensationClaimCommand(Guid TripId, string Note, List<string>? EvidenceUrls) : IRequest<Result<CompensationClaimDto>>;

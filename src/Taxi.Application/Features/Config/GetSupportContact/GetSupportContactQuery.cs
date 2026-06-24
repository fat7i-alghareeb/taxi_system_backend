using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetSupportContact;

public record GetSupportContactQuery : IRequest<Result<SupportContactDto>>;

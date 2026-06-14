using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetCompanyContact;

public record GetCompanyContactQuery : IRequest<Result<CompanyContactDto>>;

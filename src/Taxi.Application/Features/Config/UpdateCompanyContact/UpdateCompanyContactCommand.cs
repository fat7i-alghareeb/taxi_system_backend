using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateCompanyContact;

public record UpdateCompanyContactCommand(string? Email, string? Phone, string? Website)
    : IRequest<Result<CompanyContactDto>>;

using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdatePreferredLanguage;

public record UpdatePreferredLanguageCommand(string LanguageCode) : IRequest<Result<Success>>;

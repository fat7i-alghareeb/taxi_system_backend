using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Phone,
    string FirebaseIdToken,
    string? FcmToken) : IRequest<Result<AuthResponse>>;

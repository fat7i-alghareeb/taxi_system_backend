using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UploadProfilePhoto;

public record UploadProfilePhotoCommand(
    Stream FileStream,
    string FileName,
    string ContentType) : IRequest<Result<string>>;


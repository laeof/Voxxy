using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Users.Upload;

public sealed record UploadUserImageCommand(Guid UserId, Stream FileStream, string ContentType) : ICommand<Guid>;

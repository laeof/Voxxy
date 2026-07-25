using Application.Abstractions.Messaging;
using Application.Tracks.Batch;

namespace Application.Users.Upload;

public sealed record UploadUserImageCommand(Guid UserId, Stream FileStream, string ContentType) : ICommand<Guid>;

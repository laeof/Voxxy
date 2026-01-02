using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Tracks.Upload;

public sealed record UploadTrackCommand(Guid TrackId, Stream FileStream, string ContentType) : ICommand<Guid>;

using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Albums.Upload;

public sealed record UploadAlbumImageCommand(Guid AlbumId, Stream FileStream, string ContentType) : ICommand<Guid>;

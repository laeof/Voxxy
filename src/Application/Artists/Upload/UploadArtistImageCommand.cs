using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Artists.Upload;

public sealed record UploadArtistImageCommand(Guid ArtistId, Stream FileStream, string ContentType) : ICommand<Guid>;

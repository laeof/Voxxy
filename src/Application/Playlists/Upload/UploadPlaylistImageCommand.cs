using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Playlists.Upload;

public sealed record UploadPlaylistImageCommand(Guid PlaylistId, Stream FileStream, string ContentType) : ICommand<Guid>;

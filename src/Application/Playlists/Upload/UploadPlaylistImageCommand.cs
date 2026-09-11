using Application.Abstractions.Messaging;
using Application.Tracks.Batch;

namespace Application.Playlists.Upload;

public sealed record UploadPlaylistImageCommand(Guid PlaylistId, Stream FileStream, string ContentType) : ICommand<Guid>;

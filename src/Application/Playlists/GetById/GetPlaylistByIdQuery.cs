using Application.Abstractions.Messaging;

namespace Application.Playlists.GetById;

public sealed record GetPlaylistByIdQuery(Guid PlaylistId) : IQuery<PlaylistResponse>;
using Application.Abstractions.Messaging;
using Application.Tracks.GetById;

namespace Application.Albums.GetTracks;

public sealed record GetAlbumTracksQuery(Guid AlbumId) : IQuery<List<TrackResponse>>;

using Application.Abstractions.Messaging;
using Application.Tracks.Batch;

namespace Application.Albums.GetTracks;

public sealed record GetAlbumTracksQuery(Guid AlbumId) : IQuery<List<TrackResponse>>;

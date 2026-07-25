using Application.Abstractions.Messaging;
using Application.Tracks.Batch;

namespace Application.Playlists.GetTracks;

public sealed record GetPlaylistTracksQuery(Guid PlaylistId) : IQuery<List<TrackResponse>>;
using Application.Abstractions.Messaging;

namespace Application.Tracks.Batch;

public sealed record BatchQuery(List<Guid> TrackIds) : IQuery<List<TrackResponse>>;
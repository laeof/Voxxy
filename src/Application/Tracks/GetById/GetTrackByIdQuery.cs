using Application.Abstractions.Messaging;

namespace Application.Tracks.GetById;

public sealed record GetTrackByIdQuery(Guid TrackId) : IQuery<TrackResponse>;
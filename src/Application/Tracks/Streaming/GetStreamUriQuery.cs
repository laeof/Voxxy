using Application.Abstractions.Messaging;

namespace Application.Tracks.Streaming;

public sealed record GetStreamUriQuery(Guid TrackId): IQuery<Uri>;
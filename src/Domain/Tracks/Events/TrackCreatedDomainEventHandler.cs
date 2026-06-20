using SharedKernel;

namespace Domain.Tracks;

public sealed record TrackCreatedDomainEvent(TrackMetaData Track, Stream AudioStream) : IDomainEvent;
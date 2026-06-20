using Domain.Artists;
using Domain.Genres;
using Domain.Moods;
using Domain.Tracks;
using SharedKernel;

namespace Domain.ArtistReleases;

public sealed record ReleaseDataCreatedDomainEvent(
    Guid ReleaseId,
    List<Guid> Artists,
    List<Guid> Genres,
    List<Guid> Moods,
    List<CreateTrackDto> Tracks,
    Stream CoverImage) : IDomainEvent;
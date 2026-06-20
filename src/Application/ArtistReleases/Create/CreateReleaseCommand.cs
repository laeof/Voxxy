using Application.Abstractions.Messaging;
using Domain.ArtistReleases.Enums;

namespace Application.ArtistReleases.Create;

public sealed record CreateReleaseCommand(
    string Title,
    string AdditionalInformation,
    string Copyright,
    string ReleaseDate,
    ReleaseType ReleaseType,
    List<Guid> Artists,
    List<Guid> Genres,
    List<Guid> Moods,
    List<CreateReleaseTrack> Tracks,
    Stream CoverImage
) : ICommand<Guid>;
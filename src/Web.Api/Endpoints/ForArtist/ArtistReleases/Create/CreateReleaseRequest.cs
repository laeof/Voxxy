using Application.ArtistReleases.Create;
using Domain.ArtistReleases.Enums;

namespace Web.Api.Endpoints.ForArtist.Releases.Create;

public sealed class CreateReleaseRequest
{
    public string Title { get; init; } = string.Empty;
    public string ReleaseDate { get; init; } = string.Empty;
    public string AdditionalInformation { get; init; } = string.Empty;
    public string Copyright { get; init; } = string.Empty;
    public ReleaseType ReleaseType { get; init; }
    public List<Guid> ArtistIds { get; init; } = [];
    public List<Guid> GenreIds { get; init; } = [];
    public List<Guid> MoodIds { get; init; } = [];
    public List<CreateReleaseTrackDto> Tracks { get; init; } = [];
    public IFormFile CoverImage { get; init; }
}
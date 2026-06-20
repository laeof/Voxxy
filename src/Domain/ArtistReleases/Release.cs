using System.Globalization;
using Domain.ArtistReleases.Enums;
using Domain.Artists;
using Domain.Genres;
using Domain.Moods;
using Domain.Playlists;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;

namespace Domain.ArtistReleases;

public sealed class Release : Entity
{
    public Guid Id { get; init; }
    public Guid? ApprovedById { get; private set; }
    public string Title { get; init; } = string.Empty;
    public string AdditionalInformation { get; init; } = string.Empty;
    public string Copyright { get; init; } = string.Empty;
    public string ImageKey { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public ReleaseType Type { get; init; }
    public DateTime ReleaseDate { get; init; }
    public ReleaseStatus Status { get; private set; }

    public List<Artist> Artists { get; init; } = [];
    public List<Track> Tracks { get; init; } = [];
    public User? ApprovedBy { get; init; }

    public static Release Create(
        string title,
        string additionalInformation,
        string copyright,
        string imageKeyAsset,
        string releaseDate,
        ReleaseType releaseType,
        List<Guid> artists,
        List<Guid> genres,
        List<Guid> moods,
        List<CreateTrackDto> tracks,
        Stream coverImageStream)
    {
        var releaseId = Guid.NewGuid();

        var release = new Release
        {
            Id = releaseId,
            Title = title,
            AdditionalInformation = additionalInformation,
            Copyright = copyright,
            ReleaseDate = DateTime.ParseExact(
                releaseDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            ImageKey = imageKeyAsset.Replace("{id}", releaseId.ToString()),
            Status = ReleaseStatus.Pending,
            Type = releaseType,
        };

        release.Raise(new ReleaseDataCreatedDomainEvent(release.Id, artists, genres, moods, tracks, coverImageStream));

        return release;
    }

    public void Approve(Guid approverId)
    {
        ApprovedById = approverId;
        Status = ReleaseStatus.Ready;
    }

    public void MarkPublished()
    {
        Status = ReleaseStatus.Published;
    }

    public void MarkFailed()
    {
        Status = ReleaseStatus.Failed;
    }
}
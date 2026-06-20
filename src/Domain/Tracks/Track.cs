using Domain.ArtistReleases;
using Domain.Artists;
using Domain.Genres;
using Domain.Moods;
using Domain.Playlists;
using Domain.Tracks.Enums;
using SharedKernel;

namespace Domain.Tracks;

public sealed class Track : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public Guid ReleaseId { get; set; }
    public string ImageKey { get; set; }
    public string AudioKey { get; set; }
    public int AlbumOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public TrackStatus Status { get; set; }

    // Navigation properties
    public Release Release { get; set; }
    public List<Artist> Artists { get; set; } = new();
    public List<Playlist> Playlists { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();
    public List<Mood> Moods { get; set; } = new();

    public static Track CreateTrackRelease(TrackMetaData trackMetaData,
        string audioKey,
        string imageKey,
        Stream audioFileStream)
    {
        var track = new Track
        {
            Id = trackMetaData.Id,
            Name = trackMetaData.Name,
            ReleaseId = trackMetaData.AlbumId,
            AlbumOrder = trackMetaData.AlbumOrder,
            AudioKey = audioKey.Replace("{id}", trackMetaData.Id.ToString()),
            ImageKey = imageKey.Replace("{id}", trackMetaData.AlbumId.ToString()),
            CreatedAt = trackMetaData.CreatedAt,
            Artists = trackMetaData.Artists,
            Genres = trackMetaData.Genres,
            Moods = trackMetaData.Moods,
            Duration = trackMetaData.Duration,
            Status = TrackStatus.Pending
        };

        track.Raise(new TrackCreatedDomainEvent(trackMetaData, audioFileStream));

        return track;
    }

    public void MarkReady()
    {
        Status = TrackStatus.Ready;
    }

    public void MarkPublished()
    {
        Status = TrackStatus.Published;
    }

    public void MarkFailed()
    {
        Status = TrackStatus.Failed;
    }
}
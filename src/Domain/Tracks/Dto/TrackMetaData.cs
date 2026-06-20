using Domain.Artists;
using Domain.Genres;
using Domain.Moods;

namespace Domain.Tracks;

public sealed class TrackMetaData
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public Guid AlbumId { get; set; }
    public List<Artist> Artists { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();
    public List<Mood> Moods { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int AlbumOrder { get; set; }

    public static TrackMetaData Create(string name, Guid albumId, List<Artist> artists, List<Genre> genres, List<Mood> moods, double duration, int albumOrder, DateTime createdAt)
    {
        return new TrackMetaData
        {
            Id = Guid.NewGuid(),
            Name = name,
            Duration = (int)Math.Floor(duration),
            AlbumId = albumId,
            Artists = artists,
            Genres = genres,
            Moods = moods,
            AlbumOrder = albumOrder,
            CreatedAt = createdAt
        };
    }
}
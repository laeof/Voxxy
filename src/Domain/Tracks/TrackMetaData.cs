namespace Domain.Tracks;

public sealed class TrackMetaData
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public Guid AlbumId { get; set; }
    public Guid ArtistId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AlbumOrder { get; set; }

    public static TrackMetaData Create(Guid id, string name, Guid albumId, Guid artistId, int albumOrder, DateTime createdAt)
    {
        return new TrackMetaData
        {
            Id = id,
            Name = name,
            AlbumId = albumId,
            ArtistId = artistId,
            AlbumOrder = albumOrder,
            CreatedAt = createdAt
        };
    }
}
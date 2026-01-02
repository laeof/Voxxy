using Domain.Albums;
using Domain.Artists;
using Domain.Playlists;
using SharedKernel;

namespace Domain.Tracks;

public sealed class Track : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public Guid AlbumId { get; set; }
    public Guid ArtistId { get; set; }
    public string ImageUrl { get; set; }
    public string AudioKey { get; set; }
    public int AlbumOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Album Album { get; set; }
    public Artist Artist { get; set; }
    public List<Playlist> Playlists { get; set; } = new();
}
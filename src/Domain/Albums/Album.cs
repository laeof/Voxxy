using Domain.Artists;
using Domain.Playlists;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;

namespace Domain.Albums;

public sealed class Album : Entity, IPlaylist
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int Type { get; set; }
    public string Color { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public List<Track> Tracks { get; set; }
    public User CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; }
}
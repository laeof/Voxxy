using Application.Albums.GetById;
using Application.Tracks.GetById;

namespace Application.Artists.GetById;

public sealed class ArtistResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TrackResponse> Tracks { get; set; } = new();
    public List<AlbumResponse> Albums { get; set; } = new();
}
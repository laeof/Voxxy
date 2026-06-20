using Application.Albums.GetById;
using Application.Artists.GetById;
using Domain.Artists;

namespace Application.Tracks.GetById;

public sealed class TrackResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public string ImageUrl { get; set; }
    public string AudioKey { get; set; }
    public int AlbumOrder { get; set; }
    public AlbumResponse Album { get; set; }
    public List<ArtistResponse> Artists { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? FromPlaylist { get; set; }
}
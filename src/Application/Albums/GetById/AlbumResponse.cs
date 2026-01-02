using Application.Artists.GetById;
using Application.Tracks.GetById;
using SharedKernel.Enums;

namespace Application.Albums.GetById;

public sealed class AlbumResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string PrimaryColor { get; set; }
    public PlaylistType PlaylistType { get; set; }
    public ArtistResponse CreatedBy { get; set; }
    public List<TrackResponse> Tracks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
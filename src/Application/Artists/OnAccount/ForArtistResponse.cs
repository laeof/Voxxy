using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Tracks.GetById;

namespace Application.Artists.OnAccount;

public sealed class ForArtistResponse
{
    public List<ArtistResponse> Artists { get; set; } = new();
}
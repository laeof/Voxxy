using Application.Artists.GetById;
using Application.Tracks.GetById;
using SharedKernel.Enums;

namespace Application.Followees.Get;

public sealed class FollowResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ArtistResponse Artist { get; set; }
    public FollowType FollowType { get; set; }
    public string PrimaryColor { get; set; }
    public List<TrackResponse> Tracks { get; set; } = new();
    public string ImageUrl { get; set; }
    public DateTime FollowedSince { get; set; }
}

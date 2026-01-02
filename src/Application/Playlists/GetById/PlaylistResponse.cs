using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Artists;
using Domain.Playlists;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;
using SharedKernel.Enums;

namespace Application.Playlists.GetById;

public sealed class PlaylistResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string PrimaryColor { get; set; }
    public string ImageUrl { get; set; }
    public PlaylistType PlaylistType { get; set; }
    public UserResponse CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public List<TrackResponse> Tracks { get; set; } = [];
}
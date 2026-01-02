using Domain.Tracks;
using Domain.Users;

namespace Domain.Playlists;

public interface IPlaylist
{
    Guid Id { get; set; }
    string Name { get; set; }
    int Type { get; set; }
    string Color { get; set; }
    string ImageUrl { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
    List<Track> Tracks { get; set; }
    User CreatedByUser { get; set; }
}
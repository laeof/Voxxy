using Domain.Artists;
using Domain.Follows;
using Domain.Tracks;
using Domain.Users;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Domain.Playlists;

public sealed class Playlist : Entity, IPlaylist
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Type { get; set; }
    public string Color { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public List<Track> Tracks { get; set; } = new();
    public User CreatedByUser { get; set; }

    public static Playlist CreateDefault(
        Guid userId,
        IDateTimeProvider dateTimeProvider)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            CreatedBy = userId,
            Name = PlaylistConstants.DefaultLovedSongsPlaylistName,
            Color = PlaylistConstants.DefaultPlaylistColor,
            CreatedAt = dateTimeProvider.UtcNow,
            ImageUrl = PlaylistConstants.DefaultPlaylistImageUrl,
            Type = (int)PlaylistType.LovedSongs,
        };

        playlist.Raise(new PlaylistCreatedDomainEvent(playlist.Id, userId, (PlaylistType)playlist.Type));

        return playlist;
    }
}
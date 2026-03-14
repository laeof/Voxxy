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
    public string ImageKey { get; set; }
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
        var id = Guid.NewGuid();

        Playlist playlist = Create(id, userId, PlaylistConstants.DefaultLovedSongsPlaylistName, dateTimeProvider, $"{id}/cover.jpg", (int)PlaylistType.LovedSongs);

        return playlist;
    }

    public static Playlist Create(Guid playlistId, Guid userId, string name, IDateTimeProvider dateTimeProvider, string imageKey, int type)
    {
        var playlist = new Playlist
        {
            Id = playlistId,
            CreatedBy = userId,
            Name = name,
            CreatedAt = dateTimeProvider.UtcNow,
            Color = PlaylistConstants.DefaultPlaylistColor,
            ImageKey = imageKey,
            Type = type,
        };

        playlist.Raise(new PlaylistCreatedDomainEvent(playlist.Id, userId, (PlaylistType)playlist.Type));

        return playlist;
    }
}
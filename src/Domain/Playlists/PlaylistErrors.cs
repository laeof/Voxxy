using SharedKernel;

namespace Domain.Playlists;

public static class PlaylistErrors
{
    public static Error NotFound(Guid playlistId) => Error.NotFound(
        "Playlists.NotFound",
        $"The playlist with the Id = '{playlistId}' was not found");
}

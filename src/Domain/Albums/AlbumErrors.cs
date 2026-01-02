using SharedKernel;

namespace Domain.Albums;

public static class AlbumErrors
{
    public static Error NotFound(Guid albumId) => Error.NotFound(
        "Albums.NotFound",
        $"The album with the Id = '{albumId}' was not found");
}

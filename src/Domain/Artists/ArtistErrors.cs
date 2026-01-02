using SharedKernel;

namespace Domain.Artists;

public static class ArtistErrors
{
    public static Error NotFound(Guid artistId) => Error.NotFound(
        "Artists.NotFound",
        $"The artist with the Id = '{artistId}' was not found");
}

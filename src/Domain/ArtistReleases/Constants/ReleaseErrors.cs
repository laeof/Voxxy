using SharedKernel;

namespace Domain.ArtistReleases.Constants;

public static class ReleaseErrors
{
    public static Error NotFound(Guid releaseId) => Error.NotFound(
        "Releases.NotFound",
        $"The release with the Id = '{releaseId}' was not found");
}

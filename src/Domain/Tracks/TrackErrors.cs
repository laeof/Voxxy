using SharedKernel;

namespace Domain.Tracks;

public static class TrackErrors
{
    public static Error NotFound(Guid trackId) => Error.NotFound(
        "Tracks.NotFound",
        $"The track with the Id = '{trackId}' was not found");
}
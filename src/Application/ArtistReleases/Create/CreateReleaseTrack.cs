namespace Application.ArtistReleases.Create;

public sealed record CreateReleaseTrack(
    string Title,
    int Position,
    double Duration,
    bool IsRemix,
    Stream AudioFile
);
namespace Domain.Tracks;

public sealed record CreateTrackDto(
    string Title,
    string AudioKeyAsset,
    string ImageKeyAsset,
    int Position,
    double Duration,
    bool IsRemix,
    Stream AudioStream);
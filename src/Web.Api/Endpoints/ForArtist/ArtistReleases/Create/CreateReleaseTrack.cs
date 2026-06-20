namespace Application.ArtistReleases.Create;

public sealed class CreateReleaseTrackDto
{
    public string Title { get; init; }
    public int Position { get; init; }
    public double Duration { get; init; }
    public bool IsRemix { get; init; }
    public IFormFile AudioFile { get; init; }
}
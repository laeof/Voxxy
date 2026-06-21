using SharedKernel.Enums;

namespace SharedKernel.GlobalSearch;

public sealed class SearchDocument
{
    public Guid Id { get; init; }
    public SearchEntityType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<SearchArtist> Artists { get; init; } = [];
    public string? ArtistDisplayName { get; init; }

    public Guid? ReleaseId { get; init; }
    public string? ReleaseTitle { get; init; }

    public string? Lyrics { get; init; }
    public string? ImageUrl { get; init; }
}
using SharedKernel.Enums;
using SharedKernel.GlobalSearch;

namespace Application.GlobalSearch.Search;

public sealed record SearchResponse(
    Guid EntityId,
    SearchEntityType Type,
    string Title,
    IEnumerable<SearchArtist>? Artists,
    string? ReleaseTitle,
    Uri? ImageUrl);
using Application.Abstractions.Messaging;
using Meilisearch;
using SharedKernel;
using SharedKernel.GlobalSearch;
using Index = Meilisearch.Index;

namespace Application.GlobalSearch.Search;

internal sealed class GlobalSearchQueryHandler(MeilisearchClient client) : IQueryHandler<GlobalSearchQuery, List<SearchResponse>>
{
    public async Task<SharedKernel.Result<List<SearchResponse>>> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        Index index = client.Index("global_search");

        ISearchable<SearchDocument> result = await index.SearchAsync<SearchDocument>(
            request.Query,
            new SearchQuery
            {
                Limit = request.Limit,
            },
            cancellationToken);

        var response = result.Hits.Select(x => new SearchResponse(
            x.Id,
            x.Type,
            x.Title,
            x.Artists,
            x.ReleaseTitle,
            new Uri(x.ImageUrl!)))
            .OrderBy(x => x.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        return Result.Success(response);
    }
}
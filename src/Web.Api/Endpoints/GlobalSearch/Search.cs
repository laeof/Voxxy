using System.Runtime.CompilerServices;
using Application.Abstractions.Messaging;
using Application.GlobalSearch.Search;
using SharedKernel;

namespace Web.Api.Endpoints.GlobalSearch;

internal sealed class Search : IEndpoint
{
    public sealed record SearchRequest(string? Search, int Limit = 20);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("search", async (
            [AsParameters] SearchRequest query,
            IQueryHandler<GlobalSearchQuery, List<SearchResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query.Search))
            {
                return Results.Ok(Array.Empty<SearchResponse>());
            }

            var globalSearchQuery = new GlobalSearchQuery(query.Search, query.Limit);

            Result<List<SearchResponse>> searchResult = await handler.Handle(globalSearchQuery, cancellationToken);

            return Results.Ok(searchResult);
        })
        .WithTags(Tags.Search)
        .RequireAuthorization();
    }
}
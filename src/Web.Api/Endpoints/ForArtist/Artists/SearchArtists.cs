using Application.Abstractions.Messaging;
using Application.Artists.Search;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ForArtist.Artists;

internal sealed class SearchArtists : IEndpoint
{
    public sealed record SearchArtistsRequest(string? Search, int Limit = 20);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("for-artist/artists", async (
            [AsParameters] SearchArtistsRequest request,
            IQueryHandler<SearchArtistQuery, List<SearchArtistResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchArtistQuery(
                request.Search,
                request.Limit);

            Result<List<SearchArtistResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ForArtistArtists)
        .RequireAuthorization();
    }
}
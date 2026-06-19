using Application.Abstractions.Messaging;
using Application.Genres.Search;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ForArtist.Genres;

internal sealed class SearchGenres : IEndpoint
{
    public sealed record SearchGenresRequest(string? Search, int Limit = 20);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("for-artist/genres", async (
            [AsParameters] SearchGenresRequest request,
            IQueryHandler<SearchGenreQuery, List<SearchGenreResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchGenreQuery(
                request.Search,
                request.Limit);

            Result<List<SearchGenreResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ForArtistGenres)
        .RequireAuthorization();
    }
}
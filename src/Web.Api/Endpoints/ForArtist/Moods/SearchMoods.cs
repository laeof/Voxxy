using Application.Abstractions.Messaging;
using Application.Moods.Search;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ForArtist.Moods;

internal sealed class SearchMoods : IEndpoint
{
    public sealed record SearchMoodsRequest(string? Search, int Limit = 20);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("for-artist/moods", async (
            [AsParameters] SearchMoodsRequest request,
            IQueryHandler<SearchMoodQuery, List<SearchMoodResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchMoodQuery(
                request.Search,
                request.Limit);

            Result<List<SearchMoodResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ForArtistMoods)
        .RequireAuthorization();
    }
}
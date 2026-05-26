using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using SharedKernel;
using Web.Api.Endpoints;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Artists;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("artists/{id:guid}", async (
            Guid id,
            IQueryHandler<GetArtistByIdQuery, ArtistResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetArtistByIdQuery(id);

            Result<ArtistResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Artists);
    }
}
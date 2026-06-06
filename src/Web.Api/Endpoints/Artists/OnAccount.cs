using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Artists.OnAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Artists.OnAccount;

internal sealed class OnAccount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("artists/on-account", async (
            IUserContext userContext,
            IQueryHandler<GetArtistOnAccountQuery, ForArtistResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetArtistOnAccountQuery(userContext.UserId);

            Result<ForArtistResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Artists)
        .RequireAuthorization();
    }
}
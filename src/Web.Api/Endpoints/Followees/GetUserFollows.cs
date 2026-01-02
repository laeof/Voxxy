using Application.Abstractions.Messaging;
using Application.Followees.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Followees;

internal sealed class GetUserFollows : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("followees", async (
            IQueryHandler<GetUserFollowsQuery, List<FollowResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<List<FollowResponse>> result =
                await handler.Handle(new GetUserFollowsQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Followings)
        .RequireAuthorization();
    }
}
using Application.Abstractions.Messaging;
using Application.Followees.Unfollow;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Folowees;

internal sealed class Unfollow : IEndpoint
{
    public sealed record UnfollowRequest(Guid FollowerId, Guid FolloweeId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("followees", async (
            [FromBody] UnfollowRequest request,
            ICommandHandler<UnfollowCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UnfollowCommand(request.FollowerId, request.FolloweeId);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Followings)
        .RequireAuthorization();
    }
}
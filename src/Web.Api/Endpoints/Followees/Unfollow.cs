using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Followees.Unfollow;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Folowees;

internal sealed class Unfollow : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("followees/{followeeId}", async (
            Guid followeeId,
            IUserContext userContext,
            ICommandHandler<UnfollowCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UnfollowCommand(userContext.UserId, followeeId);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Followings)
        .RequireAuthorization();
    }
}
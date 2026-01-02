using Application.Abstractions.Messaging;
using Application.Followees.Follow;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using SharedKernel.Enums;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Followees;

internal sealed class Follow : IEndpoint
{
    public sealed record FollowRequest(Guid FollowerId, Guid FolloweeId, FollowType Type);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("followees", async (
            [FromBody] FollowRequest request,
            ICommandHandler<FollowCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new FollowCommand(request.FollowerId, request.FolloweeId, request.Type);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Followings)
        .RequireAuthorization();
    }
}
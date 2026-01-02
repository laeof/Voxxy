using Application.Abstractions.Messaging;
using Application.Artists.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Artists;

internal sealed class Create : IEndpoint
{
    public sealed record CreateArtistRequest(Guid UserId, string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("artists", async (
            [FromBody] CreateArtistRequest request,
            ICommandHandler<CreateArtistCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateArtistCommand(request.UserId, request.Name);
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Artists)
        .RequireAuthorization();
    }
}
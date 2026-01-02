using Application.Abstractions.Messaging;
using Application.Tracks.Streaming;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tracks;

internal sealed class Stream : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tracks/{id:guid}/stream", async (
            Guid id,
            IQueryHandler<GetStreamUriQuery, Uri> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetStreamUriQuery(id);

            Result<Uri> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tracks)
        .RequireAuthorization();
    }
}
using Application.Abstractions.Messaging;
using Application.Tracks.GetById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tracks;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tracks/{id:guid}", async (
            Guid id,
            IQueryHandler<GetTrackByIdQuery, TrackResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetTrackByIdQuery(id);

            Result<TrackResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tracks)
        .RequireAuthorization();
    }
}
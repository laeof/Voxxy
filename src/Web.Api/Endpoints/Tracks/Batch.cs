using Application.Abstractions.Messaging;
using Application.Tracks.Batch;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tracks;

internal sealed class Batch : IEndpoint
{
    public sealed record TrackBatchRequest(List<Guid> TrackIds);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("tracks/batch", async (
            TrackBatchRequest request,
            IQueryHandler<BatchQuery, List<TrackResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new BatchQuery(request.TrackIds);

            Result<List<TrackResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tracks)
        .RequireAuthorization();
    }
}
using Application.Abstractions.Messaging;
using Application.GlobalSearch.Backfill;

namespace Web.Api.Endpoints.Admin.Search;

internal sealed class Backfill : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/backfill", async (
            ICommandHandler<BackfillCommand> handler,
            CancellationToken cancellationToken) =>
            {
                var backfillCommand = new BackfillCommand();

                await handler.Handle(backfillCommand, cancellationToken);

                return Results.Ok();
            }
        )
        .WithTags(Tags.AdminSearchBackfill)
        .RequireAuthorization();
    }
}
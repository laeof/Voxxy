using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Genres.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Admin.Genres.Create;

internal sealed class Create : IEndpoint
{
    public sealed class CreateGenreRequest
    {
        public string Title { get; init; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("admin/genres", async (
            CreateGenreRequest request,
            ICommandHandler<CreateGenreCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateGenreCommand(request.Title);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AdminGenres)
        .RequireAuthorization();
    }
}
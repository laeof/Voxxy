using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Moods.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Admin.Moods.Create;

internal sealed class Create : IEndpoint
{
    public sealed class CreateMoodRequest
    {
        public string Title { get; init; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("admin/moods", async (
            CreateMoodRequest request,
            ICommandHandler<CreateMoodCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateMoodCommand(request.Title);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AdminMoods)
        .RequireAuthorization();
    }
}
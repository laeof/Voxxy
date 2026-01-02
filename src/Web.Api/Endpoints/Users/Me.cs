using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users.GetById;
using Application.Users.Me;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Me : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me", (IUserContext context) =>
            Results.Ok(new MeResponse
            {
                Id = context.UserId,
                Email = context.Email,
                FullName = context.FullName,
                ImageUrl = context.ImageUrl,
                UserClaims = context.UserClaims,
            })
        )
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}

using Application.Abstractions.Messaging;
using Application.Users.Login;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Factories;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    public sealed record Request(string Email, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login", async (
            Request request,
            ICommandHandler<LoginUserCommand, LoginResponse> handler,
            HttpContext httpContext,
            CookieOptionsFactory cookieOptionsFactory,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginUserCommand(request.Email, request.Password);

            Result<LoginResponse> result = await handler.Handle(command, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            return httpContext.OkWithAuthCookies(
                result.Value.Me,
                result.Value.AccessToken,
                result.Value.RefreshToken,
                cookieOptionsFactory.AccessToken(),
                cookieOptionsFactory.RefreshToken()
            );
        })
        .WithTags(Tags.Users);
    }
}

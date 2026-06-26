using Microsoft.AspNetCore.Routing;
using Presentation.Endpoints;

namespace Connect.Presentation.Endpoints;

public static class ConnectEndpoints
{
    public static IEndpointRouteBuilder MapConnectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSetPlayerStateEndpoint();

        return app;
    }
}
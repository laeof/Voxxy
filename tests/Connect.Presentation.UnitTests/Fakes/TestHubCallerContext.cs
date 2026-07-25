using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestHubCallerContext(
    string? userIdentifier,
    string connectionId = "server-connection",
    string? origin = null)
    : HubCallerContext
{
    private readonly CancellationToken _connectionAborted = CancellationToken.None;

    public override string ConnectionId { get; } = connectionId;
    public override string? UserIdentifier { get; } = userIdentifier;
    public override ClaimsPrincipal? User { get; } = userIdentifier is null
        ? null
        : new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdentifier)],
                "test"));
    public override IDictionary<object, object?> Items { get; } =
        new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = CreateFeatures(origin);
    public override CancellationToken ConnectionAborted => _connectionAborted;

    public override void Abort()
    {
    }

    private static FeatureCollection CreateFeatures(string? origin)
    {
        var features = new FeatureCollection();
        var context = new DefaultHttpContext();
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }
        features.Set<IHttpContextFeature>(new TestHttpContextFeature(context));
        return features;
    }

    private sealed class TestHttpContextFeature(HttpContext context) : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; } = context;
    }
}

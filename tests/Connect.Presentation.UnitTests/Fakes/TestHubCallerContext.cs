using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestHubCallerContext(
    string? userIdentifier,
    string connectionId = "server-connection")
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
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => _connectionAborted;

    public override void Abort()
    {
    }
}

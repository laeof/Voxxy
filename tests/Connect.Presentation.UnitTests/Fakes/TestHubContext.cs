using Connect.Presentation.Hubs;
using Connect.Presentation.Transport;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestConnectHubClient : IConnectHubClient
{
    public object? LastEvent { get; private set; }

    public Task PlayerStateChanged(PlayerStateChangedEvent message) =>
        Capture(message);

    public Task QueueStateChanged(QueueStateChangedEvent message) =>
        Capture(message);

    public Task PresenceStateChanged(PresenceStateChangedEvent message) =>
        Capture(message);

    public Task PlayerQueueStateChanged(PlayerQueueStateChangedEvent message) =>
        Capture(message);

    public Task PlayerPresenceStateChanged(PlayerPresenceStateChangedEvent message) =>
        Capture(message);

    private Task Capture(object message)
    {
        LastEvent = message;
        return Task.CompletedTask;
    }
}

internal sealed class TestHubClients(TestConnectHubClient client)
    : IHubClients<IConnectHubClient>
{
    public IConnectHubClient All => client;
    public IConnectHubClient AllExcept(IReadOnlyList<string> excludedConnectionIds) =>
        client;
    public IConnectHubClient Client(string connectionId) => client;
    public IConnectHubClient Clients(IReadOnlyList<string> connectionIds) => client;
    public IConnectHubClient Group(string groupName) => client;
    public IConnectHubClient GroupExcept(
        string groupName,
        IReadOnlyList<string> excludedConnectionIds) =>
        client;
    public IConnectHubClient Groups(IReadOnlyList<string> groupNames) => client;
    public IConnectHubClient User(string userId) => client;
    public IConnectHubClient Users(IReadOnlyList<string> userIds) => client;
}

internal sealed class TestGroupManager : IGroupManager
{
    public string? AddedConnectionId { get; private set; }
    public string? AddedGroupName { get; private set; }

    public Task AddToGroupAsync(
        string connectionId,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        AddedConnectionId = connectionId;
        AddedGroupName = groupName;
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(
        string connectionId,
        string groupName,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class TestHubContext(TestConnectHubClient client)
    : IHubContext<PlayerHub, IConnectHubClient>
{
    public IHubClients<IConnectHubClient> Clients { get; } = new TestHubClients(client);
    public IGroupManager Groups { get; } = new TestGroupManager();
}

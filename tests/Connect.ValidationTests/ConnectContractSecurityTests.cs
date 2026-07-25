using System.Reflection;
using Connect.Presentation.Transport;

namespace Connect.ValidationTests;

public sealed class ConnectContractSecurityTests
{
    private static readonly string[] ServerOwnedProperties =
    [
        "UserId",
        "ConnectionId",
        "AudioOwnerConnectionId",
        "ServerTime"
    ];

    [Fact]
    public void SpoofedConnectionIdentity_IsImpossible()
    {
        Type[] requests =
        [
            typeof(RegisterConnectionRequest),
            typeof(CommandRequest),
            typeof(SelectActiveDeviceRequest),
            typeof(ChangePositionRequest),
            typeof(ChangeVolumeRequest),
            typeof(AddQueueItemRequest),
            typeof(QueueItemRequest),
            typeof(MoveQueueItemRequest),
            typeof(ChangeRepeatModeRequest)
        ];

        requests
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Select(property => property.Name)
            .ShouldNotContain(name => ServerOwnedProperties.Contains(name, StringComparer.Ordinal));
    }
}

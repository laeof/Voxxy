using Connect.Application.Results;
using Connect.Presentation.Broadcasting;

namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestBroadcaster : IConnectBroadcaster
{
    public int Calls { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? CommandId { get; private set; }
    public ConnectApplicationResult? Result { get; private set; }
    public Exception? Exception { get; set; }

    public Task BroadcastAsync(
        Guid userId,
        Guid commandId,
        ConnectApplicationResult result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is not null)
        {
            throw Exception;
        }
        Calls++;
        UserId = userId;
        CommandId = commandId;
        Result = result;
        return Task.CompletedTask;
    }
}

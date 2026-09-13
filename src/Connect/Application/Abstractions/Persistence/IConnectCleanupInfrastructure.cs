namespace Connect.Application.Abstractions.Persistence;

public sealed class ConnectInfrastructureUnavailableException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);

public interface IConnectSessionDiscovery
{
    IAsyncEnumerable<Guid> GetCandidateUsersAsync(
        CancellationToken cancellationToken = default);

    Task RemoveCandidateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IConnectCleanupLease
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

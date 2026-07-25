namespace Connect.Application.Abstractions.Persistence;

public enum PersistenceStatus
{
    Success,
    Applied,
    Duplicate,
    VersionConflict,
    CommandCollision,
    CorruptState,
    Unavailable,
    ConnectionNotFound
}

public sealed record ConnectVersions(
    long? Player,
    long? Queue,
    long? Presence);

public sealed record PersistenceCommitResult(
    PersistenceStatus Status,
    string? Outcome,
    ConnectVersions Versions)
{
    public bool IsApplied => Status == PersistenceStatus.Applied;
}

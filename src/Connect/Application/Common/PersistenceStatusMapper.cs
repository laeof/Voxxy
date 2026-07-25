using Connect.Application.Abstractions.Persistence;
using Connect.Application.Results;

namespace Connect.Application.Common;

internal static class PersistenceStatusMapper
{
    public static ConnectCommandStatus ToCommandStatus(PersistenceStatus status) =>
        status switch
        {
            PersistenceStatus.Applied => ConnectCommandStatus.Applied,
            PersistenceStatus.Duplicate => ConnectCommandStatus.Duplicate,
            PersistenceStatus.VersionConflict => ConnectCommandStatus.Conflict,
            PersistenceStatus.CommandCollision => ConnectCommandStatus.CommandCollision,
            PersistenceStatus.CorruptState => ConnectCommandStatus.CorruptState,
            PersistenceStatus.Unavailable => ConnectCommandStatus.Unavailable,
            PersistenceStatus.ConnectionNotFound => ConnectCommandStatus.ConnectionNotFound,
            _ => throw new InvalidOperationException(
                $"Persistence status '{status}' cannot be mapped to a command result.")
        };

    public static ConnectApplicationResult FromReadFailure(
        PersistenceStatus status,
        string? error) =>
        new(ToCommandStatus(status), Error: error);
}

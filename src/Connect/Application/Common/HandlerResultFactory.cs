using Connect.Application.Abstractions.Persistence;
using Connect.Application.Results;
using Connect.Contracts.States;

namespace Connect.Application.Common;

internal static class HandlerResultFactory
{
    public static ConnectApplicationResult ValidationFailed(string error) =>
        new(ConnectCommandStatus.ValidationFailed, Error: error);

    public static ConnectApplicationResult FromCommit(
        PersistenceCommitResult commit,
        ConnectCommandStatus appliedStatus,
        ConnectSnapshot? snapshot = null,
        PlayerStateDto? player = null,
        QueueStateDto? queue = null,
        PresenceStateDto? presence = null,
        ConnectCommandOutcome? outcome = null) =>
        new(
            commit.Status == PersistenceStatus.Applied
                ? appliedStatus
                : PersistenceStatusMapper.ToCommandStatus(commit.Status),
            snapshot,
            player,
            queue,
            presence,
            commit.Status == PersistenceStatus.Duplicate
                ? CommandOutcomeSerializer.Deserialize(commit.Outcome)
                : outcome,
            commit.Status is PersistenceStatus.CorruptState or PersistenceStatus.Unavailable
                ? $"Persistence operation failed with status '{commit.Status}'."
                : null);
}

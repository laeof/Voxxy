using Connect.Application.Commands;
using Connect.Domain.Queue;

namespace Connect.Application.Common;

internal static class CommandValidation
{
    public const int MaximumDeviceNameLength = 100;

    public static string? Validate(RegisterConnectionCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return "User ID is required.";
        }

        if (command.CommandId == Guid.Empty)
        {
            return "Command ID is required.";
        }

        if (command.DeviceId == Guid.Empty)
        {
            return "Device ID is required.";
        }

        if (string.IsNullOrWhiteSpace(command.DeviceName))
        {
            return "Device name is required.";
        }
        if (command.DeviceName.Length > MaximumDeviceNameLength)
        {
            return $"Device name cannot exceed {MaximumDeviceNameLength} characters.";
        }
        if (command.DeviceName.Any(char.IsControl))
        {
            return "Device name cannot contain control characters.";
        }

        return string.IsNullOrWhiteSpace(command.ConnectionId)
            ? "Connection ID is required."
            : null;
    }

    public static string? Validate(RefreshConnectionLeaseCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return "User ID is required.";
        }

        return string.IsNullOrWhiteSpace(command.ConnectionId)
            ? "Connection ID is required."
            : null;
    }

    public static string? Validate(DisconnectConnectionCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        return common ?? (string.IsNullOrWhiteSpace(command.ConnectionId)
            ? "Connection ID is required."
            : null);
    }

    public static string? Validate(ExpireConnectionsCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        if (common is not null)
        {
            return common;
        }

        if (command.ConnectionIds is null)
        {
            return "Connection IDs are required.";
        }

        return command.ConnectionIds.Any(string.IsNullOrWhiteSpace)
            ? "Connection IDs cannot contain empty values."
            : null;
    }

    public static string? Validate(DetectMissingLeasesCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(SelectActiveDeviceCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        if (common is not null)
        {
            return common;
        }

        if (command.DeviceId == Guid.Empty)
        {
            return "Device ID is required.";
        }

        return command.ConnectionId is not null &&
            string.IsNullOrWhiteSpace(command.ConnectionId)
            ? "Connection ID cannot be empty."
            : null;
    }

    public static string? Validate(PlayCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(PauseCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(ChangePositionCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        return common ?? (command.PositionMs < 0 ? "Position cannot be negative." : null);
    }

    public static string? Validate(ChangeVolumeCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(AddQueueItemCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        if (common is not null)
        {
            return common;
        }
        if (command.QueueItemId == Guid.Empty)
        {
            return "Queue item ID is required.";
        }

        return command.TrackId == Guid.Empty ? "Track ID is required." : null;
    }

    public static string? Validate(RemoveQueueItemCommand command) =>
        ValidateQueueItemCommand(command.UserId, command.CommandId, command.QueueItemId);

    public static string? Validate(MoveQueueItemCommand command)
    {
        string? common = ValidateQueueItemCommand(
            command.UserId,
            command.CommandId,
            command.QueueItemId);
        return common ?? (command.TargetIndex < 0 ? "Target index cannot be negative." : null);
    }

    public static string? Validate(SelectQueueItemCommand command) =>
        ValidateQueueItemCommand(command.UserId, command.CommandId, command.QueueItemId);

    public static string? Validate(ShuffleQueueCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(UnshuffleQueueCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(ChangeRepeatModeCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        return common ?? (!Enum.IsDefined(command.RepeatMode)
            ? $"Repeat mode '{command.RepeatMode}' is not defined."
            : null);
    }

    public static string? Validate(NextQueueItemCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    public static string? Validate(CompleteCurrentTrackCommand command)
    {
        string? common = ValidateQueueItemCommand(
            command.UserId,
            command.CommandId,
            command.ExpectedQueueItemId);
        return common ?? (command.CompletedPositionMs < 0
            ? "Completed position cannot be negative."
            : null);
    }

    public static string? Validate(StartPlaybackContextCommand command)
    {
        string? common = ValidateCommand(command.UserId, command.CommandId);
        if (common is not null)
        {
            return common;
        }
        if (command.SourceId == Guid.Empty)
        {
            return "Playback source ID is required.";
        }
        if (!Enum.IsDefined(command.SourceType))
        {
            return $"Playback source type '{command.SourceType}' is not defined.";
        }
        if (command.Items is null || command.Items.Count == 0)
        {
            return "Playback source cannot be empty.";
        }
        if (command.Items.Count > QueueState.MaximumItemCount)
        {
            return $"Playback source cannot contain more than {QueueState.MaximumItemCount} items.";
        }
        if (command.Items.Any(item =>
                item.QueueItemId == Guid.Empty || item.TrackId == Guid.Empty))
        {
            return "Queue and track IDs are required.";
        }
        if (command.Items.Select(item => item.QueueItemId).Distinct().Count() !=
            command.Items.Count)
        {
            return "Queue item IDs must be unique.";
        }
        return command.StartIndex is < 0 ||
            command.StartIndex >= command.Items.Count
                ? "Start index must identify an item in the playback source."
                : null;
    }

    public static string? Validate(PreviousQueueItemCommand command) =>
        ValidateCommand(command.UserId, command.CommandId);

    private static string? ValidateCommand(Guid userId, Guid commandId)
    {
        if (userId == Guid.Empty)
        {
            return "User ID is required.";
        }

        return commandId == Guid.Empty ? "Command ID is required." : null;
    }

    private static string? ValidateQueueItemCommand(
        Guid userId,
        Guid commandId,
        Guid queueItemId)
    {
        string? common = ValidateCommand(userId, commandId);
        return common ?? (queueItemId == Guid.Empty ? "Queue item ID is required." : null);
    }
}

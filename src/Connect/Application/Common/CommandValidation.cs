using Connect.Application.Commands;

namespace Connect.Application.Common;

internal static class CommandValidation
{
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

    private static string? ValidateCommand(Guid userId, Guid commandId)
    {
        if (userId == Guid.Empty)
        {
            return "User ID is required.";
        }

        return commandId == Guid.Empty ? "Command ID is required." : null;
    }
}

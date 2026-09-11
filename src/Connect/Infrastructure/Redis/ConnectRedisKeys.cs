using System.Text;

namespace Connect.Infrastructure.Redis;

public static class ConnectRedisKeys
{
    private const string Prefix = "connect:v2";

    public static string Player(Guid userId) => $"{Prefix}:player:{{{userId:D}}}";

    public static string Queue(Guid userId) => $"{Prefix}:queue:{{{userId:D}}}";

    public static string Presence(Guid userId) => $"{Prefix}:presence:{{{userId:D}}}";

    public static string Sessions => $"{Prefix}:sessions";

    public static string CleanupLease(Guid userId) => $"{Prefix}:cleanup:{{{userId:D}}}";

    public static string Command(Guid userId, Guid commandId) =>
        $"{Prefix}:command:{{{userId:D}}}:{commandId:D}";

    public static string Lease(Guid userId, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        string encodedConnectionId = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(connectionId))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"{Prefix}:lease:{{{userId:D}}}:{encodedConnectionId}";
    }
}

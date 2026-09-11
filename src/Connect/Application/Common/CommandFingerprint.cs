using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Connect.Application.Common;

internal static class CommandFingerprint
{
    public static string Create(string commandType, params string?[] values)
    {
        var canonical = new StringBuilder();
        foreach (string? value in values.Prepend(commandType))
        {
            string normalized = value ?? "<null>";
            canonical.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            canonical.Append(':');
            canonical.Append(normalized);
            canonical.Append('|');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(hash);
    }

    public static string GuidValue(Guid value) => value.ToString("D");

    public static string ExpiredConnections(IReadOnlyCollection<string> connectionIds) =>
        string.Join(
            "\n",
            connectionIds.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
}

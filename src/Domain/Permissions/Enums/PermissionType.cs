using System.Collections.Immutable;
using Domain.Roles.Enums;

namespace Domain.Permissions.Enums;

public static class PermissionType
{
    private const string PlayerPlay = "Player.Play";
    public static readonly ImmutableDictionary<string, List<string>> RoleClaims = new Dictionary<string, List<string>>
    {
        { ApplicationRole.Admin, [PlayerPlay] },
        { ApplicationRole.Artist, [PlayerPlay] },
        { ApplicationRole.User, [PlayerPlay] }
    }.ToImmutableDictionary();
}
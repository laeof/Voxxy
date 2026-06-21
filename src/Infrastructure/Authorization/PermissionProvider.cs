using Application.Abstractions.Data;
using Domain.Roles;

namespace Infrastructure.Authorization;

internal sealed class PermissionProvider(IApplicationDbContext context)
{
    public Task<HashSet<string>> GetForUserIdAsync(Guid userId)
    {
        var permissions = context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .SelectMany(r => r.Permissions)
            .Select(p => p.Value)
            .ToHashSet();

        return Task.FromResult(permissions);
    }
}

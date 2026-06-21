using System.ComponentModel;
using Application.Abstractions.Data;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Roles;
using Domain.Roles.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database;

public static class DatabaseInitializer
{
    public static async Task InitializePermissionsAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = app.Services.CreateScope();

        IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (await context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        await using IDbContextTransaction transaction = await context.BeginTransactionAsync(cancellationToken);

        try
        {
            await SeedRolesAsync(context, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await SeedPermissionsAsync(context, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static async Task SeedRolesAsync(IApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        Role[] roles = [
            new Role { Name = ApplicationRole.Admin },
            new Role { Name = ApplicationRole.User },
            new Role { Name = ApplicationRole.Artist }
        ];

        await db.Roles.AddRangeAsync(roles, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(IApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        List<Role> roles = await db.Roles
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);

        foreach ((string roleName, List<string> permissions) in PermissionType.RoleClaims)
        {
            Role? role = roles.FirstOrDefault(r => r.Name == roleName);

            if (role is null)
            {
                continue;
            }

            foreach (string permissionValue in permissions)
            {
                bool exists = role.Permissions.Any(p => p.Value == permissionValue);

                if (!exists)
                {
                    role.Permissions.Add(new Permission
                    {
                        Value = permissionValue
                    });
                }
            }
        }
    }
}
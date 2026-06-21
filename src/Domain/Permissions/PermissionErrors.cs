using SharedKernel;

namespace Domain.Permissions;

public static class PermissionErrors
{
    public static Error PermissionNotFound(Guid id) => Error.NotFound(
        "Permission.NotFound",
        $"Permission with id {id} not found.");
}
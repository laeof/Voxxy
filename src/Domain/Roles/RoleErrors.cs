using SharedKernel;

namespace Domain.Roles;

public static class RoleErrors
{
    public static Error RoleNotFound(Guid id) => Error.NotFound(
        "Role.NotFound",
        $"Role with id {id} not found.");
}
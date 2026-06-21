using SharedKernel;
using Domain.Roles;

namespace Domain.Permissions;

public class Permission : Entity
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }

    public static Permission Create(string permission, Guid roleId)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Value = permission,
            RoleId = roleId
        };
    }
}

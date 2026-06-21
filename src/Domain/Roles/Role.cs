using Domain.Permissions;
using Domain.Users;
using SharedKernel;

namespace Domain.Roles;

public class Role: Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Permission> Permissions { get; set; }
    public List<User> Users { get; set; }
}
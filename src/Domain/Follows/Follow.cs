using SharedKernel;
using SharedKernel.Enums;

namespace Domain.Follows;

public class Following: Entity
{
    public Guid Id { get; set; }
    public Guid FollowerId { get; set; }
    public Guid FolloweeId { get; set; }
    public FollowType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
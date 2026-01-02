using SharedKernel;
using SharedKernel.Enums;

namespace Domain.Users;

public static class FollowErrors
{
    public static Error AlreadyFollowing(Guid followerId, Guid followeeId) => Error.Conflict(
        "Follows.AlreadyFollowing",
        $"The follower with the Id = '{followerId}' is already following the followee with the Id = '{followeeId}'");

    public static Error NotFollowing(Guid followerId, Guid followeeId) => Error.Conflict(
        "Follows.NotFollowing",
        $"The follower with the Id = '{followerId}' is not following the followee with the Id = '{followeeId}'");

    public static Error InvalidFollowType(FollowType type) => Error.Validation(
        "Follows.InvalidFollowType",
        $"The follow type '{type}' is invalid");

    public static Error CannotFollowOneself(Guid followerId) => Error.Conflict(
        "Follows.CannotFollowOneself",
        $"The follower with the Id = '{followerId}' cannot follow themselves");

    public static Error CannotUnfollowOneself(Guid userId) => Error.Conflict(
        "Follows.CannotUnfollowOneself",
        $"The user with the Id = '{userId}' cannot unfollow themselves");

    public static Error FollowerAndCurrentUserAreDifferent(Guid followerId, Guid currentUserId) => Error.Conflict(
        "Follows.FollowerAndCurrentUserAreDifferent",
        $"The follower with the Id = '{followerId}' does not match the current user with the Id = '{currentUserId}'");
}
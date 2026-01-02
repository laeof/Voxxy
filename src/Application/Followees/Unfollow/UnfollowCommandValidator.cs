using FluentValidation;

namespace Application.Followees.Unfollow;

public class UnfollowCommandValidator : AbstractValidator<UnfollowCommand>
{
    public UnfollowCommandValidator()
    {
        RuleFor(c => c.FollowerId).NotEmpty();
        RuleFor(c => c.FolloweeId).NotEmpty();
    }
}

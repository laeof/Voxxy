using FluentValidation;

namespace Application.Followees.Follow;

public class FollowCommandValidator : AbstractValidator<FollowCommand>
{
    public FollowCommandValidator()
    {
        RuleFor(c => c.FollowerId).NotEmpty();
        RuleFor(c => c.FolloweeId).NotEmpty();
        RuleFor(c => c.Type).IsInEnum();
    }
}

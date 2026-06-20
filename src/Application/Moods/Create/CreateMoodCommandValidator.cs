using FluentValidation;

namespace Application.Moods.Create;

public class CreateMoodCommandValidator : AbstractValidator<CreateMoodCommand>
{
    public CreateMoodCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
    }
}

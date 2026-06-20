using FluentValidation;

namespace Application.Genres.Create;

public class CreateGenreCommandValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
    }
}

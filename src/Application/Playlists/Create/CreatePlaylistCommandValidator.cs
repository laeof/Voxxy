using FluentValidation;

namespace Application.Playlists.Create;

public class CreatePlaylistCommandValidator : AbstractValidator<CreatePlaylistCommand>
{
    public CreatePlaylistCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
    }
}

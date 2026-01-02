using System.Data;
using FluentValidation;

namespace Application.Tracks.Create;

public class CreateTrackCommandValidator : AbstractValidator<CreateTrackCommand>
{
    public CreateTrackCommandValidator()
    {
        RuleFor(c => c.ArtistId).NotEmpty();
        RuleFor(c => c.AlbumId).NotEmpty();
        RuleFor(c => c.AlbumOrder).GreaterThan(0);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
    }
}

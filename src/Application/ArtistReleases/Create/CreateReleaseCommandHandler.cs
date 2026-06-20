using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.ArtistReleases;
using Domain.Artists;
using Domain.Genres;
using Domain.Moods;
using Domain.Tracks;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.ArtistReleases.Create;

internal sealed class CreateReleaseCommandHandler(
    IApplicationDbContext context,
    IOptions<DefaultAssetsOptions> defaultAssets,
    IUserContext userContext)
    : ICommandHandler<CreateReleaseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateReleaseCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (e.g. check if artists exist, validate tracks etc.)

        var release = Release.Create(
            command.Title,
            command.AdditionalInformation,
            command.Copyright,
            defaultAssets.Value.ImageLogicUrl,
            command.ReleaseDate,
            command.ReleaseType,
            command.Artists,
            command.Genres,
            command.Moods,
            command.Tracks.Select(t => new CreateTrackDto(t.Title, defaultAssets.Value.AudioLogicUrl, defaultAssets.Value.ImageLogicUrl, t.Position, t.Duration, t.IsRemix, t.AudioFile)).ToList(),
            command.CoverImage);

        release.Artists.AddRange(await context.Artists.Where(a => command.Artists.Contains(a.Id)).ToListAsync(cancellationToken));

        context.Releases.Add(release);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(release.Id);
    }
}
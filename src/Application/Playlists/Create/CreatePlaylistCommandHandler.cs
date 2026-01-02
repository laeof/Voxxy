using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Playlists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Playlists.Create;

internal sealed class CreatePlaylistCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreatePlaylistCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePlaylistCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations

        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            CreatedBy = userContext.UserId,
            Name = command.Name,
            CreatedAt = dateTimeProvider.UtcNow,
            Color = PlaylistConstants.DefaultPlaylistColor,
            ImageUrl = PlaylistConstants.DefaultPlaylistImageUrl,
            Type = (int)PlaylistType.Playlist,
        };

        context.Playlists.Add(playlist);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(playlist.Id);
    }
}
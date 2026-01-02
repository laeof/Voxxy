using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Albums;
using Domain.Artists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Albums.Create;

internal sealed class CreateAlbumCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateAlbumCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAlbumCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        Artist? artist = await context.Artists.AsNoTracking()
            .SingleOrDefaultAsync(a => a.UserId == userContext.UserId, cancellationToken);

        if (artist is null)
        {
            return Result.Failure<Guid>(ArtistErrors.NotFound(userContext.UserId));
        }

        //todo make more validations

        var album = new Album
        {
            Id = Guid.NewGuid(),
            CreatedBy = userContext.UserId,
            Name = command.Name,
            CreatedAt = dateTimeProvider.UtcNow,
            Color = AlbumsConstants.DefaultAlbumColor,
            ImageUrl = AlbumsConstants.DefaultAlbumImageUrl,
            Type = (int)PlaylistType.Album,
            ArtistId = artist.Id,
        };

        context.Albums.Add(album);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(album.Id);
    }
}
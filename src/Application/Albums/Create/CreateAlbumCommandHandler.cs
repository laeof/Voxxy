using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Albums;
using Domain.Artists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Albums.Create;

internal sealed class CreateAlbumCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IOptions<DefaultAssetsOptions> assetsOptions,
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

        var albumId = Guid.NewGuid();

        var album = Album.Create(
            albumId, 
            userContext.UserId, 
            command.Name, 
            dateTimeProvider, 
            assetsOptions.Value.ImageLogicUrl.Replace("{id}", albumId.ToString()), 
            (int)PlaylistType.Album, 
            artist.Id);

        context.Albums.Add(album);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(album.Id);
    }
}
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Artists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Artists.Create;

internal sealed class CreateArtistCommandHandler(
    IApplicationDbContext context,
    IOptions<DefaultAssetsOptions> defaultAssets,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateArtistCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateArtistCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations

        var artistId = Guid.NewGuid();

        var artist = Artist.Create(
            command.UserId,
            command.Name,
            defaultAssets.Value.ImageLogicUrl.Replace("{id}", artistId.ToString()),
            dateTimeProvider.UtcNow,
            userContext.UserId);

        context.Artists.Add(artist);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(artist.Id);
    }
}
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Domain.Playlists;
using Domain.Tracks;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Tracks.Create;

internal sealed class CreateTrackCommandHandler(
    IApplicationDbContext context,
    IOptions<DefaultAssetsOptions> defaultAssets,
    IUserContext userContext)
    : ICommandHandler<CreateTrackCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTrackCommand command, CancellationToken cancellationToken)
    {
        //todo make more validations
        
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        var trackId = Guid.NewGuid();

        string audioKey = defaultAssets.Value.AudioLogicUrl.Replace("{id}", trackId.ToString());
        string imageKey = defaultAssets.Value.ImageLogicUrl.Replace("{id}", command.AlbumId.ToString());

        var trackMetaData = TrackMetaData.Create(trackId, command.Name, command.AlbumId, command.ArtistId, command.AlbumOrder, DateTime.UtcNow);

        var track = Track.Create(trackMetaData, audioKey, imageKey);

        context.Tracks.Add(track);

        await context.SaveChangesAsync(cancellationToken);

        return trackId;
    }
}
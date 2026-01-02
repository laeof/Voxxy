using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Domain.Playlists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Tracks.Upload;

internal sealed class UploadTrackCommandHandler(
    IApplicationDbContext context,
    IOptions<ConnectionStringsOptions> connStrings,
    IUserContext userContext)
    : ICommandHandler<UploadTrackCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadTrackCommand command, CancellationToken cancellationToken)
    {
        //todo make more validations
        
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        var service = new BlobServiceClient(connStrings.Value.AzureStorage);

        BlobContainerClient container = service.GetBlobContainerClient("tracks");
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient($"{command.TrackId}/original.mp3");

        await blob.UploadAsync(
            command.FileStream,
            new BlobHttpHeaders { ContentType = command.ContentType },
            cancellationToken: cancellationToken);

        return command.TrackId;
    }
}
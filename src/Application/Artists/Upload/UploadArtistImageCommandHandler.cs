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

namespace Application.Artists.Upload;

internal sealed class UploadArtistImageCommandHandler(
    IApplicationDbContext context,
    IOptions<ConnectionStringsOptions> connStrings,
    IUserContext userContext)
    : ICommandHandler<UploadArtistImageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadArtistImageCommand command, CancellationToken cancellationToken)
    {
        //todo make more validations
        
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        var service = new BlobServiceClient(connStrings.Value.AzureStorage);

        BlobContainerClient container = service.GetBlobContainerClient(AzureContainerNames.Artists);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient($"{command.ArtistId}/cover.jpg");

        await blob.UploadAsync(
            command.FileStream,
            new BlobHttpHeaders { ContentType = command.ContentType },
            cancellationToken: cancellationToken);

        return command.ArtistId;
    }
}
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Tracks;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;

namespace Application.Tracks.Create.TrackCreatedDomainEventHandlers;

internal sealed class UploadTrackAudioDomainEventHandler : IDomainEventHandler<TrackCreatedDomainEvent>
{
    private readonly IOptions<ConnectionStringsOptions> connStrings;
    public UploadTrackAudioDomainEventHandler(IOptions<ConnectionStringsOptions> connStrings)
    {
        this.connStrings = connStrings;
    }

    public async Task Handle(TrackCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var service = new BlobServiceClient(connStrings.Value.AzureStorage);

        BlobContainerClient container = service.GetBlobContainerClient(AzureContainerNames.Tracks);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient($"{domainEvent.Track.Id}/original.mp3");

        await blob.UploadAsync(
            domainEvent.AudioStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "audio/mpeg"
                }
            },
            cancellationToken: cancellationToken);
    }
}
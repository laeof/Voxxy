using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.ArtistReleases;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;

namespace Application.ArtistReleases.Create.DomainEventHandlers;

public sealed class UploadReleaseImageDomainEvent : IDomainEventHandler<ReleaseDataCreatedDomainEvent>
{
    private readonly IOptions<ConnectionStringsOptions> connStrings;
    public UploadReleaseImageDomainEvent(IOptions<ConnectionStringsOptions> connStrings)
    {
        this.connStrings = connStrings;
    }
    public async Task Handle(ReleaseDataCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var service = new BlobServiceClient(connStrings.Value.AzureStorage);

        BlobContainerClient container = service.GetBlobContainerClient(AzureContainerNames.Albums);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient($"{domainEvent.ReleaseId}/cover.jpg");

        await blob.UploadAsync(
            domainEvent.CoverImage,
            cancellationToken: cancellationToken);
    }
}
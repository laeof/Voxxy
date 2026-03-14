using Application.Abstractions.Media;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Infrastructure.AzureBlobStorage;

internal sealed class AzureBlobMediaUrlResolver : IMediaUrlResolver
{
    private readonly BlobServiceClient _client;

    public AzureBlobMediaUrlResolver(IOptions<ConnectionStringsOptions> options)
    {
        _client = new BlobServiceClient(options.Value.AzureStorage);
    }

    public Uri GetPublicUrl(string containerName, string key)
    {
        BlobContainerClient container = _client.GetBlobContainerClient(containerName);
        BlobClient blob = container.GetBlobClient(key);

        return blob.Uri;
    }

    public Uri GetSasUrl(string containerName, string key, TimeSpan ttl)
    {
        BlobContainerClient container = _client.GetBlobContainerClient(containerName);
        BlobClient blob = container.GetBlobClient(key);

        Uri sas = blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(ttl));

        return sas;
    }
}

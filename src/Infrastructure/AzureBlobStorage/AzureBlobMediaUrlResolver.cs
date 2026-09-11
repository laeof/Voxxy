using Application.Abstractions.Media;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Infrastructure.AzureBlobStorage;

internal sealed class AzureBlobMediaUrlResolver : IMediaUrlResolver
{
    private readonly BlobServiceClient _client;
    private readonly string _publicBaseUrl;

    public AzureBlobMediaUrlResolver(IOptions<ConnectionStringsOptions> options)
    {
        _client = new BlobServiceClient(options.Value.AzureStorage);
        _publicBaseUrl = "/blob";
    }

    public Uri GetPublicUrl(string containerName, string key)
    {
        return new Uri($"{_publicBaseUrl}/{containerName}/{key}", UriKind.Relative);
    }

    public Uri GetSasUrl(string containerName, string key, TimeSpan ttl)
    {
        BlobContainerClient container = _client.GetBlobContainerClient(containerName);
        BlobClient blob = container.GetBlobClient(key);

        Uri sas = blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(ttl));

        return new Uri($"{_publicBaseUrl}/{containerName}/{key}{sas.Query}", UriKind.Relative);
    }
}

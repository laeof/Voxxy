using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Tracks.Streaming;

internal sealed class GetStreamUriQueryHandler(
    IApplicationDbContext context,
    IOptions<ConnectionStringsOptions> connStrings)
    : IQueryHandler<GetStreamUriQuery, Uri>
{
    public async Task<Result<Uri>> Handle(GetStreamUriQuery query, CancellationToken cancellationToken)
    {
        Track? track = await context.Tracks.FirstOrDefaultAsync(t => t.Id == query.TrackId, cancellationToken);
        
        if (track is null)
        {
            return Result.Failure<Uri>(TrackErrors.NotFound(query.TrackId));
        }

        var service = new BlobServiceClient(connStrings.Value.AzureStorage);

        BlobContainerClient container = service.GetBlobContainerClient("tracks");
        BlobClient blob = container.GetBlobClient(track.AudioKey);

        Uri uri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(5));

        return uri;
    }
}

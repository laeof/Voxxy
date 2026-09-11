using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Constants;

namespace Application.Tracks.Streaming;

internal sealed class GetStreamUriQueryHandler(
    IApplicationDbContext context,
    IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetStreamUriQuery, Uri>
{
    public async Task<Result<Uri>> Handle(GetStreamUriQuery query, CancellationToken cancellationToken)
    {
        Track? track = await context.Tracks.FirstOrDefaultAsync(t => t.Id == query.TrackId, cancellationToken);

        if (track is null)
        {
            return Result.Failure<Uri>(TrackErrors.NotFound(query.TrackId));
        }

        return mediaUrlResolver.GetSasUrl(AzureContainerNames.Tracks, track.AudioKey, TimeSpan.FromMinutes(5));
    }
}

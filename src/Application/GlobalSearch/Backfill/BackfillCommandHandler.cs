using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.GlobalSearch.Backfill;
using Application.MeiliSearch;
using Microsoft.EntityFrameworkCore;
using SharedKernel.GlobalSearch;
using SharedKernel.Constants;
using SharedKernel.Enums;
using SharedKernel;

namespace Infrastructure.MeiliSearch.BackFill;

internal sealed class BackFillCommandHandler(ISearchIndexer searchIndexer, IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver) : ICommandHandler<BackfillCommand>
{
    public async Task<Result> Handle(BackfillCommand request, CancellationToken cancellationToken)
    {
        var documents = new List<SearchDocument>();

        documents.AddRange(await context.Tracks
            .Select(track => new SearchDocument
            {
                Id = track.Id,
                Type = SearchEntityType.Track,
                Title = track.Name,
                Artists = track.Artists.Select(a => new SearchArtist
                {
                    Id = a.Id,
                    Name = a.Name
                }).ToList(),
                ReleaseTitle = track.Release.Title,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Release.ImageKey).ToString(),
            })
            .ToListAsync(cancellationToken));

        documents.AddRange(await context.Artists
            .Select(artist => new SearchDocument
            {
                Id = artist.Id,
                Type = SearchEntityType.Artist,
                Title = artist.Name,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
            })
            .ToListAsync(cancellationToken));

        documents.AddRange(await context.Releases
            .Select(release => new SearchDocument
            {
                Id = release.Id,
                Type = SearchEntityType.Release,
                Title = release.Title,
                Artists = release.Artists.Select(a => new SearchArtist
                {
                    Id = a.Id,
                    Name = a.Name
                }).ToList(),
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, release.ImageKey).ToString()
            })
            .ToListAsync(cancellationToken));

        await searchIndexer.IndexManyAsync(documents, cancellationToken);

        return Result.Success();
    }
}
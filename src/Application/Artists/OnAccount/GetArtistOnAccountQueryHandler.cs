using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Domain.Artists;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Artists.OnAccount;

internal sealed class GetArtistOnAccountQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetArtistOnAccountQuery, ForArtistResponse>
{
    public async Task<Result<ForArtistResponse>> Handle(GetArtistOnAccountQuery query, CancellationToken cancellationToken)
    {
        ForArtistResponse? forArtistResponse = await context.Artists
            .Where(artist => artist.UserId == query.UserId)
            .Select(artist => new ForArtistResponse
            {
                Artists = new List<ArtistResponse>
                {
                    new ArtistResponse
                    {
                        Id = artist.Id,
                        Name = artist.Name,
                        CreatedAt = artist.CreatedAt,
                        UpdatedAt = artist.UpdatedAt,
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                    },
                },
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (forArtistResponse is null)
        {
            return Result.Failure<ForArtistResponse>(ArtistErrors.NotFound(query.UserId));
        }

        return forArtistResponse;
    }
}

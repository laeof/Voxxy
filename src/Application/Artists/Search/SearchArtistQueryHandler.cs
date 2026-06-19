using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Artists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Artists.Search;

internal sealed class SearchArtistQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<SearchArtistQuery, List<SearchArtistResponse>>
{
    public async Task<Result<List<SearchArtistResponse>>> Handle(SearchArtistQuery request, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<List<SearchArtistResponse>>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (artist check)

        int limit = Math.Clamp(request.Limit, 1, 30);

        IQueryable<Artist> artistsQuery = context.Artists
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();

            artistsQuery = artistsQuery.Where(x =>
                EF.Functions.Like(x.Name, $"%{search}%"));
        }

        List<SearchArtistResponse> artists = await artistsQuery
            .OrderBy(x => x.Name)
            .Take(limit)
            .Select(x => new SearchArtistResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(artists);
    }
}
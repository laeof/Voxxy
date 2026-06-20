using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Genres;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Genres.Search;

internal sealed class SearchGenreQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<SearchGenreQuery, List<SearchGenreResponse>>
{
    public async Task<Result<List<SearchGenreResponse>>> Handle(SearchGenreQuery request, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<List<SearchGenreResponse>>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (artist check)

        int limit = Math.Clamp(request.Limit, 1, 30);

        IQueryable<Genre> genresQuery = context.Genres
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();

            genresQuery = genresQuery.Where(x =>
                EF.Functions.Like(x.Name, $"%{search}%"));
        }

        List<SearchGenreResponse> genres = await genresQuery
            .OrderBy(x => x.Name)
            .Take(limit)
            .Select(x => new SearchGenreResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(genres);
    }
}
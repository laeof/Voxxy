using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Moods;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Moods.Search;

internal sealed class SearchMoodQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<SearchMoodQuery, List<SearchMoodResponse>>
{
    public async Task<Result<List<SearchMoodResponse>>> Handle(SearchMoodQuery request, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<List<SearchMoodResponse>>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (artist check)

        int limit = Math.Clamp(request.Limit, 1, 30);

        IQueryable<Mood> moodsQuery = context.Moods
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();

            moodsQuery = moodsQuery.Where(x =>
                EF.Functions.Like(x.Name, $"%{search}%"));
        }

        List<SearchMoodResponse> moods = await moodsQuery
            .OrderBy(x => x.Name)
            .Take(limit)
            .Select(x => new SearchMoodResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(moods);
    }
}
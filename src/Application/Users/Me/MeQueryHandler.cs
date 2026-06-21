using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;

namespace Application.Users.Me;

internal sealed class MeQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> Handle(MeQuery query, CancellationToken cancellationToken)
    {
        MeResponse? user = await context.Users
            .Where(u => u.Id == query.Id)
            .Select(u => new MeResponse
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Users, u.ImageKey).ToString(),
                UserClaims = u.Roles
                    .SelectMany(role => role.Permissions)
                    .Select(permission => permission.Value)
                    .Distinct()
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<MeResponse>(UserErrors.NotFound(query.Id));
        }

        return Result.Success(user);
    }
}
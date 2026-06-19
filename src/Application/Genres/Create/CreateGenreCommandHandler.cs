using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Genres;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Genres.Create;

internal sealed class CreateGenreCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateGenreCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (admin check)

        var genre = Genre.Create(command.Title);

        context.Genres.Add(genre);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(genre.Id);
    }
}
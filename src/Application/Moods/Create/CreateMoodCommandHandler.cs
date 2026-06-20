using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Moods;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Moods.Create;

internal sealed class CreateMoodCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateMoodCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMoodCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userContext.UserId));
        }

        //todo make more validations (admin check)

        var mood = Mood.Create(command.Title);

        context.Moods.Add(mood);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(mood.Id);
    }
}
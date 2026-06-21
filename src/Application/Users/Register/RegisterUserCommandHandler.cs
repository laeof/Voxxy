using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Roles;
using Domain.Roles.Enums;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<DefaultAssetsOptions> assetsOptions) : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        Role role = await context.Roles.SingleAsync(r => r.Name == ApplicationRole.User, cancellationToken);

        var userId = Guid.NewGuid();

        var user = User.Create(
            userId,
            command.Email,
            command.FirstName,
            command.LastName,
            passwordHasher.Hash(command.Password),
            assetsOptions.Value.ImageLogicUrl.Replace("{id}", userId.ToString()),
            DateTime.UtcNow);

        user = User.AssignRole(user, role);

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        context.Users.Add(user);

        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

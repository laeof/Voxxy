using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Users.Me;
using Domain.Token;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IMediaUrlResolver mediaUrlResolver,
    ITokenProvider tokenProvider) : ICommandHandler<LoginUserCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(UserErrors.NotFoundByEmail);
        }

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            return Result.Failure<LoginResponse>(UserErrors.InvalidEmailOrPassword);
        }

        string token = tokenProvider.CreateAccessToken(user);
        string refreshToken = tokenProvider.CreateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync(cancellationToken);

        List<string> permissions = await context.Users
            .Where(u => u.Id == user.Id)
            .SelectMany(u => u.Roles)
            .SelectMany(role => role.Permissions)
            .Select(permission => permission.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        LoginResponse response = new()
        {
            Me = new MeResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FirstName + " " + user.LastName,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Users, user.ImageKey).ToString(),
                UserClaims = permissions
            },
            AccessToken = token,
            RefreshToken = refreshToken,
        };

        return Result.Success(response);
    }
}

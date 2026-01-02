using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Me;
using Domain.Token;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Refresh;

internal sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ITokenProvider tokenProvider) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == command.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure<RefreshTokenResponse>(RefreshTokenErrors.InvalidRefreshToken);
        }

        User user = refreshToken.User;
        refreshToken.RevokedAt = dateTimeProvider.UtcNow;
        context.RefreshTokens.Update(refreshToken);

        RefreshToken newRefreshToken = new()
        {
            Id = Guid.NewGuid(),
            Token = tokenProvider.CreateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = dateTimeProvider.UtcNow.AddDays(30),
            CreatedAt = dateTimeProvider.UtcNow,
        };

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokenResponse
        {
            Me = new MeResponse {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FirstName + " " + user.LastName,
                ImageUrl = user.ImageUrl,
                UserClaims = Array.Empty<string>()
            },
            AccessToken = tokenProvider.CreateAccessToken(user),
            RefreshToken = newRefreshToken.Token
        });
    }
}
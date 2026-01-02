using SharedKernel;

namespace Domain.Token;

public static class RefreshTokenErrors
{
    public static readonly Error InvalidRefreshToken = Error.NotFound(
        "RefreshToken.Invalid",
        "The provided refresh token is invalid.");
}
using Domain.Users;
using SharedKernel.Constants;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
}

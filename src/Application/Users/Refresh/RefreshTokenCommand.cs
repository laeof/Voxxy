using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Refresh;

public sealed record RefreshTokenCommand(User user, string RefreshToken) : ICommand<RefreshTokenResponse>;

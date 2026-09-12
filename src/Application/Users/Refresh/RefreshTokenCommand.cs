using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

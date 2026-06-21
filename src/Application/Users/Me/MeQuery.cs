using Application.Abstractions.Messaging;

namespace Application.Users.Me;

public sealed record MeQuery(Guid Id) : IQuery<MeResponse>;
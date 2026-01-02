using Application.Abstractions.Messaging;

namespace Application.Followees.Get;

public sealed record GetUserFollowsQuery() : IQuery<List<FollowResponse>>;
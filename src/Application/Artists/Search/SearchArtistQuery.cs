using Application.Abstractions.Messaging;

namespace Application.Artists.Search;

public sealed record SearchArtistQuery(string? Search, int Limit) : IQuery<List<SearchArtistResponse>>;
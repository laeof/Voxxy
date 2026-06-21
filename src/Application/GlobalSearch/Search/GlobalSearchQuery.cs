using Application.Abstractions.Messaging;

namespace Application.GlobalSearch.Search;

public sealed record GlobalSearchQuery(string Query, int Limit = 20) : IQuery<List<SearchResponse>>;
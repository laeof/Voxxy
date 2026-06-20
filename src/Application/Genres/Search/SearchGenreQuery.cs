using Application.Abstractions.Messaging;

namespace Application.Genres.Search;

public sealed record SearchGenreQuery(string? Search, int Limit) : IQuery<List<SearchGenreResponse>>;
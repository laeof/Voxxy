using Application.Abstractions.Messaging;

namespace Application.Moods.Search;

public sealed record SearchMoodQuery(string? Search, int Limit) : IQuery<List<SearchMoodResponse>>;
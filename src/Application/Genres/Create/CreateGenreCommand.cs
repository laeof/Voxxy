using Application.Abstractions.Messaging;

namespace Application.Genres.Create;

public sealed record CreateGenreCommand(string Title) : ICommand<Guid>;
using Application.Abstractions.Messaging;

namespace Application.Moods.Create;

public sealed record CreateMoodCommand(string Title) : ICommand<Guid>;